using System.Collections.Immutable;
using System.Data;
using System.Text.RegularExpressions;
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
