using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record OsExecuteCommandsInContainersConfig : OsProbeConfig
{
    [Required, Description("A list of the k8s labels of the pods to execute the command in, for example: app=test")]
    public string[]? ApplicationLabels { get; set; }

    [Required, Description("A list of the commands to execute in the chosen containers")]
    public string[]? Commands { get; set; }

    [Description("The name of the container to run the commands in in all the found pods," +
                 " if no name is given runs the command in all pod containers")]
    public string? ContainerName { get; set; }
}