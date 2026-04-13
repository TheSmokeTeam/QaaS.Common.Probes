using System.Text.Json;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.SDK.ContextObjects;

namespace QaaS.Common.Probes.RabbitMqProbes;

internal static class RabbitMqRecoverySnapshotHelper
{
    public static TConfig? TryGetConfigurationDefaults<TConfig>(Context context, IReadOnlyList<string> aliasPath)
        where TConfig : class
        => TryGetPayload<TConfig>(context, aliasPath, ProbeGlobalDictionaryHelper.ConfigurationDefaultsPayloadKey);

    public static TPayload? TryGetRecoveryPayload<TPayload>(Context context, IReadOnlyList<string> aliasPath)
        where TPayload : class
        => TryGetPayload<TPayload>(context, aliasPath, "recovery");

    public static TItem[] FilterByNames<TItem>(IEnumerable<TItem>? source, IEnumerable<string> names, Func<TItem, string?> nameSelector)
    {
        if (source == null)
        {
            return [];
        }

        var wantedNames = new HashSet<string>(names, StringComparer.Ordinal);
        return source.Where(item =>
            {
                var name = nameSelector(item);
                return !string.IsNullOrWhiteSpace(name) && wantedNames.Contains(name);
            })
            .ToArray();
    }

    private static TPayload? TryGetPayload<TPayload>(Context context, IReadOnlyList<string> aliasPath, string payloadKey)
        where TPayload : class
    {
        if (!ProbeGlobalDictionaryHelper.TryGetPayload(context, aliasPath, payloadKey, out var payload) || payload == null)
        {
            return null;
        }

        if (payload is TPayload typedPayload)
        {
            return typedPayload;
        }

        return JsonSerializer.Deserialize<TPayload>(JsonSerializer.Serialize(payload));
    }
}
