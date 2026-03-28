using System.Diagnostics.CodeAnalysis;
using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that Executes a command passed by the `cmd` string variable on every pod and container
/// if passed to the function
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="In-container commands" />
public class OsExecuteCommandsInContainers : BaseOsProbe<OsExecuteCommandsInContainersConfig>
{
    private static readonly TimeSpan OutputReadIdleTimeout = TimeSpan.FromMilliseconds(250);

    protected override void RunOsProbe()
    {
        var pods = new List<V1Pod>();
        pods = Configuration.ApplicationLabels!.Aggregate(pods, (current, label)
            => current.Concat(GetAllPods(label).Items).ToList());

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
        using var websocket = Kubernetes!
            .WebSocketNamespacedPodExecAsync(pod.Name(), pod.Namespace(), Configuration.Commands, containerName)
            .GetAwaiter().GetResult();
        using var demux = new StreamDemuxer(websocket);
        demux.Start();

        using var stream = demux.GetStream(1, 1);
        return ReadAvailableOutput(stream).Replace("\r", "").Replace("\n", "");
    }

    private static string ReadAvailableOutput(Stream stream)
        => ReadAvailableOutput(stream, OutputReadIdleTimeout);

    private static string ReadAvailableOutput(Stream stream, TimeSpan idleTimeout)
    {
        var builder = new StringBuilder();
        var buffer = new byte[4096];

        while (true)
        {
            using var cancellationTokenSource = new CancellationTokenSource(idleTimeout);

            try
            {
                var bytesRead = stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationTokenSource.Token)
                    .GetAwaiter().GetResult();
                if (bytesRead <= 0)
                {
                    break;
                }

                builder.Append(Encoding.Default.GetString(buffer, 0, bytesRead));
            }
            catch (OperationCanceledException)
            {
                break;
            }
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
