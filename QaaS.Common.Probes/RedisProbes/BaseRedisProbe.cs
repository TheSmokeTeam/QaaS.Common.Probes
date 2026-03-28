using System.CodeDom.Compiler;
using System.Collections.Immutable;
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Framework.Protocols.ConfigurationObjects.Redis;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using StackExchange.Redis;

namespace QaaS.Common.Probes.RedisProbes;

public abstract class BaseRedisProbe<TBaseRedisProbeConfig> : BaseProbe<TBaseRedisProbeConfig>
    where TBaseRedisProbeConfig : BaseRedisConfig, new()
{
    private IConnectionMultiplexer _redisConnection = null!;
    protected IDatabase RedisDb = null!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        var configurationOptions = Configuration.CreateRedisConfigurationOptions();
        TextWriter consoleWriter = new IndentedTextWriter(Console.Out);
        _redisConnection = ConnectionMultiplexer.Connect(configurationOptions, consoleWriter);
        RedisDb = _redisConnection.GetDatabase(GetDatabaseNumber());
        RunRedisProbe();
    }

    protected abstract void RunRedisProbe();

    protected virtual int GetDatabaseNumber()
    {
        return Configuration is RedisDataBaseProbeBaseConfig redisConfiguration
            ? redisConfiguration.RedisDataBase
            : -1;
    }

    public void Dispose()
    {
        _redisConnection.Dispose();
    }
}
