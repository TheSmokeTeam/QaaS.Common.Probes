using System.Data;
using Npgsql;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured PostgreSQL tables in the order they are listed.
/// </summary>
/// <qaas-docs group="SQL maintenance" subgroup="PostgreSQL tables" />
public class PostgreSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbeWithGlobalDictDefaults
{
    protected override IDbConnection CreateDbConnection()
        => new NpgsqlConnection(Configuration.ConnectionString);
}
