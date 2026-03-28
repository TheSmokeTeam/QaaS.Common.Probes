using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using QaaS.Common.Probes.ConfigurationObjects.MongoDb;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.MongoDbProbes;

/// <summary>
/// Deletes all documents from the configured MongoDB collection so it starts clean for the test run.
/// </summary>
/// <qaas-docs group="Document stores" subgroup="MongoDB collections" />
public class EmptyMongoDbCollection : BaseProbe<EmptyMongoDbCollectionConfig>
{
    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var collection = CreateCollection();
        var deleteResult = collection.DeleteMany(FilterDefinition<BsonDocument>.Empty, new DeleteOptions());
        Context.Logger.LogInformation("Deleted {DeletedDocuments} documents from MongoDB collection {CollectionName}",
            deleteResult.IsAcknowledged ? deleteResult.DeletedCount : 0,
            Configuration.CollectionName);
    }

    protected virtual IMongoCollection<BsonDocument> CreateCollection()
    {
        var mongoClient = new MongoClient(Configuration.ConnectionString);
        var database = mongoClient.GetDatabase(Configuration.DatabaseName);
        return database.GetCollection<BsonDocument>(Configuration.CollectionName);
    }
}
