using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured Oracle tables in the order they are listed.
/// </summary>
public class OracleSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new OracleConnection(Configuration.ConnectionString);
}
