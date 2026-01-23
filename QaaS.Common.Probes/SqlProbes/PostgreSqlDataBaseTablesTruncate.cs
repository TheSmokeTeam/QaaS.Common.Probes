using System.Data;
using Npgsql;

namespace QaaS.Common.Probes.SqlProbes;

public class PostgreSqlDataBaseTablesTruncate : BaseSqlDataBaseTablesTruncateProbe
{
    protected override IDbConnection CreateDbConnection()
        => new NpgsqlConnection(Configuration.ConnectionString);
}