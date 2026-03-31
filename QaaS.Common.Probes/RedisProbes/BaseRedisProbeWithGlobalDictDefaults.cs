using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using QaaS.Common.Probes.ConfigurationObjects;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

/// <summary>
/// Shared Redis probe base that resolves missing connection and database settings from the probe global dictionary
/// before connecting.
/// </summary>
public abstract class BaseRedisProbeWithGlobalDict<TBaseRedisProbeConfig>
    : BaseProbeWithGlobalDict<TBaseRedisProbeConfig>, IDisposable
    where TBaseRedisProbeConfig : BaseRedisConfig, IUseGlobalDictProbeConfiguration, new()
{
    private IConnectionMultiplexer _redisConnection = null!;
    protected IDatabase RedisDb = null!;

    [ExcludeFromCodeCoverage]
    protected virtual IConnectionMultiplexer CreateConnectionMultiplexer(ConfigurationOptions configurationOptions,
        TextWriter consoleWriter)
        => ConnectionMultiplexer.Connect(configurationOptions, consoleWriter);

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("Redis", "Defaults");

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var configurationOptions = Configuration.CreateRedisConfigurationOptions();
        TextWriter consoleWriter = new IndentedTextWriter(Console.Out);
        _redisConnection = CreateConnectionMultiplexer(configurationOptions, consoleWriter);
        var redisDataBase = (Configuration as RedisDataBaseProbeBaseConfig)?.RedisDataBase ?? 0;
        RedisDb = _redisConnection.GetDatabase(redisDataBase);
        RunRedisProbe();
    }

    protected abstract void RunRedisProbe();

    public void Dispose()
    {
        _redisConnection.Dispose();
    }
}
