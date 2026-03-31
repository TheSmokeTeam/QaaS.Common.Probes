using System.Data;
using Microsoft.Data.SqlClient;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates the configured Microsoft SQL Server tables in the order they are listed.
/// </summary>
/// <qaas-docs group="SQL maintenance" subgroup="Microsoft SQL tables" />
public class MsSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbeWithGlobalDict
{
    protected override IDbConnection CreateDbConnection()
        => new SqlConnection(Configuration.ConnectionString);
}
