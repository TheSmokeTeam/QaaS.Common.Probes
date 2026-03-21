using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

public class UploadRabbitMqDefinitions : BaseRabbitMqManagementProbe<UploadRabbitMqDefinitionsConfig>
{
    protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
    {
        var definitionsPayload = ResolveDefinitionsPayload();
        var relativePath = string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
            ? "definitions"
            : $"definitions/{EncodePathSegment(Configuration.VirtualHostName)}";

        SendManagementRequestAsync(httpClient, HttpMethod.Post, relativePath, definitionsPayload)
            .GetAwaiter()
            .GetResult();

        Context.Logger.LogInformation("Uploaded rabbitmq definitions to {DefinitionsScope}",
            string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
                ? "the cluster"
                : $"virtual host {Configuration.VirtualHostName}");
    }

    protected virtual string ReadAllText(string path) => File.ReadAllText(path);

    private string ResolveDefinitionsPayload()
    {
        var hasInlineDefinitions = !string.IsNullOrWhiteSpace(Configuration.DefinitionsJson);
        var hasFileDefinitions = !string.IsNullOrWhiteSpace(Configuration.DefinitionsFilePath);

        return (hasInlineDefinitions, hasFileDefinitions) switch
        {
            (true, false) => Configuration.DefinitionsJson!,
            (false, true) => ReadAllText(Configuration.DefinitionsFilePath!),
            (true, true) => throw new InvalidOperationException(
                "Provide either DefinitionsJson or DefinitionsFilePath, but not both."),
            _ => throw new InvalidOperationException(
                "Either DefinitionsJson or DefinitionsFilePath must be configured.")
        };
    }
}
