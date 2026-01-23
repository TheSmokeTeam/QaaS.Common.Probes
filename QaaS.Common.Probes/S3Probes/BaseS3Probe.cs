using System.Collections.Immutable;
using Amazon.S3;
using QaaS.Framework.Configurations.CommonConfigurationObjects;
using QaaS.Framework.Protocols.Utils.S3Utils;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.S3Probes;

public abstract class BaseS3Probe<TBaseS3ProbeConfig> : BaseProbe<TBaseS3ProbeConfig>
    where TBaseS3ProbeConfig : S3BucketConfig, new()
{
    protected IAmazonS3 S3Client = null!;
    protected IS3Client DataTransferS3Client = null!;

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
        DataTransferS3Client.Dispose(); // Disposes of S3Client used and itself
    }
}