using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using k8s;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Shared OpenShift probe base that resolves missing cluster settings from the probe global dictionary before creating
/// the Kubernetes client.
/// </summary>
public abstract class BaseOsProbeWithGlobalDict<TOsProbeConfig>
    : BaseProbeWithGlobalDict<TOsProbeConfig>, IDisposable
    where TOsProbeConfig : OsProbeConfig, new()
{
    protected Kubernetes? Kubernetes;

    [ExcludeFromCodeCoverage]
    protected virtual Kubernetes CreateConnection()
    {
        var k8SClient = OpenshiftAuthentication.CreateKubernetesClient(Configuration.Openshift!.Cluster!,
            Configuration.Openshift.Username!, Configuration.Openshift.Password!,
            Configuration.Openshift.AllowInvalidServerCertificates);
        return k8SClient;
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        Kubernetes = CreateConnection();
        RunOsProbe();
    }

    protected override IReadOnlyList<string> GetConfigurationDefaultsAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Defaults");

    protected abstract void RunOsProbe();

    public void Dispose()
    {
        Kubernetes?.Dispose();
    }
}
