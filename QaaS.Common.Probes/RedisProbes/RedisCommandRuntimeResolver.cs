using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using QaaS.Framework.SDK.ContextObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Resolves <c>redisResults</c> placeholders at runtime and stores Redis command results
/// so later commands in the same probe execution can reuse them.
/// </summary>
internal static partial class RedisCommandRuntimeResolver
{
    private const string ResultsRootKey = "__RedisResults";

    [GeneratedRegex(@"\$\{redisResults:(?<path>[^}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex RedisResultsPlaceholderRegex();

    /// <summary>
    /// Resolves scalar placeholders inside a Redis command name.
    /// </summary>
    /// <param name="context">The current probe execution context.</param>
    /// <param name="command">The raw command text from configuration.</param>
    /// <returns>The command text with scalar placeholders expanded.</returns>
    public static string ResolveCommand(Context context, string command)
        => ReplaceScalarPlaceholders(context, command);

    /// <summary>
    /// Resolves configured Redis command arguments, expanding collection placeholders into multiple arguments.
    /// </summary>
    /// <param name="context">The current probe execution context.</param>
    /// <param name="arguments">The raw configured arguments.</param>
    /// <returns>The runtime Redis argument array.</returns>
    public static object[] ResolveArguments(Context context, IEnumerable<string>? arguments)
    {
        if (arguments == null)
            return [];

        var resolvedArguments = new List<object>();
        foreach (var argument in arguments)
        {
            var placeholderMatch = RedisResultsPlaceholderRegex().Match(argument);
            if (placeholderMatch.Success && placeholderMatch.Length == argument.Length)
            {
                var resolvedValue = ResolvePlaceholderValue(context, placeholderMatch.Groups["path"].Value);
                ExpandResolvedArgument(resolvedArguments, resolvedValue);
                continue;
            }

            resolvedArguments.Add(ReplaceScalarPlaceholders(context, argument));
        }

        return resolvedArguments.ToArray();
    }

    /// <summary>
    /// Stores a Redis command result under an alias for later placeholder reuse.
    /// </summary>
    /// <param name="context">The current probe execution context.</param>
    /// <param name="alias">The configured result alias.</param>
    /// <param name="result">The Redis command result to persist.</param>
    public static void StoreResult(Context context, string? alias, RedisResult result)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        context.InsertValueIntoGlobalDictionary([ResultsRootKey, alias.Trim()], ConvertRedisResult(result));
    }

    /// <summary>
    /// Replaces a stored result alias with an empty collection when a command is skipped.
    /// </summary>
    /// <param name="context">The current probe execution context.</param>
    /// <param name="alias">The configured result alias.</param>
    public static void StoreEmptyResult(Context context, string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        context.InsertValueIntoGlobalDictionary([ResultsRootKey, alias.Trim()], Array.Empty<object?>());
    }

    /// <summary>
    /// Resolves a stored result path and requires it to be scalar so it can be used by loop conditions.
    /// </summary>
    /// <param name="context">The current probe execution context.</param>
    /// <param name="resultPath">The stored result path to inspect.</param>
    /// <returns>The resolved scalar value converted to text.</returns>
    public static string ResolveStoredResultAsString(Context context, string resultPath)
    {
        var resolvedValue = ResolvePlaceholderValue(context, resultPath);
        EnsureScalarValue(resultPath, resolvedValue);
        return ConvertScalarToString(resolvedValue);
    }

    /// <summary>
    /// Expands scalar placeholders embedded inside a single string value.
    /// </summary>
    private static string ReplaceScalarPlaceholders(Context context, string value)
    {
        return RedisResultsPlaceholderRegex().Replace(value, match =>
        {
            var resolvedValue = ResolvePlaceholderValue(context, match.Groups["path"].Value);
            EnsureScalarValue(match.Value, resolvedValue);

            return ConvertScalarToString(resolvedValue);
        });
    }

    /// <summary>
    /// Resolves one placeholder expression, including optional <c>??</c> default fallback syntax.
    /// </summary>
    private static object? ResolvePlaceholderValue(Context context, string placeholderExpression)
    {
        var expressionParts = placeholderExpression.Split("??", 2, StringSplitOptions.TrimEntries);
        var placeholderPath = expressionParts[0];
        var defaultValue = expressionParts.Length == 2 ? expressionParts[1] : null;

        var pathParts = placeholderPath.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathParts.Length == 0)
        {
            throw new InvalidOperationException("redisResults placeholders must include a stored result alias.");
        }

        try
        {
            object? current = context.GetValueFromGlobalDictionary([ResultsRootKey, pathParts[0]]);
            foreach (var pathPart in pathParts.Skip(1))
            {
                current = ResolvePathPart(current, pathPart);
            }

            return current;
        }
        catch (KeyNotFoundException) when (defaultValue != null)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Walks one segment deeper into a stored Redis result object graph.
    /// </summary>
    private static object? ResolvePathPart(object? value, string pathPart)
    {
        if (value is Dictionary<string, object?> dictionary && dictionary.TryGetValue(pathPart, out var nestedValue))
            return nestedValue;

        if (value is IReadOnlyList<object?> readOnlyList &&
            TryResolveListIndex(pathPart, readOnlyList.Count, out var index))
        {
            return readOnlyList[index];
        }

        if (value is IList<object?> list && TryResolveListIndex(pathPart, list.Count, out index))
        {
            return list[index];
        }

        if (value is object?[] array && TryResolveListIndex(pathPart, array.Length, out index))
        {
            return array[index];
        }

        throw new KeyNotFoundException(
            $"Could not resolve redisResults path part '{pathPart}' from value type '{value?.GetType().Name ?? "null"}'.");
    }

    /// <summary>
    /// Tries to parse and validate a collection index.
    /// </summary>
    private static bool TryResolveListIndex(string pathPart, int count, out int index)
    {
        if (int.TryParse(pathPart, out index) && index >= 0 && index < count)
            return true;

        index = -1;
        return false;
    }

    /// <summary>
    /// Appends one resolved placeholder value to the outgoing Redis argument list,
    /// recursively flattening enumerable results into multiple arguments.
    /// </summary>
    private static void ExpandResolvedArgument(ICollection<object> arguments, object? value)
    {
        if (value == null)
        {
            arguments.Add(RedisValue.Null);
            return;
        }

        if (value is string or byte[])
        {
            arguments.Add(value);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                ExpandResolvedArgument(arguments, item);
            }

            return;
        }

        arguments.Add(value);
    }

