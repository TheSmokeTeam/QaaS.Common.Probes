using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.S3;
using QaaS.Common.Probes.S3Probes;
using QaaS.Framework.Protocols.Utils.S3Utils;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class S3ProbesTests
{
    private sealed class TestableEmptyS3Bucket : EmptyS3Bucket
    {
        public void SetClients(IAmazonS3 s3Client, IS3Client dataTransferS3Client)
        {
            S3Client = s3Client;
            DataTransferS3Client = dataTransferS3Client;
        }

        public void InvokeRunS3Probe() => RunS3Probe();
    }

    private sealed class TestableDeleteS3Bucket : DeleteS3Bucket
    {
        public void SetClients(IAmazonS3 s3Client, IS3Client dataTransferS3Client)
        {
            S3Client = s3Client;
            DataTransferS3Client = dataTransferS3Client;
        }

        public void InvokeRunS3Probe() => RunS3Probe();
    }

    [Test]
    public void TestEmptyS3Bucket_WhenBucketDoesNotExist_ShouldNotAttemptToDeleteObjects()
    {
        // Arrange
        var s3ClientMock = new Mock<IAmazonS3>();
        s3ClientMock.Setup(m => m.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new S3Bucket { BucketName = "other-bucket" }]
            });

        var dataTransferMock = new Mock<IS3Client>();

        var probe = new TestableEmptyS3Bucket
        {
            Configuration = new EmptyS3BucketConfig
            {
                StorageBucket = "target-bucket",
                Prefix = "prefix/"
            },
            Context = Globals.Context
        };
        probe.SetClients(s3ClientMock.Object, dataTransferMock.Object);

        // Act
        probe.InvokeRunS3Probe();

        // Assert
        dataTransferMock.Verify(m => m.EmptyS3Bucket(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void TestEmptyS3Bucket_WhenBucketExists_ShouldDeleteObjectsByPrefix()
    {
        // Arrange
        var s3ClientMock = new Mock<IAmazonS3>();
        s3ClientMock.Setup(m => m.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new S3Bucket { BucketName = "target-bucket" }]
            });

        var dataTransferMock = new Mock<IS3Client>();
        dataTransferMock.Setup(m => m.EmptyS3Bucket(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DeleteObjectsResponse>
            {
                new()
                {
                    DeletedObjects =
                    [
                        new DeletedObject(),
                        new DeletedObject()
                    ]
                }
            });

        var probe = new TestableEmptyS3Bucket
        {
            Configuration = new EmptyS3BucketConfig
            {
                StorageBucket = "target-bucket",
                Prefix = "prefix/"
            },
            Context = Globals.Context
        };
        probe.SetClients(s3ClientMock.Object, dataTransferMock.Object);

        // Act
        probe.InvokeRunS3Probe();

        // Assert
        dataTransferMock.Verify(m => m.EmptyS3Bucket("target-bucket", "prefix/"), Times.Once);
    }

    [Test]
    public void TestDeleteS3Bucket_WhenBucketDoesNotExist_ShouldNotDeleteObjectsOrBucket()
    {
        // Arrange
        var s3ClientMock = new Mock<IAmazonS3>();
        s3ClientMock.Setup(m => m.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new S3Bucket { BucketName = "other-bucket" }]
            });

        var dataTransferMock = new Mock<IS3Client>();

        var probe = new TestableDeleteS3Bucket
        {
            Configuration = new DeleteS3BucketConfig
            {
                StorageBucket = "target-bucket"
            },
            Context = Globals.Context
        };
        probe.SetClients(s3ClientMock.Object, dataTransferMock.Object);

        // Act
        probe.InvokeRunS3Probe();

        // Assert
        dataTransferMock.Verify(m => m.EmptyS3Bucket(It.IsAny<string>()), Times.Never);
        s3ClientMock.Verify(m => m.DeleteBucketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void TestDeleteS3Bucket_WhenBucketExists_ShouldEmptyAndDeleteBucket()
    {
        // Arrange
        var s3ClientMock = new Mock<IAmazonS3>();
        s3ClientMock.Setup(m => m.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = [new S3Bucket { BucketName = "target-bucket" }]
            });
        s3ClientMock.Setup(m => m.DeleteBucketAsync("target-bucket", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteBucketResponse { HttpStatusCode = HttpStatusCode.OK });

        var dataTransferMock = new Mock<IS3Client>();
        dataTransferMock.Setup(m => m.EmptyS3Bucket("target-bucket"))
            .ReturnsAsync(new List<DeleteObjectsResponse>
            {
                new()
                {
                    DeletedObjects =
                    [
                        new DeletedObject()
                    ]
                }
            });

        var probe = new TestableDeleteS3Bucket
        {
            Configuration = new DeleteS3BucketConfig
            {
                StorageBucket = "target-bucket"
            },
            Context = Globals.Context
        };
        probe.SetClients(s3ClientMock.Object, dataTransferMock.Object);

        // Act
        probe.InvokeRunS3Probe();

        // Assert
        dataTransferMock.Verify(m => m.EmptyS3Bucket("target-bucket"), Times.Once);
        s3ClientMock.Verify(m => m.DeleteBucketAsync("target-bucket", It.IsAny<CancellationToken>()), Times.Once);
    }
}
