using System.Net;
using System.Reflection;
using k8s;
using k8s.Autorest;
using k8s.Models;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsExecuteCommandsInContainersTests
{
    private sealed class NonTerminatingStream(byte[] initialPayload) : Stream
    {
        private readonly MemoryStream _memoryStream = new(initialPayload);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _memoryStream.Length;
        public override long Position { get => _memoryStream.Position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_memoryStream.Position < _memoryStream.Length)
            {
                return await _memoryStream.ReadAsync(buffer, cancellationToken);
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestableOsExecuteCommandsInContainers : OsExecuteCommandsInContainers
    {
        public List<(string PodName, string ContainerName)> Executions { get; } = [];

        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public void InvokeRunOsProbe() => RunOsProbe();

        protected override string ExecuteCommands(V1Pod pod, string containerName)
        {
            Executions.Add((pod.Metadata!.Name!, containerName));
            return $"{pod.Metadata!.Name}:{containerName}";
        }
    }

    [Test]
    public void RunOsProbe_WhenNoPodsMatchConfiguredLabels_ShouldReturnWithoutExecutingCommands()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": []
            }
            """);

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=test"],
                Commands = ["echo", "hello"]
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
        Assert.That(probe.Executions, Is.Empty);
    }

    [Test]
    public void RunOsProbe_WhenContainerNameIsSpecified_ShouldExecuteOnlyMatchingContainersAcrossAllLabels()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": [
                {
                  "metadata": { "name": "pod-a", "namespace": "namespace" },
                  "spec": {
                    "containers": [
                      { "name": "worker" },
                      { "name": "sidecar" }
                    ]
                  }
                }
              ]
            }
            """);
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            """
            {
              "apiVersion": "v1",
              "kind": "PodList",
              "items": [
                {
                  "metadata": { "name": "pod-b", "namespace": "namespace" },
                  "spec": {
                    "containers": [
                      { "name": "worker" }
                    ]
                  }
                }
              ]
            }
            """);

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=one", "app=two"],
                Commands = ["echo", "hello"],
                ContainerName = "worker"
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        probe.InvokeRunOsProbe();

        Assert.That(probe.Executions, Is.EqualTo(new[]
        {
            ("pod-a", "worker"),
            ("pod-b", "worker")
        }));
    }

    [Test]
    public void RunOsProbe_WhenPodLookupFails_ShouldThrowWrappedHttpOperationException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/pods",
            "{\"message\":\"lookup failed\"}",
            HttpStatusCode.InternalServerError);

        var probe = new TestableOsExecuteCommandsInContainers
        {
            Configuration = new OsExecuteCommandsInContainersConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ApplicationLabels = ["app=test"],
                Commands = ["echo", "hello"]
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var exception = Assert.Throws<HttpOperationException>(() => probe.InvokeRunOsProbe());
        Assert.That(exception!.Message, Does.Contain("lookup failed"));
    }

    [Test]
    public void ReadAvailableOutput_WhenStreamNeverCloses_ReturnsBufferedContentAfterIdleTimeout()
    {
        var method = typeof(OsExecuteCommandsInContainers)
            .GetMethod("ReadAvailableOutput", BindingFlags.NonPublic | BindingFlags.Static, null,
                [typeof(Stream), typeof(TimeSpan)], null)!;
        using var stream = new NonTerminatingStream("hello\n"u8.ToArray());

        var result = (string)method.Invoke(null, [stream, TimeSpan.FromMilliseconds(10)])!;

        Assert.That(result, Is.EqualTo("hello\n"));
    }

    [Test]
    public void TryReadStreamOutput_WhenStreamFactoryBlocks_ReturnsEmptyStringAfterTimeout()
    {
        var method = typeof(OsExecuteCommandsInContainers)
            .GetMethod("TryReadStreamOutput", BindingFlags.NonPublic | BindingFlags.Static, null,
                [typeof(Func<Stream>), typeof(TimeSpan), typeof(TimeSpan)], null)!;
        var blockedFactory = new Func<Stream>(() =>
        {
            using var neverCompletes = new ManualResetEventSlim(false);
            neverCompletes.Wait();
            return Stream.Null;
        });

        var result = (string)method.Invoke(null,
            [blockedFactory, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10)])!;

        Assert.That(result, Is.Empty);
    }

    private static Openshift CreateOpenshiftConfig()
    {
        return new Openshift
        {
            Cluster = "cluster",
            Namespace = "namespace",
            Username = "username",
            Password = "password"
        };
    }
}
