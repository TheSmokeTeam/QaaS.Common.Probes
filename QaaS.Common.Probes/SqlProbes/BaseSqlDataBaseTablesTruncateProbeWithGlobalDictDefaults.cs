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

    /// <summary>
    /// Executes the truncate flow after configuration defaults have been resolved from the probe global dictionary.
    /// </summary>
    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        _dbConnection = CreateDbConnection();
        foreach (var table in Configuration.TableNames!)
            TruncateTable(table);
    }

    /// <summary>
    /// Executes the truncate command for a single configured table, opening and closing the connection when needed.
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

    /// <summary>
    /// Creates the provider-specific database connection used by this probe.
    /// </summary>
    protected abstract IDbConnection CreateDbConnection();

    /// <summary>
    /// Builds the provider-specific TRUNCATE command text for the supplied table identifier.
    /// </summary>
    protected virtual string BuildTruncateCommandText(string tableName)
        => $"TRUNCATE TABLE {FormatQualifiedIdentifier(tableName)}";

    /// <summary>
    /// Validates and formats a potentially schema-qualified table identifier by quoting each segment independently.
    /// </summary>
    protected string FormatQualifiedIdentifier(string qualifiedIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedIdentifier);

        return string.Join(".",
            qualifiedIdentifier.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(QuoteIdentifier));
    }

    /// <summary>
    /// Quotes a single identifier segment for the current SQL dialect after validating that it is safe to emit.
    /// </summary>
    protected virtual string QuoteIdentifier(string identifier)
    {
        ValidateIdentifier(identifier);
        return identifier;
    }

    /// <summary>
    /// Rejects identifier segments that contain unsupported or unsafe characters before SQL text is generated.
    /// </summary>
    protected static void ValidateIdentifier(string identifier)
    {
        if (!Regex.IsMatch(identifier, "^[A-Za-z_][A-Za-z0-9_$#]*$"))
        {
            throw new InvalidOperationException(
                $"Table identifier '{identifier}' contains unsupported or unsafe characters.");
        }
    }

    /// <summary>
    /// Disposes the cached database connection created for the probe run.
    /// </summary>
    public void Dispose()
    {
        _dbConnection?.Dispose();
    }
}
