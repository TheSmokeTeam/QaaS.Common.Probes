using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;

namespace QaaS.Common.Probes.ElasticProbes;

/// <summary>
/// Deletes every Elasticsearch index that matches the configured index pattern.
/// </summary>
/// <qaas-docs group="Databases" subgroup="Elasticsearch" />
public class DeleteElasticIndices : BaseElasticProbe<DeleteElasticIndicesConfig>
{
    protected virtual IEnumerable<string> GetIndexNames()
    {
        return ElasticClient.Cat.Indices(new CatIndicesRequest(Configuration.IndexPattern!))
            .Records
            .Select(record => record.Index);
    }

    protected virtual DeleteIndexResponse DeleteIndex(string indexName)
    {
        return ElasticClient.Indices.Delete(indexName);
    }

    protected override void RunElasticProbe()
    {
        var indexNames = GetIndexNames().Distinct(StringComparer.Ordinal).ToArray();

        Context.Logger.LogInformation(
            "Found {NumberOfIndexes} indexes to delete with index pattern {DeletedIndexPattern}",
            indexNames.Length,
            Configuration.IndexPattern!);

        Parallel.ForEach(indexNames, indexName =>
        {
            var response = DeleteIndex(indexName);
            if (response?.ApiCall is not { Success: true } || response.ServerError is not null)
            {
                throw new Exception(
                    $"Failed to delete index {indexName} because {response?.DebugInformation ?? "<no response>"}");
            }

            Context.Logger.LogDebug("Index {DeletedIndex} deleted successfully", indexName);
        });

        Context.Logger.LogInformation(
            "All indexes under index pattern {DeletedIndexPattern} deleted successfully",
            Configuration.IndexPattern!);
    }
}
