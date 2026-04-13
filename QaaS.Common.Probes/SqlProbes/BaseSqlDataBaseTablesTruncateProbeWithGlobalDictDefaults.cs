using System.Collections.Immutable;
using System.Data;
using System.Text.RegularExpressions;
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
        using var command = _dbConnection.CreateCommand();
        command.CommandText = BuildTruncateCommandText(tableName);
        command.CommandTimeout = Configuration.CommandTimeoutSeconds;

        var shouldCloseConnection = _dbConnection.State == ConnectionState.Closed;
        if (shouldCloseConnection)
        {
            _dbConnection.Open();
        }

        try
        {
            command.ExecuteNonQuery();
            Context.Logger.LogInformation("Truncated table {TableName}", tableName);
        }
        finally
        {
            if (shouldCloseConnection && _dbConnection.State != ConnectionState.Closed)
            {
                _dbConnection.Close();
            }
        }
    }

    protected abstract IDbConnection CreateDbConnection();

    protected virtual string BuildTruncateCommandText(string tableName)
        => $"TRUNCATE TABLE {FormatQualifiedIdentifier(tableName)}";

    protected string FormatQualifiedIdentifier(string qualifiedIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedIdentifier);

        return string.Join(".",
            qualifiedIdentifier.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(QuoteIdentifier));
    }

    protected virtual string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier);
        return identifier;
    }

    protected static void ValidateIdentifier(string identifier)
    {
        if (!Regex.IsMatch(identifier, "^[A-Za-z_][A-Za-z0-9_$#]*$"))
        {
            throw new InvalidOperationException(
                $"Table identifier '{identifier}' contains unsupported or unsafe characters.");
        }
    }

    public void Dispose()
    {
        _dbConnection?.Dispose();
    }
}
