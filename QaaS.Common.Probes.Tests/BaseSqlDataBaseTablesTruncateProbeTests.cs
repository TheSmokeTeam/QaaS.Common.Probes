using System.Collections.Immutable;
using System.Data;
using Moq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Common.Probes.SqlProbes;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.Tests;

/// <summary>
/// Class for testing `BaseSqlDataBaseTablesTruncateProbe` functionalities 
/// </summary>
internal class MockSqlDataBaseTablesTruncateProbe(IDbConnection dbConnection) : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
    {
        return dbConnection; // Return the mocked connection
    }
}

[TestFixture]
public class BaseSqlDataBaseTablesTruncateProbeTests
{
    private static Mock<IDbConnection>? _dbConnectionMock;
    private static Mock<IDbCommand>? _dbCommandMock;

    [SetUp]
    public void SetUp()
    {
        _dbCommandMock = new Mock<IDbCommand>();
        _dbCommandMock.Setup(mock => mock.ExecuteNonQuery()).Verifiable();

        _dbConnectionMock = new Mock<IDbConnection>();
        _dbConnectionMock.Setup(mock => mock.CreateCommand()).Returns(_dbCommandMock.Object)
            .Verifiable();
    }

    public static IEnumerable<TestCaseData> TestTruncateTableUnlessDisabledDataSource()
    {
        yield return new TestCaseData(new SqlDataBaseTablesTruncateProbeConfig { TableNames = [] }, Times.Never())
            .SetName("Truncate0Tables");
        yield return new TestCaseData(new SqlDataBaseTablesTruncateProbeConfig { TableNames = ["table1"] }, Times.Once())
            .SetName("TruncateOneTable");
        yield return new TestCaseData(new SqlDataBaseTablesTruncateProbeConfig { TableNames = ["table1", "table2", "table3"] },
            Times.Exactly(3)).SetName("TruncateMultipleTables");
    }

    [Test, TestCaseSource(nameof(TestTruncateTableUnlessDisabledDataSource))]
    public void TestTruncateTablesRunProbe_CallFunctionRunProbe_ShouldTruncateGivenNumberOfTables(
        SqlDataBaseTablesTruncateProbeConfig config, Times executedNonQueryCount)
    {
        // Arrange
        var mockBaseSqlDataBaseConsumer = new MockSqlDataBaseTablesTruncateProbe(_dbConnectionMock!.Object);
        mockBaseSqlDataBaseConsumer.Configuration = config;
        mockBaseSqlDataBaseConsumer.Context = Globals.Context;

        // Act
        mockBaseSqlDataBaseConsumer.Run(new List<SessionData>().ToImmutableList(),
            new List<DataSource>().ToImmutableList());

        // Assert
        _dbCommandMock!.Verify(mock => mock.ExecuteNonQuery(), executedNonQueryCount);
    }

    [Test]
    public void TestTruncateTablesRunProbe_WhenConnectionClosed_ShouldSetCommandAndOpenAndCloseConnection()
    {
        // Arrange
        const int commandTimeout = 42;
        var commandMock = new Mock<IDbCommand>();
        commandMock.Setup(mock => mock.ExecuteNonQuery()).Returns(1);

        var connectionMock = new Mock<IDbConnection>();
        connectionMock.Setup(mock => mock.CreateCommand()).Returns(commandMock.Object);
        connectionMock.SetupSequence(mock => mock.State)
            .Returns(ConnectionState.Closed)
            .Returns(ConnectionState.Open);

        var probe = new MockSqlDataBaseTablesTruncateProbe(connectionMock.Object)
        {
            Configuration = new SqlDataBaseTablesTruncateProbeConfig
            {
                TableNames = ["users"],
                CommandTimeoutSeconds = commandTimeout
            },
            Context = Globals.Context
        };

        // Act
        probe.Run(new List<SessionData>().ToImmutableList(), new List<DataSource>().ToImmutableList());

        // Assert
        commandMock.VerifySet(mock => mock.CommandText = "TRUNCATE TABLE users", Times.Once);
        commandMock.VerifySet(mock => mock.CommandTimeout = commandTimeout, Times.Once);
        commandMock.Verify(mock => mock.ExecuteNonQuery(), Times.Once);
        connectionMock.Verify(mock => mock.Open(), Times.Once);
        connectionMock.Verify(mock => mock.Close(), Times.Once);
    }
}
