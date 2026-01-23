using System.Data;
using System.Data.SqlClient;

namespace QaaS.Common.Probes.SqlProbes;

public class MsSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new SqlConnection(Configuration.ConnectionString);
}