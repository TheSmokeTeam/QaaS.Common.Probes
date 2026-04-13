using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.S3;

namespace QaaS.Common.Probes.S3Probes;

/// <summary>
/// Ensures the configured S3 bucket exists by creating it when it is missing.
/// </summary>
/// <qaas-docs group="Databases" subgroup="S3" />
public class CreateS3Bucket : BaseS3ProbeWithGlobalDict<CreateS3BucketConfig>
{
    protected override void RunS3Probe()
    {
        try
        {
            var response = S3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = Configuration.StorageBucket
            }).Result;

            Context.Logger.LogInformation(
                "Created S3 bucket {S3BucketName}, http response status code is {HttpResponseStatusCode}",
                Configuration.StorageBucket,
                response.HttpStatusCode);
        }
        catch (AggregateException exception)
            when (exception.InnerException is AmazonS3Exception amazonS3Exception &&
                  IsBucketAlreadyOwnedByCaller(amazonS3Exception))
        {
            Context.Logger.LogInformation("S3 bucket {S3BucketName} already exists", Configuration.StorageBucket);
        }
    }

    private static bool IsBucketAlreadyOwnedByCaller(AmazonS3Exception exception)
        => exception.StatusCode == System.Net.HttpStatusCode.Conflict &&
           string.Equals(exception.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.Ordinal);
}
