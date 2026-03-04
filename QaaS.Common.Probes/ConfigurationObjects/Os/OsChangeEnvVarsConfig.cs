using System.ComponentModel;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsChangeEnvVarsConfig : OsUpdatePodsProbeConfig
{
    [Description("The name of the container we would like to update, if not given - the probe will update all of the " +
                 "pod's containers")]
    public string? ContainerName { get; set; }

    [Description("The environment variables to update/add")]
    public Dictionary<string, string?> EnvVarsToUpdate { get; set; } = [];

    [Description("The environment variables to remove")]
    public List<string> EnvVarsToRemove { get; set; } = [];
}
