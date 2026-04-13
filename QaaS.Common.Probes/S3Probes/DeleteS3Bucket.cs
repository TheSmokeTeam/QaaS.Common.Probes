using Amazon.S3;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.S3;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Empties the configured S3 bucket and deletes it, treating a missing bucket as an already-satisfied state.
/// </summary>
/// <qaas-docs group="Databases" subgroup="S3" />
public class DeleteS3Bucket : BaseS3ProbeWithGlobalDict<DeleteS3BucketConfig>
{
    /// <summary>
    /// Deletes all objects from the configured bucket before deleting the bucket itself.
    /// </summary>
    protected override void RunS3Probe()
    {
        try
        {
            var deleteObjectsResponses =
                DataTransferS3Client.EmptyS3Bucket(Configuration.StorageBucket!).Result.ToList();
            Context.Logger.LogInformation("Emptied s3 bucket {S3BucketName} of {NumberOfObjects} objects successfully",
                Configuration.StorageBucket, deleteObjectsResponses.Sum(res
                    => res.DeletedObjects.Count));

            var bucketDeletionResponse = S3Client.DeleteBucketAsync(Configuration.StorageBucket).Result;
            Context.Logger.LogInformation("Deleted s3 bucket {S3BucketName}," +
                                          " http response status code is {HttpResponseStatusCode}",
                Configuration.StorageBucket, bucketDeletionResponse.HttpStatusCode);
        }
        catch (AggregateException exception)
            when (exception.InnerException is AmazonS3Exception amazonS3Exception &&
                  IsMissingBucket(amazonS3Exception))
        {
            Context.Logger.LogError(
                "S3 bucket {S3BucketName} that should be deleted by probe does not exist in the first place",
                Configuration.StorageBucket);
        }
    }

    private static bool IsMissingBucket(AmazonS3Exception exception)
        => exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
           string.Equals(exception.ErrorCode, "NoSuchBucket", StringComparison.Ordinal);
}
