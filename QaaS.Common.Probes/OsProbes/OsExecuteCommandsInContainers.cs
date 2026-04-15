using System.Diagnostics.CodeAnalysis;
using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that Executes a command passed by the `cmd` string variable on every pod and container
/// if passed to the function
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="In-container commands" />
public class OsExecuteCommandsInContainers : BaseOsProbe<OsExecuteCommandsInContainersConfig>
{
    protected override void RunOsProbe()
    {
        var pods = Configuration.ApplicationLabels!
            .SelectMany(label => GetAllPods(label).Items)
            .GroupBy(pod => $"{pod.Namespace()}/{pod.Name()}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        // if no pods were found skip rest of the function
        if (pods.Count <= 0)
        {
            Context.Logger.LogError("Found no pods matching any of the given labels {GivenLabels}," +
                                    " execute command in containers probe won't run",
                string.Join(", ", Configuration.ApplicationLabels!));
            return;
        }

        foreach (var pod in pods)
        {
            foreach (var container in pod.Spec.Containers)
            {
                if (Configuration.ContainerName != null && container.Name != Configuration.ContainerName) continue;

                Context.Logger.LogInformation("Executing commands on pod {PodName} in container" +
                                              " {ContainerName}", pod.Name(), container.Name);
                Context.Logger.LogDebug("Commands executed are: {ExecutedCommandsList}",
                    string.Join(", ", Configuration.Commands!));

                var result = ExecuteCommands(pod, container.Name);
                Context.Logger.LogDebug("Result of command execution is {CommandExecutionResult}", result);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    protected virtual string ExecuteCommands(V1Pod pod, string containerName)
    {
        // Request a non-TTY exec session so Kubernetes keeps stdout/stderr/status on separate channels and
        // surfaces non-zero exits on the status stream.
        using var websocket = Kubernetes!
            .WebSocketNamespacedPodExecAsync(pod.Name(), pod.Namespace(), Configuration.Commands, containerName,
                stderr: true, stdin: false, stdout: true, tty: false)
            .GetAwaiter().GetResult();
        using var demux = new StreamDemuxer(websocket);
        // StreamDemuxer only buffers channels that were requested before the websocket frames arrive, so create
        // stdout/stderr/status streams up front to avoid losing a failure status while stdout is still being read.
        using var standardOutputStream = demux.GetStream(1, 1);
        using var standardErrorStream = demux.GetStream(2, 2);
        using var statusStream = demux.GetStream(3, 3);
        demux.Start();

        var standardOutput = ReadAvailableOutput(standardOutputStream);
        var standardError = ReadAvailableOutput(standardErrorStream);
        var executionError = GetExecutionError(ReadAvailableOutput(statusStream));
        if (!string.IsNullOrWhiteSpace(standardError))
        {
            Context.Logger.LogDebug("Command execution stderr for pod {PodName} container {ContainerName}: {CommandError}",
                pod.Name(), containerName, standardError.Trim());
        }
        if (!string.IsNullOrWhiteSpace(executionError))
        {
            var errorDetails = string.IsNullOrWhiteSpace(standardError)
                ? executionError.Trim()
                : $"{executionError.Trim()}{Environment.NewLine}{standardError.Trim()}";
            throw new InvalidOperationException(
                $"Command execution failed for pod '{pod.Name()}' container '{containerName}': {errorDetails}");
        }

        return standardOutput.Replace("\r", "").Replace("\n", "");
    }

    private static string GetExecutionError(string executionStatus)
    {
        var trimmedStatus = executionStatus.Trim();
        if (string.IsNullOrWhiteSpace(trimmedStatus))
        {
            return string.Empty;
        }

        try
        {
            var statusPayload = JObject.Parse(trimmedStatus);
            return string.Equals(statusPayload["status"]?.ToString(), "Success", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : trimmedStatus;
        }
        catch
        {
            return trimmedStatus;
        }
    }

    private static string ReadAvailableOutput(Stream stream)
    {
        var builder = new StringBuilder();
        var buffer = new byte[4096];

        while (true)
        {
            var bytesRead = stream.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken.None)
                .GetAwaiter().GetResult();
            if (bytesRead <= 0)
            {
                break;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Method to get all pods by a given label selector from a given namespace. 
    /// </summary>
    /// <param name="labelSelector">The label selector describing the pods whose events will be saved.</param>
    /// <returns>An <see cref="V1PodList"/> of all pods who falls under the label selector criteria in the given namespace.</returns>
    /// <exception cref="HttpOperationException">Thrown when an HTTP operation error occurs while using the k8s clients API.</exception>
    private V1PodList GetAllPods(string labelSelector)
    {
        try
        {
            return Kubernetes.ListNamespacedPod(Configuration.Openshift!.Namespace,
                labelSelector: labelSelector);
        }
        catch (HttpOperationException exception)
        {
            throw new HttpOperationException($"{exception.Response.Content}\n{exception}");
        }
    }
}
