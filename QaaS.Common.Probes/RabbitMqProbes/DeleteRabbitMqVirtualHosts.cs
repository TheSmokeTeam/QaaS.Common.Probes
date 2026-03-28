using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ virtual hosts through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Virtual hosts lifecycle" />
public class DeleteRabbitMqVirtualHosts
    : BaseRabbitMqManagementObjectsManipulation<DeleteRabbitMqVirtualHostsConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.VirtualHostNames!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient, string objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq virtual host {VirtualHostName}", objectToManipulateConfig);
        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"vhosts/{EncodePathSegment(objectToManipulateConfig)}");
    }
}
