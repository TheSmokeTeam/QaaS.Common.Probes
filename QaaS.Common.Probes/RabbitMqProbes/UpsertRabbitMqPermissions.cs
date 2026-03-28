using System.Text.Json;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates or updates RabbitMQ permissions for users in one or more virtual hosts through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Permissions" />
public class UpsertRabbitMqPermissions
    : BaseRabbitMqManagementObjectsManipulation<UpsertRabbitMqPermissionsConfig, RabbitMqPermissionConfig>
{
    protected override IEnumerable<RabbitMqPermissionConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Permissions!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient, RabbitMqPermissionConfig objectToManipulateConfig)
    {
        var payload = JsonSerializer.Serialize(new
        {
            configure = objectToManipulateConfig.Configure,
            write = objectToManipulateConfig.Write,
            read = objectToManipulateConfig.Read
        });

        Context.Logger.LogDebug("Creating or updating rabbitmq permissions for user {Username} in virtual host {VirtualHostName}",
            objectToManipulateConfig.Username, objectToManipulateConfig.VirtualHostName);

        return SendManagementRequestAsync(httpClient, HttpMethod.Put,
            $"permissions/{EncodePathSegment(objectToManipulateConfig.VirtualHostName!)}/{EncodePathSegment(objectToManipulateConfig.Username!)}",
            payload);
    }
}
