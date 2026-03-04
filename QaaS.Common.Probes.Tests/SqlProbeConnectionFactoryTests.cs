using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Common.Probes.SqlProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class SqlProbeConnectionFactoryTests
{
    private sealed class TestableMsSqlProbe : MsSqlDataBaseTablesTruncate
    {
        public IDbConnection InvokeCreateDbConnection() => CreateDbConnection();
    }

    private sealed class TestablePostgreProbe : PostgreSqlDataBaseTablesTruncate
    {
        public IDbConnection InvokeCreateDbConnection() => CreateDbConnection();
    }

    private sealed class TestableOracleProbe : OracleSqlDataBaseTablesTruncate
    {
        public IDbConnection InvokeCreateDbConnection() => CreateDbConnection();
    }

    [Test]
    public void TestMsSqlCreateConnection_ShouldReturnSqlConnectionWithConfiguredConnectionString()
    {
        // Arrange
        var probe = new TestableMsSqlProbe
        {
            Configuration = new SqlDataBaseTablesTruncateProbeConfig
            {
                ConnectionString = "Server=.;Database=db;User Id=u;Password=p;",
                TableNames = []
            },
            Context = Globals.Context
        };

        // Act
        using var connection = probe.InvokeCreateDbConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<SqlConnection>());
        Assert.That(((SqlConnection)connection).ConnectionString, Is.EqualTo(probe.Configuration.ConnectionString));
    }

    [Test]
    public void TestPostgreCreateConnection_ShouldReturnNpgsqlConnectionWithConfiguredConnectionString()
    {
        // Arrange
        var probe = new TestablePostgreProbe
        {
            Configuration = new SqlDataBaseTablesTruncateProbeConfig
            {
                ConnectionString = "Host=localhost;Database=db;Username=u;Password=p",
                TableNames = []
            },
            Context = Globals.Context
        };

        // Act
        using var connection = probe.InvokeCreateDbConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<NpgsqlConnection>());
        Assert.That(((NpgsqlConnection)connection).ConnectionString, Is.EqualTo(probe.Configuration.ConnectionString));
    }

    [Test]
    public void TestOracleCreateConnection_ShouldReturnOracleConnectionWithConfiguredConnectionString()
    {
        // Arrange
        var probe = new TestableOracleProbe
        {
            Configuration = new SqlDataBaseTablesTruncateProbeConfig
            {
                ConnectionString = "User Id=user;Password=password;Data Source=localhost/XEPDB1",
                TableNames = []
            },
            Context = Globals.Context
        };

        // Act
        using var connection = probe.InvokeCreateDbConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<OracleConnection>());
        Assert.That(((OracleConnection)connection).ConnectionString, Is.EqualTo(probe.Configuration.ConnectionString));
    }
}
