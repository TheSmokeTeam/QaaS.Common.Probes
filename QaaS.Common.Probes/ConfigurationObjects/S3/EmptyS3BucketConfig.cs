using System.ComponentModel;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Framework.Configurations.CommonConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.S3;

public record EmptyS3BucketConfig : S3BucketConfig, IUseGlobalDictProbeConfiguration
{
    [Description("When true, missing S3 probe configuration keys can be resolved from the shared global dictionary before local values are applied."),
     DefaultValue(false)]
    public bool UseGlobalDict { get; set; }

    [Description("Prefix of all objects to delete from s3 bucket"), DefaultValue("")]
    public string Prefix { get; set; } = "";
}
