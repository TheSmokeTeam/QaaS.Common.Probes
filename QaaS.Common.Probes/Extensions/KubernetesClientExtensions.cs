using System.Diagnostics.CodeAnalysis;
using k8s;

namespace QaaS.Common.Probes.Extensions;

/// <summary>
/// Contains extension functions for kubernetes client
/// </summary>
[ExcludeFromCodeCoverage]
public static class KubernetesClientExtensions
{
    /// <summary>
    /// Copies file from k8s pod and writes it locally
    /// </summary>
    /// <param name="kubernetesClient">The k8s client</param>
    /// <param name="podName">The pod name</param>
    /// <param name="namespace">The namespace of the pod</param>
    /// <param name="containerName">The container name of the pod</param>
    /// <param name="podFilePath">The path in the pod that the file is located at</param>
    /// <param name="localFilePath">The local path to copy the file to</param>
    /// <returns></returns>
    public static async Task CopyNameSpacedPodFileAsync(this IKubernetes kubernetesClient, string podName,
        string @namespace,
        string containerName, string podFilePath, string localFilePath)
    {
        await using var fileStream = File.Create(localFilePath);
        await kubernetesClient.CopyNameSpacedPodFileAsync(podName, @namespace, containerName, podFilePath, fileStream);
    }

    /// <summary>
    /// Copies file from k8s pod and writes it locally
    /// </summary>
    /// <param name="kubernetesClient">The k8s client</param>
    /// <param name="podName">The pod name</param>
    /// <param name="namespace">The namespace of the pod</param>
    /// <param name="containerName">The container name of the pod</param>
    /// <param name="podFilePath">The path in the pod that the file is located at</param>
    /// <param name="outputStream">The output stream the data will be written to</param>
    /// <returns></returns>
    public static async Task CopyNameSpacedPodFileAsync(this IKubernetes kubernetesClient, string podName,
        string @namespace,
        string containerName, string podFilePath, Stream outputStream)
    {
        var execCommand = new List<string>() { "cat", podFilePath };

        // running a `cat` command in a container
        using var webSocket = await kubernetesClient.WebSocketNamespacedPodExecAsync(podName, @namespace, execCommand,
            containerName, false, false);

        // duplicating the stream of the command
        var demux = new StreamDemuxer(webSocket);
        demux.Start();

        // copying the stdout into the output stream
        byte? index = 1;
        await using var stdOut = demux.GetStream(index, null);
        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stdOut.ReadAsync(buffer.AsMemory(0, buffer.Length), CancellationToken.None)) > 0)
            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), CancellationToken.None);

        await outputStream.FlushAsync(CancellationToken.None);
    }
}
