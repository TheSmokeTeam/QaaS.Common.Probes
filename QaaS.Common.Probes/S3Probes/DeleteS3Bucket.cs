using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.S3;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Probe to delete a s3 bucket
/// </summary>
/// <qaas-docs group="Databases" subgroup="S3" />
public class DeleteS3Bucket : BaseS3Probe<DeleteS3BucketConfig>
{
    protected override void RunS3Probe()
    {
        if (S3Client.ListBucketsAsync().Result.Buckets.All(bucket => bucket.BucketName != Configuration.StorageBucket))
        {
            Context.Logger.LogError(
                "S3 bucket {S3BucketName} that should be deleted by probe does not exist in the first place"
                , Configuration.StorageBucket);
            return;
        }

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
}
