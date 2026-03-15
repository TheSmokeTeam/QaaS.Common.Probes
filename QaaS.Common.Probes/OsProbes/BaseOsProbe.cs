using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using k8s;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Extensions;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Hooks.Probe;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.OsProbes;

public abstract class BaseOsProbe<TOsProbeConfig> : BaseProbe<TOsProbeConfig>, IDisposable where
    TOsProbeConfig : OsProbeConfig, new()
{
    protected Kubernetes? Kubernetes;

    [ExcludeFromCodeCoverage]
    protected virtual Kubernetes CreateConnection()
    {
        var k8SClient = OpenshiftAuthentication.CreateKubernetesClient(Configuration.Openshift!.Cluster!,
            Configuration.Openshift.Username!, Configuration.Openshift.Password!);
        return k8SClient;
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        Kubernetes = CreateConnection();
        RunOsProbe();
    }

    protected abstract void RunOsProbe();

    public void Dispose()
    {
        Kubernetes?.Dispose();
    }
}
