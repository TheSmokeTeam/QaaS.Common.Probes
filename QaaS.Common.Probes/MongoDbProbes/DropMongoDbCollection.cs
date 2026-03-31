using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using QaaS.Common.Probes.ConfigurationObjects.MongoDb;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.MongoDbProbes;

/// <summary>
/// Drops the configured MongoDB collection so a later run can recreate it from scratch.
/// </summary>
/// <qaas-docs group="Databases" subgroup="MongoDB" />
public class DropMongoDbCollection : BaseMongoDbProbeWithGlobalDictDefaults<DropMongoDbCollectionConfig>
{
    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var database = CreateDatabase();
        database.DropCollection(Configuration.CollectionName);
        Context.Logger.LogInformation("Dropped MongoDB collection {CollectionName}", Configuration.CollectionName);
    }

    protected virtual IMongoDatabase CreateDatabase()
    {
        var mongoClient = new MongoClient(Configuration.ConnectionString);
        return mongoClient.GetDatabase(Configuration.DatabaseName);
    }
}
