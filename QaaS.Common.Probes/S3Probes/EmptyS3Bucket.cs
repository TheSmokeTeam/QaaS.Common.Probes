using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.S3;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Probe to empty a s3 bucket according to a certain prefix
/// </summary>
/// <qaas-docs group="Databases" subgroup="S3" />
public class EmptyS3Bucket : BaseS3Probe<EmptyS3BucketConfig>
{
    protected override void RunS3Probe()
    {
        if (S3Client.ListBucketsAsync().Result.Buckets.All(bucket => bucket.BucketName != Configuration.StorageBucket))
        {
            Context.Logger.LogError(
                "S3 bucket {S3BucketName} that should be emptied by probe does not exist in the first place"
                , Configuration.StorageBucket);
            return;
        }

        var deleteObjectsResponses =
            DataTransferS3Client.EmptyS3Bucket(Configuration.StorageBucket!,
                Configuration.Prefix).Result.ToList();
        Context.Logger.LogInformation("Emptied s3 bucket {S3BucketName} of {NumberOfObjects} objects with prefix " +
                                      "{PrefixOfObjectsToDelete} successfully",
            Configuration.StorageBucket, deleteObjectsResponses.Sum(res
                => res.DeletedObjects.Count), Configuration.Prefix);
    }
}
