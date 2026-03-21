using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;

namespace QaaS.Common.Probes.RabbitMqProbes;

public abstract class BaseRabbitMqManagementObjectsManipulation<TRabbitMqManagementConfig, TObjectToManipulateConfig>
    : BaseRabbitMqManagementProbe<TRabbitMqManagementConfig>
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
