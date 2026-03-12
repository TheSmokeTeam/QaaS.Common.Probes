using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.MongoDb;
using QaaS.Common.Probes.MongoDbProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class EmptyMongoDbCollectionTests
{
    private sealed class TestableEmptyMongoDbCollection(IMongoCollection<BsonDocument> collection)
        : EmptyMongoDbCollection
    {
        protected override IMongoCollection<BsonDocument> CreateCollection() => collection;
    }

    [Test]
    public void Run_ShouldDeleteAllDocumentsFromConfiguredCollection()
    {
        var collectionMock = new Mock<IMongoCollection<BsonDocument>>();
        var deleteResultMock = new Mock<DeleteResult>();
        deleteResultMock.SetupGet(result => result.IsAcknowledged).Returns(true);
        deleteResultMock.SetupGet(result => result.DeletedCount).Returns(7);
        collectionMock.Setup(collection => collection.DeleteMany(
                It.IsAny<FilterDefinition<BsonDocument>>(),
                It.IsAny<DeleteOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(deleteResultMock.Object);

        var probe = new TestableEmptyMongoDbCollection(collectionMock.Object)
        {
            Configuration = new EmptyMongoDbCollectionConfig
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "db",
                CollectionName = "col"
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        collectionMock.Verify(collection => collection.DeleteMany(
            It.IsAny<FilterDefinition<BsonDocument>>(),
            It.IsAny<DeleteOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
