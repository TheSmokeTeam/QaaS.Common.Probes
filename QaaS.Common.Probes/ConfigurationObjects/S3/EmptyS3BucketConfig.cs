using System.ComponentModel;
using QaaS.Framework.Configurations.CommonConfigurationObjects;

namespace QaaS.Common.Probes.ConfigurationObjects.S3;

public record EmptyS3BucketConfig : S3BucketConfig
{
    [Description("Prefix of all objects to delete from s3 bucket"), DefaultValue("")]
    public string Prefix { get; set; } = "";
}