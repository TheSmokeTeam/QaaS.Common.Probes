using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using QaaS.Framework.SDK.ContextObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

internal static partial class RedisCommandProbeSupport
{
    private const string ResultsRootKey = "RedisCommandResults";

    public static object[] BuildArguments(
        Context context,
        IEnumerable<string> rawArguments,
        string? appendArgumentsFromResultPath,
        bool skipWhenExpandedArgumentsEmpty,
        ILogger logger,
        out bool shouldSkip)
    {
        var arguments = rawArguments.Select(argument => (object)ExpandPlaceholders(argument, context)).ToList();
        var appendedArguments = ExpandArgumentsFromPath(context, appendArgumentsFromResultPath);

        shouldSkip = skipWhenExpandedArgumentsEmpty && appendedArguments.Count == 0;
        if (shouldSkip)
        {
            logger.LogDebug(
                "Skipping redis command because result path {ResultPath} resolved to no values",
                appendArgumentsFromResultPath);
            return [];
        }

        arguments.AddRange(appendedArguments);
        return arguments.ToArray();
    }

    public static void StoreResult(Context context, string resultKey, RedisResult result)
    {
        context.InsertValueIntoGlobalDictionary(
            [ResultsRootKey, resultKey],
            ConvertResultToJsonNode(result));
    }

    public static string ResolveValueAsString(Context context, string resultPath)
    {
        var resolvedNode = ResolveStoredResultPath(context, resultPath);
        return ConvertJsonNodeToString(resolvedNode);
    }

    private static List<object> ExpandArgumentsFromPath(Context context, string? resultPath)
    {
        if (string.IsNullOrWhiteSpace(resultPath))
            return [];

        var resolvedNode = ResolveStoredResultPath(context, resultPath);
        return resolvedNode switch
        {
            null => [],
            JsonArray array => array.Select(ConvertJsonNodeToArgument).ToList(),
            _ => [ConvertJsonNodeToArgument(resolvedNode)]
        };
    }

    private static string ExpandPlaceholders(string value, Context context)
    {
        return PlaceholderPattern().Replace(value, match =>
        {
            var resultPath = match.Groups["path"].Value;
            var fallbackValue = match.Groups["fallback"].Success
                ? match.Groups["fallback"].Value
                : null;

            try
            {
                var resolvedNode = ResolveStoredResultPath(context, resultPath);
                return resolvedNode is null
                    ? fallbackValue ?? string.Empty
                    : ConvertJsonNodeToString(resolvedNode);
            }
            catch (Exception) when (fallbackValue is not null)
            {
                return fallbackValue;
            }
        });
    }

    private static JsonNode? ResolveStoredResultPath(Context context, string resultPath)
    {
        var pathSegments = resultPath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathSegments.Length == 0)
            throw new ArgumentException("Result path cannot be empty", nameof(resultPath));

        var storedNode = context.GetValueFromGlobalDictionary([ResultsRootKey, pathSegments[0]]) as JsonNode
                         ?? throw new KeyNotFoundException(
                             $"Redis command result '{pathSegments[0]}' was not found in probe context.");

        JsonNode? currentNode = storedNode;
        foreach (var segment in pathSegments.Skip(1))
            currentNode = ResolveNextPathSegment(currentNode, segment);

        return currentNode;
    }

    private static JsonNode? ResolveNextPathSegment(JsonNode? currentNode, string segment)
    {
        return currentNode switch
        {
            null => null,
            JsonObject jsonObject when jsonObject.TryGetPropertyValue(segment, out var nextNode) => nextNode,
            JsonArray jsonArray when int.TryParse(segment, out var arrayIndex) &&
                                     arrayIndex >= 0 &&
                                     arrayIndex < jsonArray.Count => jsonArray[arrayIndex],
            _ => throw new KeyNotFoundException($"Redis command result path segment '{segment}' was not found.")
        };
    }

    private static object ConvertJsonNodeToArgument(JsonNode? node)
    {
        return node switch
        {
            null => string.Empty,
            JsonValue jsonValue => ConvertJsonValue(jsonValue),
            _ => ConvertJsonNodeToString(node)
        };
    }

    private static object ConvertJsonValue(JsonValue jsonValue)
    {
        if (jsonValue.TryGetValue<string>(out var stringValue))
            return stringValue ?? string.Empty;
        if (jsonValue.TryGetValue<long>(out var longValue))
            return longValue;
        if (jsonValue.TryGetValue<double>(out var doubleValue))
            return doubleValue;
        if (jsonValue.TryGetValue<bool>(out var boolValue))
            return boolValue;

        return jsonValue.ToJsonString();
    }

    private static string ConvertJsonNodeToString(JsonNode? node)
    {
        return ConvertJsonNodeToArgument(node).ToString() ?? string.Empty;
    }

    private static JsonNode? ConvertResultToJsonNode(RedisResult result)
    {
        try
        {
            var nestedResults = (RedisResult[]?)result;
            if (nestedResults is not null)
            {
                var jsonArray = new JsonArray();
                foreach (var nestedResult in nestedResults)
                    jsonArray.Add(ConvertResultToJsonNode(nestedResult));
                return jsonArray;
            }
        }
        catch (InvalidCastException)
        {
            // Scalar result.
        }

        try
        {
            return JsonValue.Create((string?)result);
        }
        catch (InvalidCastException)
        {
            // Not a string result.
        }

        try
        {
            return JsonValue.Create((long)result);
        }
        catch (InvalidCastException)
        {
            // Not an integer result.
        }

        try
        {
            return JsonValue.Create((double)result);
        }
        catch (InvalidCastException)
        {
            // Not a floating point result.
        }

        return JsonValue.Create(result.ToString());
    }

    [GeneratedRegex("\\{\\{(?<path>[^}|]+)(\\|(?<fallback>[^}]*))?}}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderPattern();
}
