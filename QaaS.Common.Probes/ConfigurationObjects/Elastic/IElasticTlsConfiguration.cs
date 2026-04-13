namespace QaaS.Common.Probes.ConfigurationObjects.Elastic;

public interface IElasticTlsConfiguration
{
    bool AllowInvalidServerCertificates { get; }
}
