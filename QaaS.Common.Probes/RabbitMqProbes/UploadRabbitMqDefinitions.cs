using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Uploads a previously captured RabbitMQ definitions file back into the broker through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Definitions" />
public class UploadRabbitMqDefinitions : BaseRabbitMqManagementProbeWithGlobalDict<UploadRabbitMqDefinitionsConfig>
{
    protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
    {
        var hasInlineDefinitions = !string.IsNullOrWhiteSpace(Configuration.DefinitionsJson);
        var hasDefinitionsFile = !string.IsNullOrWhiteSpace(Configuration.DefinitionsFilePath);
        if (hasInlineDefinitions == hasDefinitionsFile)
        {
            throw new InvalidOperationException(
                "Exactly one rabbitmq definitions source must be provided via DefinitionsJson or DefinitionsFilePath.");
        }

        var definitionsSource = hasInlineDefinitions ? "<inline-json>" : Configuration.DefinitionsFilePath!;
        var definitionsJson = hasInlineDefinitions
            ? Configuration.DefinitionsJson!
            : ReadAllText(Configuration.DefinitionsFilePath!);
        var relativePath = string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
            ? "definitions"
            : $"definitions/{EncodePathSegment(Configuration.VirtualHostName)}";

        SendManagementRequestAsync(httpClient, HttpMethod.Post, relativePath, definitionsJson)
            .GetAwaiter()
            .GetResult();

        Context.Logger.LogInformation("Uploaded rabbitmq definitions from {DefinitionsFilePath} into {DefinitionsScope}",
            definitionsSource,
            string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
                ? "the cluster"
                : $"virtual host {Configuration.VirtualHostName}");
    }

    protected virtual string ReadAllText(string path) => File.ReadAllText(path);
}
