using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Shared management-API object-manipulation base for RabbitMQ probes that iterate over a configured object set.
/// </summary>
public abstract class BaseRabbitMqManagementObjectsManipulationWithGlobalDict<TRabbitMqManagementConfig,
    TObjectToManipulateConfig> : BaseRabbitMqManagementProbeWithGlobalDict<TRabbitMqManagementConfig>
    where TRabbitMqManagementConfig : BaseRabbitMqManagementConfig, new()
{
    protected override void RunRabbitMqManagementProbe(HttpClient httpClient)
    {
        foreach (var objectToManipulateConfig in GetObjectsToManipulateConfigurations())
        {
            ManipulateObjectAsync(httpClient, objectToManipulateConfig).GetAwaiter().GetResult();
        }
    }

    protected abstract IEnumerable<TObjectToManipulateConfig> GetObjectsToManipulateConfigurations();

    protected abstract Task ManipulateObjectAsync(HttpClient httpClient,
        TObjectToManipulateConfig objectToManipulateConfig);
}
