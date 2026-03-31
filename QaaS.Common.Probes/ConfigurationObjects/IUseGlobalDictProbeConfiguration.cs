using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects;

/// <summary>
/// Enables opt-in fallback from the shared global dictionary when a probe omits configuration keys locally.
/// When <see cref="UseGlobalDict"/> is <see langword="false"/>, probes keep their existing configuration loading
/// behavior and ignore all probe-global-dictionary defaults and recovery aliases.
/// </summary>
public interface IUseGlobalDictProbeConfiguration
{
    /// <summary>
    /// When true, the probe first loads matching defaults and recovery patches from the shared global dictionary and
    /// only then overlays the raw YAML/code configuration on top of them.
    /// </summary>
    [Description("When true, missing probe configuration keys may be resolved from the shared global dictionary before local YAML/code values are applied."),
     DefaultValue(false)]
    bool UseGlobalDict { get; set; }
}
