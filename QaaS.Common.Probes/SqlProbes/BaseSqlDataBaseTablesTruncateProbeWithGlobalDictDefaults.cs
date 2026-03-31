using System.Collections.Immutable;
using System.Data;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Sql;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.SqlProbes;

/// <summary>
/// Shared SQL truncate probe base that resolves missing connection settings from the probe global dictionary and then
/// truncates the configured tables.
/// </summary>
public abstract class BaseSqlDataBaseTablesTruncateProbeWithGlobalDict
    : BaseProbeWithGlobalDict<SqlDataBaseTablesTruncateProbeConfig>
{
    private IDbConnection _dbConnection = null!;

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("Sql", "Defaults");

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        _dbConnection = CreateDbConnection();
        foreach (var table in Configuration.TableNames!)
            TruncateTable(table);
    }

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
