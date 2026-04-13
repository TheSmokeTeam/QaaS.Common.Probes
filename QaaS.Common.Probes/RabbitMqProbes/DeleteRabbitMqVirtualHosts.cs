using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ virtual hosts through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Virtual hosts lifecycle" />
public class DeleteRabbitMqVirtualHosts
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDict<DeleteRabbitMqVirtualHostsConfig, string>
{
    private CreateRabbitMqVirtualHostsConfig? _previousDefaults;
    private VirtualHostRecoveryPayload? _previousRecovery;

    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.VirtualHostNames!;

    protected override void SaveResolvedConfigurationDefaults()
    {
        if (Configuration.UseGlobalDict)
        {
            _previousRecovery = RabbitMqRecoverySnapshotHelper.TryGetRecoveryPayload<VirtualHostRecoveryPayload>(Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "VirtualHosts"));
            _previousDefaults =
                RabbitMqRecoverySnapshotHelper.TryGetConfigurationDefaults<CreateRabbitMqVirtualHostsConfig>(Context,
                    BuildGlobalDictionaryAliasPath("RabbitMq", "ManagementDefaults"));
        }

        base.SaveResolvedConfigurationDefaults();
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            var virtualHostNames = Configuration.VirtualHostNames!;
            var virtualHosts = RabbitMqRecoverySnapshotHelper.FilterByNames(
                _previousRecovery?.VirtualHosts ?? _previousDefaults?.VirtualHosts,
                virtualHostNames,
                virtualHost => virtualHost.Name);
            if (virtualHosts.Length == 0)
            {
                virtualHosts = virtualHostNames
                    .Select(name => new RabbitMqVirtualHostConfig { Name = name })
                    .ToArray();
            }

            SaveGlobalDictionaryPayload("recovery",
                new VirtualHostRecoveryPayload
                {
                    VirtualHosts = virtualHosts
                },
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "VirtualHosts"));
        }
    }

    protected override Task ManipulateObjectAsync(HttpClient httpClient, string objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq virtual host {VirtualHostName}", objectToManipulateConfig);
        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"vhosts/{EncodePathSegment(objectToManipulateConfig)}");
    }

    private sealed record VirtualHostRecoveryPayload
    {
        public RabbitMqVirtualHostConfig[]? VirtualHosts { get; init; }
    }
}
