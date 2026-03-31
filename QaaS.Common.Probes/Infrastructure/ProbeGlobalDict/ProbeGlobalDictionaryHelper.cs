using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using QaaS.Framework.Configurations;
using QaaS.Framework.SDK.ContextObjects;

namespace QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

/// <summary>
/// Describes a global-dictionary payload lookup that participates in the final probe configuration merge.
/// </summary>
/// <param name="PayloadKey">The payload slot to read from the resolved canonical probe entry.</param>
/// <param name="AliasPath">The session-scoped alias path that points to the canonical entry.</param>
public readonly record struct ProbeGlobalDictReadRequest(string PayloadKey, IReadOnlyList<string> AliasPath);

/// <summary>
/// Shared helper for probe-specific global-dictionary fallbacks and recovery payloads.
/// The helper keeps all canonical data under a unique probe-scoped path and uses aliases only as lightweight pointers
/// to that canonical entry. This avoids collisions between different probes while still enabling recovery flows such as
/// "delete X" followed by "recreate X" in the same execution and session.
/// </summary>
internal static class ProbeGlobalDictionaryHelper
{
    private const string RootKey = "__ProbeGlobalDict";
    private const string ScopedKey = "Scoped";
    private const string AliasesKey = "Aliases";
    private const string SessionNameBaggageKey = "qaas.probe.session-name";
    private const string ProbeNameBaggageKey = "qaas.probe.probe-name";
    internal const string ConfigurationDefaultsPayloadKey = "__ConfigurationDefaults";

    /// <summary>
    /// Merges probe-global-dictionary payloads into the raw local configuration.
    /// Merge order is strictly "global defaults first, local configuration last" so raw YAML/code values always win
    /// when a key is present locally, even if that value is <c>false</c>, <c>0</c>, or an empty collection.
    /// </summary>
    internal static IConfiguration MergeConfigurationPatches(
        Context context,
        IConfiguration localConfiguration,
        IEnumerable<ProbeGlobalDictReadRequest> readRequests)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(localConfiguration);
        ArgumentNullException.ThrowIfNull(readRequests);

        IConfiguration mergedConfiguration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        foreach (var readRequest in readRequests)
        {
            if (TryGetPayload(context, readRequest.AliasPath, readRequest.PayloadKey, out var payload))
            {
                mergedConfiguration = mergedConfiguration.UpdateConfiguration(payload);
            }
        }

        // Force the raw IConfiguration patch through the object-shaped merge path. Calling the generic
        // IConfiguration overload here would treat IConfiguration as the target runtime type and skip the
        // key-presence-based flatten/overlay behavior that preserves local YAML/code values correctly.
        return mergedConfiguration.UpdateConfiguration((object)localConfiguration);
    }

    /// <summary>
    /// Saves the fully resolved probe configuration under the current probe's canonical path and updates the supplied
    /// family-level aliases to point at that canonical entry.
    /// </summary>
    internal static void SaveConfigurationDefaults(Context context, object configuration,
        params IReadOnlyList<string>[] aliasPaths)
    {
        SavePayload(context, ConfigurationDefaultsPayloadKey, configuration, aliasPaths);
    }

    /// <summary>
    /// Saves a custom recovery payload under the current probe's canonical path and updates the supplied aliases to
    /// point at that canonical entry.
    /// </summary>
    internal static void SavePayload(Context context, string payloadKey, object payload,
        params IReadOnlyList<string>[] aliasPaths)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadKey);
        ArgumentNullException.ThrowIfNull(payload);

        var canonicalPath = GetCanonicalPath(context);
        var entry = GetEntry(context, canonicalPath) ?? new ProbeGlobalDictEntry();
        entry.Payloads[payloadKey] = payload;
        context.InsertValueIntoGlobalDictionary(canonicalPath.ToList(), entry);

        foreach (var aliasPath in aliasPaths)
        {
            context.InsertValueIntoGlobalDictionary(aliasPath.ToList(), new ProbeGlobalDictAlias(canonicalPath));
        }
    }

    internal static bool TryGetPayload(Context context, IReadOnlyList<string> aliasPath, string payloadKey,
        out object payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(aliasPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadKey);

        payload = default!;
        var entry = ResolveEntry(context, aliasPath);
        if (entry == null ||
            !entry.Payloads.TryGetValue(payloadKey, out var storedPayload) ||
            storedPayload == null)
        {
            return false;
        }

        payload = storedPayload;
        return true;
    }

    internal static IReadOnlyList<string> GetCanonicalPath(Context context)
    {
        var descriptor = GetCurrentProbeDescriptor();
        return
        [
            RootKey,
            ScopedKey,
            GetExecutionScopeKey(context),
            descriptor.SessionName,
            descriptor.ProbeName
        ];
    }

    internal static IReadOnlyList<string> GetAliasPath(Context context, params string[] aliasSegments)
    {
        var descriptor = GetCurrentProbeDescriptor();
        var path = new List<string>(4 + aliasSegments.Length)
        {
            RootKey,
            AliasesKey,
            GetExecutionScopeKey(context),
            descriptor.SessionName
        };
        path.AddRange(aliasSegments);
        return path;
    }

    private static ProbeGlobalDictEntry? ResolveEntry(Context context, IReadOnlyList<string> aliasPath)
    {
        try
        {
            var aliasValue = context.GetValueFromGlobalDictionary(aliasPath.ToList());
            return aliasValue switch
            {
                ProbeGlobalDictAlias alias => GetEntry(context, alias.CanonicalPath),
                ProbeGlobalDictEntry entry => entry,
                _ => null
            };
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static ProbeGlobalDictEntry? GetEntry(Context context, IReadOnlyList<string> canonicalPath)
    {
        try
        {
            return context.GetValueFromGlobalDictionary(canonicalPath.ToList()) as ProbeGlobalDictEntry;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static string GetExecutionScopeKey(Context context)
    {
        if (!string.IsNullOrWhiteSpace(context.ExecutionId) || !string.IsNullOrWhiteSpace(context.CaseName))
        {
            return $"{context.ExecutionId ?? "<null>"}::{context.CaseName ?? "<null>"}";
        }

        return $"context::{RuntimeHelpers.GetHashCode(context):X8}";
    }

    private static (string SessionName, string ProbeName) GetCurrentProbeDescriptor()
    {
        var currentActivity = Activity.Current;
        var sessionName = currentActivity?.GetBaggageItem(SessionNameBaggageKey);
        var probeName = currentActivity?.GetBaggageItem(ProbeNameBaggageKey);
        if (!string.IsNullOrWhiteSpace(sessionName) && !string.IsNullOrWhiteSpace(probeName))
        {
            return (sessionName, probeName);
        }

        throw new InvalidOperationException(
            "Probe execution scope is not available. Runner should wrap probe configuration loading and execution " +
            "inside a probe execution Activity so probe global-dictionary paths remain unique per session and probe.");
    }

    private sealed record ProbeGlobalDictAlias(IReadOnlyList<string> CanonicalPath);

    private sealed class ProbeGlobalDictEntry
    {
        public Dictionary<string, object?> Payloads { get; } = new(StringComparer.Ordinal);
    }
}
