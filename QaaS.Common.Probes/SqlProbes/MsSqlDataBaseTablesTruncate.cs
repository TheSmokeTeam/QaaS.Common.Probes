using System.Data;
using Microsoft.Data.SqlClient;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured Microsoft SQL Server tables in the order they are listed.
/// </summary>
public class MsSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new SqlConnection(Configuration.ConnectionString);
}
