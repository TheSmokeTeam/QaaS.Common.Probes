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
        public HttpClient InvokeCreateHttpClient() => CreateHttpClient();

        protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
        {
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
}
