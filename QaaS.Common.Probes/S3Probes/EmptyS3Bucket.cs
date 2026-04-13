using Amazon.S3;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.S3;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Deletes objects from the configured S3 bucket, optionally constrained to a prefix, while treating a missing bucket as a no-op.
/// </summary>
/// <qaas-docs group="Databases" subgroup="S3" />
public class EmptyS3Bucket : BaseS3ProbeWithGlobalDict<EmptyS3BucketConfig>
{
    /// <summary>
    /// Removes the matching objects from the configured bucket without deleting the bucket itself.
    /// </summary>
    protected override void RunS3Probe()
    {
        try
        {
            var deleteObjectsResponses =
                DataTransferS3Client.EmptyS3Bucket(Configuration.StorageBucket!,
                    Configuration.Prefix).Result.ToList();
            Context.Logger.LogInformation("Emptied s3 bucket {S3BucketName} of {NumberOfObjects} objects with prefix " +
                                          "{PrefixOfObjectsToDelete} successfully",
                Configuration.StorageBucket, deleteObjectsResponses.Sum(res
                    => res.DeletedObjects.Count), Configuration.Prefix);
        }
        catch (AggregateException exception)
            when (exception.InnerException is AmazonS3Exception amazonS3Exception &&
                  IsMissingBucket(amazonS3Exception))
        {
            Context.Logger.LogError(
                "S3 bucket {S3BucketName} that should be emptied by probe does not exist in the first place",
                Configuration.StorageBucket);
        }
    }

    private static bool IsMissingBucket(AmazonS3Exception exception)
        => exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
           string.Equals(exception.ErrorCode, "NoSuchBucket", StringComparison.Ordinal);
}
