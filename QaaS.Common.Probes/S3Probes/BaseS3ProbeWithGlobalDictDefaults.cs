using System.Collections.Immutable;
using Amazon.S3;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.Configurations.CommonConfigurationObjects;
using QaaS.Framework.Protocols.Utils.S3Utils;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Shared S3 probe base that resolves missing endpoint and bucket settings from the probe global dictionary before
/// constructing the S3 clients.
/// </summary>
public abstract class BaseS3ProbeWithGlobalDictDefaults<TBaseS3ProbeConfig>
    : BaseProbeWithGlobalDictDefaults<TBaseS3ProbeConfig>
    where TBaseS3ProbeConfig : S3BucketConfig, IUseGlobalDictProbeConfiguration, new()
{
    protected IAmazonS3 S3Client = null!;
    protected IS3Client DataTransferS3Client = null!;

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("S3", "Defaults");

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        S3Client = new AmazonS3Client(Configuration.AccessKey,
            Configuration.SecretKey, new AmazonS3Config
            {
                ServiceURL = Configuration.ServiceURL,
                ForcePathStyle = Configuration.ForcePathStyle
            });
        DataTransferS3Client = new S3Client(S3Client, Context.Logger);
        RunS3Probe();
    }

    protected abstract void RunS3Probe();

    public void Dispose()
    {
        DataTransferS3Client.Dispose();
    }
}
