using System.Net;
using System.Reflection;
using k8s;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsEditYamlConfigMapTests
{
    private sealed class TestableOsEditYamlConfigMap : OsEditYamlConfigMap
    {
        public void SetClient(Kubernetes kubernetesClient) => Kubernetes = kubernetesClient;

        public void InvokeRunOsProbe() => RunOsProbe();
    }

    [Test]
    public void RunOsProbe_WhenConfigMapExists_ShouldUpdateYamlAndReplaceConfigMap()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/configmaps/app-config",
            """
            {
              "apiVersion": "v1",
              "kind": "ConfigMap",
              "metadata": { "name": "app-config", "namespace": "namespace" },
              "data": {
                "app.yaml": "service:\n  enabled: true\nitems:\n  - old-value\n"
              }
            }
            """);
        server.EnqueueJsonResponse(
            "PUT",
            "/api/v1/namespaces/namespace/configmaps/app-config",
            """
            {
              "apiVersion": "v1",
              "kind": "ConfigMap",
              "metadata": { "name": "app-config", "namespace": "namespace" },
              "data": {
                "app.yaml": "service:\n  enabled: false\nitems:\n  - new-value\n"
              }
            }
            """,
            assertBody: body =>
            {
                Assert.That(body, Does.Contain("enabled: false"));
                Assert.That(body, Does.Contain("- new-value"));
            });

        var probe = new TestableOsEditYamlConfigMap
        {
            Configuration = new OsEditYamlConfigMapConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ConfigMapName = "app-config",
                ConfigMapYamlFileName = "app.yaml",
                ValuesToEdit = new Dictionary<string, object>
                {
                    ["service.enabled"] = false,
                    ["items[0]"] = "new-value",
                    ["service.missing"] = "ignored"
                }
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
    }

    [Test]
    public void RunOsProbe_WhenConfigMapIsMissing_ShouldThrowArgumentException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/configmaps/missing-config",
            "{\"message\":\"missing\"}",
            HttpStatusCode.NotFound);

        var probe = new TestableOsEditYamlConfigMap
        {
            Configuration = new OsEditYamlConfigMapConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ConfigMapName = "missing-config",
                ConfigMapYamlFileName = "app.yaml"
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var exception = Assert.Throws<ArgumentException>(() => probe.InvokeRunOsProbe());
        Assert.That(exception!.Message, Does.Contain("Could not find configmap 'missing-config'"));
    }

    [Test]
    public void RunOsProbe_WhenYamlEntryIsMissing_ShouldThrowArgumentException()
    {
        using var server = new TestHttpServer();
        server.EnqueueJsonResponse(
            "GET",
            "/api/v1/namespaces/namespace/configmaps/app-config",
            """
            {
              "apiVersion": "v1",
              "kind": "ConfigMap",
              "metadata": { "name": "app-config", "namespace": "namespace" },
              "data": {
                "other.yaml": "service:\n  enabled: true\n"
              }
            }
            """);

        var probe = new TestableOsEditYamlConfigMap
        {
            Configuration = new OsEditYamlConfigMapConfig
            {
                Openshift = CreateOpenshiftConfig(),
                ConfigMapName = "app-config",
                ConfigMapYamlFileName = "app.yaml"
            },
            Context = Globals.Context
        };
        probe.SetClient(server.CreateKubernetesClient());

        var exception = Assert.Throws<ArgumentException>(() => probe.InvokeRunOsProbe());
        Assert.That(exception!.Message, Does.Contain("Could not find yaml file 'app.yaml'"));
    }

    [Test]
    public void ConvertToDotNetObject_ShouldHandleJsonObjectArrayScalarAndPassthroughValues()
    {
        var method = typeof(OsEditYamlConfigMap)
            .GetMethod("ConvertToDotNetObject", BindingFlags.NonPublic | BindingFlags.Static)!;

        var objectResult = method.Invoke(null, [JObject.Parse("""{"service":{"enabled":false}}""")]);
        var objectDictionary = objectResult as Dictionary<string, object>;

        var arrayResult = method.Invoke(null, [JArray.Parse("""["alpha", {"nested": true}]""")]);
        var arrayValues = ((IEnumerable<object?>)arrayResult!).ToList();

        var scalarResult = method.Invoke(null, [new JValue("value")]);
        var passthroughResult = method.Invoke(null, [42]);

        Assert.That(objectDictionary, Is.Not.Null);
        Assert.That(objectDictionary!["service"], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(arrayValues, Has.Count.EqualTo(2));
        Assert.That(arrayValues[0], Is.EqualTo("alpha"));
        Assert.That(arrayValues[1], Is.TypeOf<Dictionary<string, object>>());
        Assert.That(scalarResult, Is.EqualTo("value"));
        Assert.That(passthroughResult, Is.EqualTo(42));
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
