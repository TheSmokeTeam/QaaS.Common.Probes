using System.Collections.Immutable;
using Npgsql;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Common.Probes.SqlProbes;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class PostgreSqlDataBaseTablesTruncateIntegrationTests
{
    private const string PostgisConnectionStringEnvironmentVariableName = "QAAS_POSTGIS_CONNECTION_STRING";

    [Test]
    public void PostgreSqlDataBaseTablesTruncate_TruncatesTablesContainingGeometryColumns()
    {
        var connectionString = GetPostgisConnectionStringOrIgnore();
        var tableName = $"public.qaas_probe_geometry_{Guid.NewGuid():N}";

        try
        {
            using var setupConnection = new NpgsqlConnection(connectionString);
            setupConnection.Open();

            using (var setupCommand = setupConnection.CreateCommand())
            {
                setupCommand.CommandText = $"""
                                           CREATE EXTENSION IF NOT EXISTS postgis;
                                           DROP TABLE IF EXISTS {tableName};
                                           CREATE TABLE {tableName}
                                           (
                                               id integer NOT NULL,
                                               shape geometry(Polygon, 4326) NOT NULL
                                           );
                                           INSERT INTO {tableName} (id, shape)
                                           VALUES (1, ST_GeomFromText('POLYGON((35 31,35 32,36 32,36 31,35 31))', 4326));
                                           """;
                setupCommand.ExecuteNonQuery();
            }

            var probe = new PostgreSqlDataBaseTablesTruncate
            {
                Configuration = new SqlDataBaseTablesTruncateProbeConfig
                {
                    ConnectionString = connectionString,
                    TableNames = [tableName]
                },
                Context = Globals.Context
            };

            probe.Run(ImmutableList<SessionData>.Empty, ImmutableList<DataSource>.Empty);
            probe.Dispose();

            using var verifyCommand = setupConnection.CreateCommand();
            verifyCommand.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            var rowCount = Convert.ToInt32(verifyCommand.ExecuteScalar());

            Assert.That(rowCount, Is.Zero);
        }
        finally
        {
            using var cleanupConnection = new NpgsqlConnection(connectionString);
            cleanupConnection.Open();
            using var cleanupCommand = cleanupConnection.CreateCommand();
            cleanupCommand.CommandText = $"DROP TABLE IF EXISTS {tableName};";
            cleanupCommand.ExecuteNonQuery();
        }
    }

    private static string GetPostgisConnectionStringOrIgnore()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgisConnectionStringEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore(
                $"Set {PostgisConnectionStringEnvironmentVariableName} to run PostgreSQL/PostGIS integration tests.");

        return connectionString;
    }
}
