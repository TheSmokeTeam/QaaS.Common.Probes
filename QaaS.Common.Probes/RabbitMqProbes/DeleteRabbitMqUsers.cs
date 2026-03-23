using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ users through the management API.
/// </summary>
public class DeleteRabbitMqUsers : BaseRabbitMqManagementObjectsManipulation<DeleteRabbitMqUsersConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.Usernames!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient, string objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq user {Username}", objectToManipulateConfig);
        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"users/{EncodePathSegment(objectToManipulateConfig)}");
    }
}
