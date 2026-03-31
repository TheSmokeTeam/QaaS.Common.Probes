using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using QaaS.Common.Probes.ConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.Sql;

public record SqlDataBaseTablesTruncateProbeConfig : IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing SQL probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Required, Description("The connection string to the database")]
    public string? ConnectionString { get; set; }

    [Required, Description(
         "The names of all the tables to truncate, they will be truncated by the order they are given in this list")]
    public string[]? TableNames { get; set; }

    [Range(1, int.MaxValue), Description(
         "The wait time (in seconds) before terminating the attempt to execute the truncate command and generating an error"),
     DefaultValue(30)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
