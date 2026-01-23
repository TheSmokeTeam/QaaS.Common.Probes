using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using QaaS.Common.Probes.ConfigurationObjects.Elastic;

namespace QaaS.Common.Probes.ElasticProbes;

/// <summary>
/// Empties elastic indices by their index pattern
/// </summary>
public class EmptyElasticIndices : BaseElasticProbe<EmptyElasticIndicesConfig>
{
    /// <summary>
    /// Returns all the indexes relevant to the configured index pattern
    /// </summary>
    protected virtual IEnumerable<string> GetIndexNames()
    {
        return ElasticClient.Cat.Indices(
                new CatIndicesRequest(Configuration.IndexPattern!))
            .Records
            .Select(record => record.Index);
    }

    /// <inheritdoc />
    protected override void RunElasticProbe()
    {
        // Get all relevant index names
        var indexNames = GetIndexNames().ToArray();

        Context.Logger.LogInformation(
            "Found {NumberOfIndexes} indexes to empty with index pattern {EmptiedIndexPattern}",
            indexNames.Length, Configuration.IndexPattern!);

        Parallel.ForEach(indexNames, index =>
        {
            var response = ElasticClient.DeleteByQuery<object>(d => d
                .Index(index)
                .Query(q => q
                    .QueryString(qs => qs
                        .Query(Configuration.MatchQueryString)))
                .Conflicts(Conflicts.Proceed));
            if (response is not { IsValid: true } || response.ApiCall is not { Success: true })
                throw new Exception(
                    $"Failed to empty index {index} because {response}");

            Context.Logger.LogDebug("Index {EmptiedIndex} emptied successfully",
                index);
        });
        Context.Logger.LogInformation("All indexes under index pattern {EmptiedIndexPattern} emptied successfully",
            Configuration.IndexPattern!);
    }
}