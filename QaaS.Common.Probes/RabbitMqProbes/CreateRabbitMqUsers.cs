using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates RabbitMQ users through the management API with the configured credentials and tags.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Users lifecycle" />
public class CreateRabbitMqUsers
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDict<CreateRabbitMqUsersConfig, RabbitMqUserConfig>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Users"));
    }

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
