using System.Reflection;
using Elasticsearch.Net;
using Moq;
using Nest;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;
using QaaS.Common.Probes.ElasticProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class EmptyElasticIndicesTests
{
    private class EmptyElasticIndicesMock(IEnumerable<string> mockIndexNames) : EmptyElasticIndices
    {
        protected override IEnumerable<string> GetIndexNames() => mockIndexNames;
    }

    private readonly FieldInfo _apiCallFieldInfo =
        typeof(ResponseBase).GetField("_originalApiCall", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly FieldInfo _elasticClientFieldInfo =
        typeof(BaseElasticProbe<EmptyElasticIndicesConfig>).GetField("ElasticClient",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly MethodInfo _runElasticProbeMethod = typeof(EmptyElasticIndices)
        .GetMethod("RunElasticProbe", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Test, TestCase(0), TestCase(1), TestCase(2)]
    public void TestRunProbe_CallFunctionWithMockElasticClient_ShouldInvokeDeleteByQueryOnceForEachIndexInIndexPattern(
        int numberOfIndexesInIndexPattern)
    {
        // Arrange
        var indexes = new List<string>();
        for (var indexCount = 0; indexCount < numberOfIndexesInIndexPattern; indexCount++)
        {
            indexes.Add($"index-{indexCount}");
        }

        var deleteByQueryResponse = new DeleteByQueryResponse();
        _apiCallFieldInfo.SetValue(deleteByQueryResponse, new ApiCallDetails { Success = true });

        var mockElasticClient = new Mock<IElasticClient>();
        mockElasticClient.Setup(m => m.DeleteByQuery(
                It.IsAny<Func<DeleteByQueryDescriptor<dynamic>, IDeleteByQueryRequest>>()))
            .Returns(deleteByQueryResponse);

        var probe = new EmptyElasticIndicesMock(indexes)
        {
            Configuration = new EmptyElasticIndicesConfig
            {
                Url = "http://localhost:6969",
                Username = "username",
                Password = "password",
                RequestTimeoutMs = 0,
                IndexPattern = "*",
                MatchQueryString = "*",
            },
            Context = Globals.Context
        };

        _elasticClientFieldInfo.SetValue(probe, mockElasticClient.Object);

        // Act
        _runElasticProbeMethod.Invoke(probe, null);

        // Assert
        mockElasticClient.Verify(
            m => m.DeleteByQuery(It.IsAny<Func<DeleteByQueryDescriptor<dynamic>, IDeleteByQueryRequest>>()),
            Times.Exactly(numberOfIndexesInIndexPattern));
    }

    [Test, TestCase(1), TestCase(2)]
    public void
        TestRunProbeWithUnSuccessfulResponse_CallFunctionWithMockElasticClientThatReturnsUnsuccessfulResponse_ShouldThrowException(
            int numberOfIndexesInIndexPattern)
    {
        // Arrange
        var indexes = new List<string>();
        for (var indexCount = 0; indexCount < numberOfIndexesInIndexPattern; indexCount++)
        {
            indexes.Add($"index-{indexCount}");
        }

        var mockResponse = new Mock<DeleteByQueryResponse>();
        mockResponse.Setup(r => r.IsValid).Returns(false);
        mockResponse.Setup(r => r.ApiCall).Returns(new ApiCallDetails { Success = false });
        mockResponse.Setup(r => r.ToString()).Returns("DeleteByQuery failed");

        var mockElasticClient = new Mock<IElasticClient>();
        mockElasticClient.Setup(m => m.DeleteByQuery(
                It.IsAny<Func<DeleteByQueryDescriptor<dynamic>, IDeleteByQueryRequest>>()))
            .Returns(mockResponse.Object);

        var probe = new EmptyElasticIndicesMock(indexes)
        {
            Configuration = new EmptyElasticIndicesConfig
            {
                Url = "http://localhost:6969",
                Username = "username",
                Password = "password",
                RequestTimeoutMs = 0,
                IndexPattern = "*",
                MatchQueryString = "*",
            },
            Context = Globals.Context
        };

        _elasticClientFieldInfo.SetValue(probe, mockElasticClient.Object);

        // Act + Assert
        Assert.Throws<AggregateException>(() =>
        {
            try
            {
                _runElasticProbeMethod.Invoke(probe, null);
            }
            catch (Exception e)
            {
                // Inner exception should be custom thrown exception after the parallel foreach caught it
                throw e.InnerException!;
            }
        });
    }

    [Test]
    public void TestRunProbeWithNullResponse_CallFunctionWithMockElasticClientThatReturnsNull_ShouldThrowException()
    {
        // Arrange
        var indexes = new List<string> { "index-1" };

        var mockElasticClient = new Mock<IElasticClient>();
        mockElasticClient.Setup(m => m.DeleteByQuery(
                It.IsAny<Func<DeleteByQueryDescriptor<dynamic>, IDeleteByQueryRequest>>()))
            .Returns((DeleteByQueryResponse)null!);

        var probe = new EmptyElasticIndicesMock(indexes)
        {
            Configuration = new EmptyElasticIndicesConfig
            {
                Url = "http://localhost:6969",
                Username = "username",
                Password = "password",
                RequestTimeoutMs = 0,
                IndexPattern = "*",
                MatchQueryString = "*",
            },
            Context = Globals.Context
        };

        _elasticClientFieldInfo.SetValue(probe, mockElasticClient.Object);

        // Act + Assert
        Assert.Throws<AggregateException>(() =>
        {
            try
            {
                _runElasticProbeMethod.Invoke(probe, null);
            }
            catch (Exception e)
            {
                throw e.InnerException!;
            }
        });
    }
}
