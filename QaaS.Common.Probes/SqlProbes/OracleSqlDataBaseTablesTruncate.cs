using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured Oracle tables in the order they are listed.
/// </summary>
/// <qaas-docs group="SQL maintenance" subgroup="Oracle SQL tables" />
public class OracleSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbeWithGlobalDict
{
    protected override IDbConnection CreateDbConnection()
        => new OracleConnection(Configuration.ConnectionString);
}
