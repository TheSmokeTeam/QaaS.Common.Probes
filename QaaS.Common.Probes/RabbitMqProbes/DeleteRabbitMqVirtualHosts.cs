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
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.VirtualHostNames!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery",
                new
                {
                    VirtualHosts = Configuration.VirtualHostNames!
                        .Select(name => new RabbitMqVirtualHostConfig { Name = name })
                        .ToArray()
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
}
