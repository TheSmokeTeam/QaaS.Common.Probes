using System.Reflection;
using Elasticsearch.Net;
using Nest;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;
using QaaS.Common.Probes.ElasticProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class DeleteElasticIndicesTests
{
    private sealed class TestableDeleteElasticIndices(
        IEnumerable<string> indexNames,
        Func<string, DeleteIndexResponse?> deleteIndex)
        : DeleteElasticIndices
    {
        protected override IEnumerable<string> GetIndexNames() => indexNames;

        protected override DeleteIndexResponse DeleteIndex(string indexName)
        {
            return deleteIndex(indexName)!;
        }
    }

    private readonly FieldInfo _apiCallFieldInfo =
        typeof(ResponseBase).GetField("_originalApiCall", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Test]
    public void Run_WhenResponsesAreSuccessful_ShouldDeleteEveryResolvedIndex()
    {
        var deletedIndexes = new List<string>();
        var probe = new TestableDeleteElasticIndices(
            ["orders-1", "orders-2"],
            indexName =>
            {
                deletedIndexes.Add(indexName);
                var response = new DeleteIndexResponse();
                _apiCallFieldInfo.SetValue(response, new ApiCallDetails { Success = true });
                return response;
            })
        {
            Configuration = new DeleteElasticIndicesConfig
            {
                Url = "http://localhost:9200",
                Username = "username",
                Password = "password",
                RequestTimeoutMs = 1000,
                IndexPattern = "orders-*"
            },
            Context = Globals.Context
        };

        Assert.DoesNotThrow(() => probe.Run([], []));
        Assert.That(deletedIndexes, Is.EquivalentTo(new[] { "orders-1", "orders-2" }));
    }

    [Test]
    public void Run_WhenDeleteResponseIsNotSuccessful_ShouldThrowAggregateException()
    {
        var probe = new TestableDeleteElasticIndices(
            ["orders-1"],
            _ =>
            {
                var response = new DeleteIndexResponse();
                _apiCallFieldInfo.SetValue(response, new ApiCallDetails { Success = false });
                return response;
            })
        {
            Configuration = new DeleteElasticIndicesConfig
            {
                Url = "http://localhost:9200",
                Username = "username",
                Password = "password",
                RequestTimeoutMs = 1000,
                IndexPattern = "orders-*"
            },
            Context = Globals.Context
        };

        Assert.Throws<AggregateException>(() => probe.Run([], []));
    }
}
