using System.Data;
using Npgsql;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured PostgreSQL tables in the order they are listed.
/// </summary>
public class PostgreSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new NpgsqlConnection(Configuration.ConnectionString);
}
