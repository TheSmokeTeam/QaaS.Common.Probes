using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

public class DownloadRabbitMqDefinitions : BaseRabbitMqManagementProbe<DownloadRabbitMqDefinitionsConfig>
{
    protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
    {
        var relativePath = string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
            ? "definitions"
            : $"definitions/{EncodePathSegment(Configuration.VirtualHostName)}";

        var definitionsJson = SendManagementRequestAsync(httpClient, HttpMethod.Get, relativePath)
            .GetAwaiter()
            .GetResult();

        WriteAllText(Configuration.DefinitionsFilePath!, definitionsJson);

        Context.Logger.LogInformation("Downloaded rabbitmq definitions from {DefinitionsScope} to {DefinitionsFilePath}",
            string.IsNullOrWhiteSpace(Configuration.VirtualHostName)
                ? "the cluster"
                : $"virtual host {Configuration.VirtualHostName}",
            Configuration.DefinitionsFilePath);
    }

    protected virtual void WriteAllText(string path, string contents)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(path, contents);
    }
}
