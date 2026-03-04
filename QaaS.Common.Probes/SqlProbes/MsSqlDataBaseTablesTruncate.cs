using System.Data;
using Microsoft.Data.SqlClient;

namespace QaaS.Common.Probes.SqlProbes;

public class MsSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new SqlConnection(Configuration.ConnectionString);
}
