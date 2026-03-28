using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using QaaS.Framework.SDK.ContextObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

internal static partial class RedisCommandRuntimeResolver
{
    private const string ResultsRootKey = "__RedisResults";

    [GeneratedRegex(@"\$\{redisResults:(?<path>[^}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex RedisResultsPlaceholderRegex();

    public static string ResolveCommand(Context context, string command)
        => ReplaceScalarPlaceholders(context, command);

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

    public static void StoreResult(Context context, string? alias, RedisResult result)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        context.InsertValueIntoGlobalDictionary([ResultsRootKey, alias.Trim()], ConvertRedisResult(result));
    }

    public static void StoreEmptyResult(Context context, string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        context.InsertValueIntoGlobalDictionary([ResultsRootKey, alias.Trim()], Array.Empty<object?>());
    }

    public static string ResolveStoredResultAsString(Context context, string resultPath)
        => ConvertScalarToString(ResolvePlaceholderValue(context, resultPath));

    private static string ReplaceScalarPlaceholders(Context context, string value)
    {
        return RedisResultsPlaceholderRegex().Replace(value, match =>
        {
            var resolvedValue = ResolvePlaceholderValue(context, match.Groups["path"].Value);
            if (resolvedValue is IEnumerable and not string and not byte[])
            {
                throw new InvalidOperationException(
                    $"Redis placeholder '{match.Value}' resolved to a collection and cannot be used as a scalar value.");
            }

            return ConvertScalarToString(resolvedValue);
        });
    }

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

    private static bool TryResolveListIndex(string pathPart, int count, out int index)
    {
        if (int.TryParse(pathPart, out index) && index >= 0 && index < count)
            return true;

        index = -1;
        return false;
    }

    private static void ExpandResolvedArgument(ICollection<object> arguments, object? value)
    {
        switch (value)
        {
            case null:
                arguments.Add(RedisValue.Null);
                return;
            case string text:
                arguments.Add(text);
                return;
            case byte[] bytes:
                arguments.Add(bytes);
                return;
            case IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    ExpandResolvedArgument(arguments, item);
                }

                return;
            default:
                arguments.Add(value);
                return;
        }
    }

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
