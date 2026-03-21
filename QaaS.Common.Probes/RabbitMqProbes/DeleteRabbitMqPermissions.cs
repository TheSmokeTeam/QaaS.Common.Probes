using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

public class DeleteRabbitMqPermissions
    : BaseRabbitMqManagementObjectsManipulation<DeleteRabbitMqPermissionsConfig, RabbitMqPermissionTargetConfig>
{
    protected override IEnumerable<RabbitMqPermissionTargetConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Permissions!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient,
        RabbitMqPermissionTargetConfig objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq permissions for user {Username} in virtual host {VirtualHostName}",
            objectToManipulateConfig.Username, objectToManipulateConfig.VirtualHostName);

        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"permissions/{EncodePathSegment(objectToManipulateConfig.VirtualHostName!)}/{EncodePathSegment(objectToManipulateConfig.Username!)}");
    }
}
