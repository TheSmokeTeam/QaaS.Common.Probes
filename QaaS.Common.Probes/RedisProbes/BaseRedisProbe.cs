using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Base implementation for Redis probes that create a connection, select the configured database,
/// and then delegate the actual probe logic to a derived type.
/// </summary>
/// <typeparam name="TBaseRedisProbeConfig">The Redis configuration type consumed by the probe.</typeparam>
public abstract class BaseRedisProbe<TBaseRedisProbeConfig> : BaseProbe<TBaseRedisProbeConfig>, IDisposable
    where TBaseRedisProbeConfig : BaseRedisConfig, new()
{
    private IConnectionMultiplexer _redisConnection = null!;
    protected IDatabase RedisDb = null!;

    /// <summary>
    /// Creates the underlying Redis connection multiplexer.
    /// </summary>
    /// <param name="configurationOptions">The Redis connection options.</param>
    /// <param name="consoleWriter">The writer used for Redis client diagnostics.</param>
    /// <returns>An open Redis connection multiplexer.</returns>
    [ExcludeFromCodeCoverage]
    protected virtual IConnectionMultiplexer CreateConnectionMultiplexer(ConfigurationOptions configurationOptions,
        TextWriter consoleWriter)
        => ConnectionMultiplexer.Connect(configurationOptions, consoleWriter);

    /// <summary>
    /// Opens the Redis connection and selected database, then executes the derived probe logic.
    /// </summary>
    /// <param name="sessionDataList">The current session data.</param>
    /// <param name="dataSourceList">The current data sources.</param>
    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var configurationOptions = Configuration.CreateRedisConfigurationOptions();
        TextWriter consoleWriter = new IndentedTextWriter(Console.Out);
        _redisConnection = CreateConnectionMultiplexer(configurationOptions, consoleWriter);
        var redisDataBase = (Configuration as RedisDataBaseProbeBaseConfig)?.RedisDataBase ?? 0;
        RedisDb = _redisConnection.GetDatabase(redisDataBase);
        RunRedisProbe();
    }

    /// <summary>
    /// Executes the concrete Redis probe logic after the Redis database has been initialized.
    /// </summary>
    protected abstract void RunRedisProbe();

    /// <summary>
    /// Disposes the Redis connection created for this probe instance.
    /// </summary>
    public void Dispose()
    {
        _redisConnection?.Dispose();
    }
}
