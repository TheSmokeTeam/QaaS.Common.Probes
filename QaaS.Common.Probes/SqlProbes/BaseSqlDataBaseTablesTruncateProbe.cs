using System.Collections.Immutable;
using System.Data;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Truncates a list of sql tables in a given database
/// </summary>
public abstract class BaseSqlDataBaseTablesTruncateProbe : BaseProbe<SqlDataBaseTablesTruncateProbeConfig>
{
    private IDbConnection _dbConnection = null!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        _dbConnection = CreateDbConnection();
        foreach (var table in Configuration.TableNames!)
            TruncateTable(table);
    }

    /// <summary>
    /// Common sql table truncation action (If this truncation syntax is not supported in a specific sql database
    /// this function can be overriden)
    /// </summary>
    protected virtual void TruncateTable(string tableName)
    {
        var command = _dbConnection.CreateCommand();
        command.CommandText = $"Truncate Table {tableName}";
        command.CommandTimeout = Configuration.CommandTimeoutSeconds;

        if (_dbConnection.State == ConnectionState.Closed)
            _dbConnection.Open();
        command.ExecuteNonQuery();
        Context.Logger.LogInformation("Truncated table {TableName}", tableName);
        if (_dbConnection.State != ConnectionState.Closed)
            _dbConnection.Close();
    }

    protected abstract IDbConnection CreateDbConnection();

    public void Dispose()
    {
        _dbConnection.Dispose();
    }
}