using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Creates RabbitMQ virtual hosts through the management API so later probes and sessions can use them.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Virtual hosts lifecycle" />
public class CreateRabbitMqVirtualHosts
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDictDefaults<CreateRabbitMqVirtualHostsConfig, RabbitMqVirtualHostConfig>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        yield return new ProbeGlobalDictReadRequest("recovery",
            BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "VirtualHosts"));
    }

    protected override IEnumerable<RabbitMqVirtualHostConfig> GetObjectsToManipulateConfigurations()
        => Configuration.VirtualHosts!;

    protected override Task ManipulateObjectAsync(HttpClient httpClient, RabbitMqVirtualHostConfig objectToManipulateConfig)
    {
        var payload = JsonSerializer.Serialize(new
        {
            description = objectToManipulateConfig.Description,
            tags = JoinTags(objectToManipulateConfig.Tags),
            default_queue_type = objectToManipulateConfig.DefaultQueueType,
            protected_from_deletion = objectToManipulateConfig.ProtectedFromDeletion,
            tracing = objectToManipulateConfig.Tracing
        }, JsonSerializerOptions);

        Context.Logger.LogDebug("Creating or updating rabbitmq virtual host {VirtualHostName}",
            objectToManipulateConfig.Name);

        return SendManagementRequestAsync(httpClient, HttpMethod.Put,
            $"vhosts/{EncodePathSegment(objectToManipulateConfig.Name!)}", payload);
    }

    private static string? JoinTags(IEnumerable<string>? tags)
        => tags is null ? null : string.Join(",", tags);
}
