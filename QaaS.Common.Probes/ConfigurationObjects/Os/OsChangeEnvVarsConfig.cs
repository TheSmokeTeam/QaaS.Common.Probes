using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

/// <summary>
/// Configuration for OpenShift probes that add, update, remove, or fully restore container environment variables.
/// </summary>
public record OsChangeEnvVarsConfig : OsUpdatePodsProbeConfig
{
    [Description("The name of the container we would like to update, if not given - the probe will update all of the " +
                 "pod's containers")]
    public string? ContainerName { get; set; }

    [Description("The environment variables to update/add")]
    public Dictionary<string, string?> EnvVarsToUpdate { get; set; } = [];

    [Description("The environment variables to remove")]
    public List<string> EnvVarsToRemove { get; set; } = [];

    [Description("Optional exact environment snapshot keyed by container name. When present, the probe restores each listed container to the provided environment instead of applying the broad update/remove rules.")]
    public Dictionary<string, Dictionary<string, string?>>? ContainerEnvVarsToUpdate { get; set; }
}
