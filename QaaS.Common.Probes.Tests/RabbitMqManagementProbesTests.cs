using System.Text.Json;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.RabbitMqProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class RabbitMqManagementProbesTests
{
    private sealed class TestableUploadRabbitMqDefinitions(HttpClient httpClient) : UploadRabbitMqDefinitions
    {
        public string FileContents { get; set; } = string.Empty;

        protected override HttpClient CreateHttpClient() => httpClient;

        protected override string ReadAllText(string path) => FileContents;
    }

    private sealed class TestableDownloadRabbitMqDefinitions(HttpClient httpClient) : DownloadRabbitMqDefinitions
    {
        public string? WrittenPath { get; private set; }

        public string? WrittenContents { get; private set; }

        protected override HttpClient CreateHttpClient() => httpClient;

        protected override void WriteAllText(string path, string contents)
        {
            WrittenPath = path;
            WrittenContents = contents;
        }
    }

    private sealed class TestableCreateRabbitMqVirtualHosts(HttpClient httpClient) : CreateRabbitMqVirtualHosts
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    private sealed class TestableDeleteRabbitMqVirtualHosts(HttpClient httpClient) : DeleteRabbitMqVirtualHosts
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    private sealed class TestableCreateRabbitMqUsers(HttpClient httpClient) : CreateRabbitMqUsers
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    private sealed class TestableDeleteRabbitMqUsers(HttpClient httpClient) : DeleteRabbitMqUsers
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    private sealed class TestableUpsertRabbitMqPermissions(HttpClient httpClient) : UpsertRabbitMqPermissions
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    private sealed class TestableDeleteRabbitMqPermissions(HttpClient httpClient) : DeleteRabbitMqPermissions
    {
        protected override HttpClient CreateHttpClient() => httpClient;
    }

    [Test]
    public void UploadRabbitMqDefinitions_WithInlineJson_ShouldPostDefinitionsPayload()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableUploadRabbitMqDefinitions(httpClient)
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                DefinitionsJson = """{"queues":[{"name":"queue-a"}]}"""
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/definitions"));
        Assert.That(NormalizeJson(request.Body!), Is.EqualTo(NormalizeJson("""{"queues":[{"name":"queue-a"}]}""")));
    }

    [Test]
    public void UploadRabbitMqDefinitions_WithFileDefinitionsAndVirtualHost_ShouldUseScopedEndpoint()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableUploadRabbitMqDefinitions(httpClient)
        {
            FileContents = """{"users":[{"name":"qa-user"}]}""",
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                DefinitionsFilePath = "defs.json",
                VirtualHostName = "/"
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/definitions/%2F"));
        Assert.That(NormalizeJson(request.Body!), Is.EqualTo(NormalizeJson("""{"users":[{"name":"qa-user"}]}""")));
    }

    [Test]
    public void UploadRabbitMqDefinitions_WhenBothInlineAndFileDefinitionsProvided_ShouldThrow()
    {
        using var httpClient = CreateHttpClient(new HttpRecordingMessageHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        var probe = new TestableUploadRabbitMqDefinitions(httpClient)
        {
            FileContents = "{}",
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                DefinitionsJson = "{}",
                DefinitionsFilePath = "defs.json"
            },
            Context = Globals.Context
        };

        Assert.Throws<InvalidOperationException>(() => probe.Run([], []));
    }

    [Test]
    public void DownloadRabbitMqDefinitions_ShouldWriteReturnedDefinitionsToConfiguredPath()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"exchanges":[{"name":"events"}]}""")
        });
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableDownloadRabbitMqDefinitions(httpClient)
        {
            Configuration = new DownloadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                DefinitionsFilePath = "artifacts/definitions.json",
                VirtualHostName = "/"
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/definitions/%2F"));
        Assert.That(probe.WrittenPath, Is.EqualTo("artifacts/definitions.json"));
        Assert.That(NormalizeJson(probe.WrittenContents!), Is.EqualTo(NormalizeJson("""{"exchanges":[{"name":"events"}]}""")));
    }

    [Test]
    public void CreateRabbitMqVirtualHosts_ShouldPutConfiguredVirtualHostPayload()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableCreateRabbitMqVirtualHosts(httpClient)
        {
            Configuration = new CreateRabbitMqVirtualHostsConfig
            {
                Host = "rabbit-host",
                VirtualHosts =
                [
                    new RabbitMqVirtualHostConfig
                    {
                        Name = "/",
                        Description = "default vhost",
                        Tags = ["qa", "shared"],
                        DefaultQueueType = "quorum",
                        ProtectedFromDeletion = true,
                        Tracing = false
                    }
                ]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/vhosts/%2F"));
        Assert.That(NormalizeJson(request.Body!), Is.EqualTo(NormalizeJson(
            """{"description":"default vhost","tags":"qa,shared","default_queue_type":"quorum","protected_from_deletion":true,"tracing":false}""")));
    }

    [Test]
    public void DeleteRabbitMqVirtualHosts_ShouldDeleteEachConfiguredVirtualHost()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableDeleteRabbitMqVirtualHosts(httpClient)
        {
            Configuration = new DeleteRabbitMqVirtualHostsConfig
            {
                Host = "rabbit-host",
                VirtualHostNames = ["/", "qa"]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(handler.Requests.Select(request => request.RequestUri),
            Is.EqualTo(new[]
            {
                "http://rabbit-host:15672/api/vhosts/%2F",
                "http://rabbit-host:15672/api/vhosts/qa"
            }));
    }

    [Test]
    public void CreateRabbitMqUsers_ShouldPutConfiguredUserPayload()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableCreateRabbitMqUsers(httpClient)
        {
            Configuration = new CreateRabbitMqUsersConfig
            {
                Host = "rabbit-host",
                Users =
                [
                    new RabbitMqUserConfig
                    {
                        Username = "qa-user",
                        Password = "secret",
                        Tags = ["administrator", "monitoring"]
                    }
                ]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/users/qa-user"));
        Assert.That(NormalizeJson(request.Body!),
            Is.EqualTo(NormalizeJson("""{"password":"secret","tags":"administrator,monitoring"}""")));
    }

    [Test]
    public void DeleteRabbitMqUsers_ShouldDeleteEachConfiguredUser()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableDeleteRabbitMqUsers(httpClient)
        {
            Configuration = new DeleteRabbitMqUsersConfig
            {
                Host = "rabbit-host",
                Usernames = ["qa-user", "guest"]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(handler.Requests.Select(request => request.RequestUri),
            Is.EqualTo(new[]
            {
                "http://rabbit-host:15672/api/users/qa-user",
                "http://rabbit-host:15672/api/users/guest"
            }));
    }

    [Test]
    public void UpsertRabbitMqPermissions_ShouldPutConfiguredPermissionPayload()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableUpsertRabbitMqPermissions(httpClient)
        {
            Configuration = new UpsertRabbitMqPermissionsConfig
            {
                Host = "rabbit-host",
                Permissions =
                [
                    new RabbitMqPermissionConfig
                    {
                        VirtualHostName = "/",
                        Username = "qa-user",
                        Configure = "^qa\\.",
                        Write = ".*",
                        Read = "^events\\."
                    }
                ]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        var request = handler.Requests.Single();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(request.RequestUri, Is.EqualTo("http://rabbit-host:15672/api/permissions/%2F/qa-user"));
        Assert.That(NormalizeJson(request.Body!),
            Is.EqualTo(NormalizeJson("""{"configure":"^qa\\.","write":".*","read":"^events\\."}""")));
    }

    [Test]
    public void DeleteRabbitMqPermissions_ShouldDeleteEachConfiguredPermissionTarget()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        using var httpClient = CreateHttpClient(handler);

        var probe = new TestableDeleteRabbitMqPermissions(httpClient)
        {
            Configuration = new DeleteRabbitMqPermissionsConfig
            {
                Host = "rabbit-host",
                Permissions =
                [
                    new RabbitMqPermissionTargetConfig
                    {
                        VirtualHostName = "/",
                        Username = "qa-user"
                    },
                    new RabbitMqPermissionTargetConfig
                    {
                        VirtualHostName = "qa",
                        Username = "guest"
                    }
                ]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(handler.Requests.Select(request => request.RequestUri),
            Is.EqualTo(new[]
            {
                "http://rabbit-host:15672/api/permissions/%2F/qa-user",
                "http://rabbit-host:15672/api/permissions/qa/guest"
            }));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = new Uri("http://rabbit-host:15672/api/")
        };

    private static string NormalizeJson(string json)
        => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);
}
