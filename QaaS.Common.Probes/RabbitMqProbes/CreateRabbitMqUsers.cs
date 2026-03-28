using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates RabbitMQ users through the management API with the configured credentials and tags.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Users lifecycle" />
public class CreateRabbitMqUsers
    : BaseRabbitMqManagementObjectsManipulation<CreateRabbitMqUsersConfig, RabbitMqUserConfig>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override IEnumerable<RabbitMqUserConfig> GetObjectsToManipulateConfigurations() => Configuration.Users!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient, RabbitMqUserConfig objectToManipulateConfig)
    {
        var payload = JsonSerializer.Serialize(new
        {
            password = objectToManipulateConfig.Password,
            password_hash = objectToManipulateConfig.PasswordHash,
            hashing_algorithm = objectToManipulateConfig.HashingAlgorithm,
            tags = JoinTags(objectToManipulateConfig.Tags)
        }, JsonSerializerOptions);

        Context.Logger.LogDebug("Creating or updating rabbitmq user {Username}", objectToManipulateConfig.Username);

        return SendManagementRequestAsync(httpClient, HttpMethod.Put,
            $"users/{EncodePathSegment(objectToManipulateConfig.Username!)}", payload);
    }

    private static string? JoinTags(IEnumerable<string>? tags)
        => tags is null ? null : string.Join(",", tags);
}
