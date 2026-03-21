using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Common.Probes.RabbitMqProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class RabbitMqManagementBaseProbeTests
{
    private sealed class TestableRabbitMqManagementProbe
        : BaseRabbitMqManagementProbe<UploadRabbitMqDefinitionsConfig>
    {
        public string? ObservedManagementApiBaseUrl { get; private set; }

        public HttpClient InvokeCreateHttpClient() => CreateHttpClient();

        public Task<string> InvokeSendManagementRequestAsync(HttpClient httpClient, HttpMethod method, string relativePath,
            string? jsonPayload = null)
            => SendManagementRequestAsync(httpClient, method, relativePath, jsonPayload);

        protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
        {
            ObservedManagementApiBaseUrl = ManagementApiBaseUrl;
        }
    }

    private sealed class TestableRabbitMqManagementObjectsProbe(HttpClient httpClient)
        : BaseRabbitMqManagementObjectsManipulation<DeleteRabbitMqUsersConfig, string>
    {
        public List<string> ManipulatedObjects { get; } = [];

        protected override HttpClient CreateHttpClient() => httpClient;

        protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => ["user-a", "user-b"];

        protected override Task ManipulateObjectAsync(HttpClient httpClient, string objectToManipulateConfig)
        {
            ManipulatedObjects.Add(objectToManipulateConfig);
            return Task.CompletedTask;
        }
    }

    [Test]
    public void CreateHttpClient_ShouldBuildManagementApiBaseUriAndAuthorizationHeader()
    {
        var probe = new TestableRabbitMqManagementProbe
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                Username = "user",
                Password = "pass",
                ManagementScheme = "https",
                ManagementPort = 15671
            },
            Context = Globals.Context
        };

        using var httpClient = probe.InvokeCreateHttpClient();

        Assert.That(httpClient.BaseAddress, Is.EqualTo(new Uri("https://rabbit-host:15671/api/")));
        Assert.That(httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
        Assert.That(httpClient.DefaultRequestHeaders.Authorization!.Scheme, Is.EqualTo("Basic"));
        Assert.That(httpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo("dXNlcjpwYXNz"));
    }

    [Test]
    public void CreateHttpClient_ShouldPreserveUnicodeCredentialsInAuthorizationHeader()
    {
        var probe = new TestableRabbitMqManagementProbe
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host",
                Username = "us\u00E9r",
                Password = "p\u00E4ss"
            },
            Context = Globals.Context
        };

        using var httpClient = probe.InvokeCreateHttpClient();

        Assert.That(httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
        Assert.That(httpClient.DefaultRequestHeaders.Authorization!.Parameter, Is.EqualTo("dXPDqXI6cMOkc3M="));
    }

    [Test]
    public void ManagementConfig_DefaultsToTlsValidationEnabled()
    {
        var configuration = new UploadRabbitMqDefinitionsConfig();

        Assert.That(configuration.AllowInvalidServerCertificates, Is.False);
    }

    [Test]
    public void Run_ShouldManipulateEveryConfiguredObject()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://rabbit-host:15672/api/")
        };

        var probe = new TestableRabbitMqManagementObjectsProbe(httpClient)
        {
            Configuration = new DeleteRabbitMqUsersConfig
            {
                Host = "rabbit-host",
                Usernames = ["user-a", "user-b"]
            },
            Context = Globals.Context
        };

        probe.Run([], []);

        Assert.That(probe.ManipulatedObjects, Is.EqualTo(new[] { "user-a", "user-b" }));
    }

    [Test]
    public async Task SendManagementRequestAsync_WhenResponseHasNoContentAndRequestHasNoPayload_ShouldReturnEmptyString()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://rabbit-host:15672/api/")
        };

        var probe = new TestableRabbitMqManagementProbe
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host"
            },
            Context = Globals.Context
        };

        var response = await probe.InvokeSendManagementRequestAsync(httpClient, HttpMethod.Get, "definitions");

        Assert.That(response, Is.Empty);
        Assert.That(handler.Requests.Single().Body, Is.Null);
    }

    [Test]
    public void SendManagementRequestAsync_WhenRequestFails_ShouldThrowInvalidOperationException()
    {
        var handler = new HttpRecordingMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request")
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://rabbit-host:15672/api/")
        };

        var probe = new TestableRabbitMqManagementProbe
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host"
            },
            Context = Globals.Context
        };

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await probe.InvokeSendManagementRequestAsync(httpClient, HttpMethod.Delete, "users/qa-user"));

        Assert.That(exception!.Message, Does.Contain("400"));
        Assert.That(exception.Message, Does.Contain("bad request"));
    }

    [Test]
    public void Run_WhenHttpClientHasNoBaseAddress_ShouldStoreEmptyManagementApiBaseUrl()
    {
        using var httpClient = new HttpClient(new HttpRecordingMessageHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        var probe = new TestableRabbitMqManagementProbe
        {
            Configuration = new UploadRabbitMqDefinitionsConfig
            {
                Host = "rabbit-host"
            },
            Context = Globals.Context
        };

        var overridingProbe = new TestableRabbitMqManagementProbeWithoutBaseAddress(httpClient)
        {
            Configuration = probe.Configuration,
            Context = probe.Context
        };

        overridingProbe.Run([], []);

        Assert.That(overridingProbe.ObservedManagementApiBaseUrl, Is.EqualTo(string.Empty));
    }

    private sealed class TestableRabbitMqManagementProbeWithoutBaseAddress(HttpClient httpClient)
        : BaseRabbitMqManagementProbe<UploadRabbitMqDefinitionsConfig>
    {
        public string? ObservedManagementApiBaseUrl { get; private set; }

        protected override HttpClient CreateHttpClient() => httpClient;

        protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
        {
            ObservedManagementApiBaseUrl = ManagementApiBaseUrl;
        }
    }
}