    /// <summary>
    /// Converts a Redis result tree into plain CLR values that can be stored in the probe context.
    /// </summary>
    private static object? ConvertRedisResult(RedisResult result)
    {
        if (result.IsNull)
            return null;

        try
        {
            var nestedResults = (RedisResult[]?)result;
            if (nestedResults != null)
                return nestedResults.Select(ConvertRedisResult).ToList();
        }
        catch (InvalidCastException)
        {
            // Scalar Redis results are handled below.
        }

        try
        {
            return (long)result;
        }
        catch (InvalidCastException)
        {
            // Continue through other scalar conversions.
        }

        try
        {
            return (byte[]?)result;
        }
        catch (InvalidCastException)
        {
            // Fall back to string conversion for textual values.
        }

        return (string?)result;
    }

    /// <summary>
    /// Ensures a resolved placeholder value is scalar before using it in a scalar-only context.
    /// </summary>
    private static void EnsureScalarValue(string valueDescription, object? value)
    {
        if (value is IEnumerable and not string and not byte[])
        {
            throw new InvalidOperationException(
                $"Redis value '{valueDescription}' resolved to a collection and cannot be used as a scalar value.");
        }
    }

    /// <summary>
    /// Converts a resolved scalar placeholder value to the text form expected by Redis command APIs.
    /// </summary>
    private static string ConvertScalarToString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString() ?? string.Empty
        };
    }
}
