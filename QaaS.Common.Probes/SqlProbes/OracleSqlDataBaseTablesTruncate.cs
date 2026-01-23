using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace QaaS.Common.Probes.SqlProbes;

public class OracleSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new OracleConnection(Configuration.ConnectionString);
}