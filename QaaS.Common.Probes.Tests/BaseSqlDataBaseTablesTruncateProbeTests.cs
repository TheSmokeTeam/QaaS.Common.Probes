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
        yield return new TestCaseData(new SqlDataBaseTablesTruncateProbeConfig { TableNames = ["1"] }, Times.Once())
            .SetName("TruncateOneTable");
        yield return new TestCaseData(new SqlDataBaseTablesTruncateProbeConfig { TableNames = ["1", "2", "3"] },
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
}