using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Configurations;
using QaaS.Framework.SDK.Hooks;
using QaaS.Framework.SDK.Hooks.Probe;

namespace QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

/// <summary>
/// Probe base that opt-in merges probe-global-dictionary defaults and recovery payloads before normal bind/validation.
/// The merge is deliberately based on raw configuration key presence rather than typed default values so explicit local
/// settings such as <c>false</c>, <c>0</c>, or an empty collection continue to override global defaults.
/// </summary>
public abstract class BaseProbeWithGlobalDictDefaults<TConfiguration> : BaseProbe<TConfiguration>, IHook
    where TConfiguration : class, IUseGlobalDictProbeConfiguration, new()
{
    /// <inheritdoc />
    public new List<ValidationResult>? LoadAndValidateConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool?>(nameof(IUseGlobalDictProbeConfiguration.UseGlobalDict)).GetValueOrDefault())
        {
            return base.LoadAndValidateConfiguration(configuration);
        }

        var mergedConfiguration = ProbeGlobalDictionaryHelper.MergeConfigurationPatches(
            Context,
            configuration,
            GetGlobalDictionaryReadRequests(configuration));

        Configuration = mergedConfiguration.BindToObject<TConfiguration>(GetConfigurationBinderOptions(), Context.Logger);

        var validationResults = new List<ValidationResult>();
        ValidationUtils.TryValidateObjectRecursive(Configuration, validationResults);
        if (validationResults.Count == 0)
        {
            SaveResolvedConfigurationDefaults();
        }

        return validationResults;
    }

    List<ValidationResult>? IHook.LoadAndValidateConfiguration(IConfiguration configuration)
    {
        return LoadAndValidateConfiguration(configuration);
    }

    /// <summary>
    /// Returns the ordered list of global-dictionary patches that participate in the configuration merge.
    /// The default configuration alias is resolved first, followed by any probe-specific recovery aliases.
    /// </summary>
    protected virtual IEnumerable<ProbeGlobalDictReadRequest> GetGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var defaultsAliasPath = GetConfigurationDefaultsAliasPath();
        if (defaultsAliasPath.Count != 0)
        {
            yield return new ProbeGlobalDictReadRequest(
                ProbeGlobalDictionaryHelper.ConfigurationDefaultsPayloadKey,
                defaultsAliasPath);
        }

        foreach (var readRequest in GetAdditionalGlobalDictionaryReadRequests(localConfiguration))
        {
            yield return readRequest;
        }
    }

    /// <summary>
    /// Returns the family-level alias that should always point at the latest resolved configuration for the current
    /// execution and session.
    /// </summary>
    protected virtual IReadOnlyList<string> GetConfigurationDefaultsAliasPath() => [];

    /// <summary>
    /// Allows concrete probes to request extra recovery payload patches, such as a saved list of deleted queues or a
    /// pre-mutation Kubernetes resource snapshot.
    /// </summary>
    protected virtual IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        return [];
    }

    /// <summary>
    /// Saves the resolved configuration under the current probe's canonical path and updates the family default alias.
    /// </summary>
    protected virtual void SaveResolvedConfigurationDefaults()
    {
        var defaultsAliasPath = GetConfigurationDefaultsAliasPath();
        if (defaultsAliasPath.Count == 0)
        {
            ProbeGlobalDictionaryHelper.SaveConfigurationDefaults(Context, Configuration);
            return;
        }

        ProbeGlobalDictionaryHelper.SaveConfigurationDefaults(Context, Configuration, defaultsAliasPath);
    }

    /// <summary>
    /// Saves a probe-specific recovery payload and updates the supplied aliases so related probes can consume it later.
    /// </summary>
    protected void SaveGlobalDictionaryPayload(string payloadKey, object payload, params IReadOnlyList<string>[] aliasPaths)
    {
        if (!Configuration.UseGlobalDict)
        {
            return;
        }

        ProbeGlobalDictionaryHelper.SavePayload(Context, payloadKey, payload, aliasPaths);
    }

    /// <summary>
    /// Builds an alias path under the probe-global-dictionary session scope.
    /// </summary>
    protected IReadOnlyList<string> BuildGlobalDictionaryAliasPath(params string[] aliasSegments)
        => ProbeGlobalDictionaryHelper.GetAliasPath(Context, aliasSegments);
}
