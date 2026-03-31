using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ user permissions in one or more virtual hosts through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Permissions" />
public class DeleteRabbitMqPermissions
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDict<DeleteRabbitMqPermissionsConfig, RabbitMqPermissionTargetConfig>
{
    protected override IEnumerable<RabbitMqPermissionTargetConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Permissions!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery",
                new
                {
                    Permissions = Configuration.Permissions!
                        .Select(permission => new RabbitMqPermissionConfig
                        {
                            Username = permission.Username,
                            VirtualHostName = permission.VirtualHostName
                        })
                        .ToArray()
                },
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Permissions"));
        }
    }

    protected override Task ManipulateObjectAsync(HttpClient httpClient,
        RabbitMqPermissionTargetConfig objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq permissions for user {Username} in virtual host {VirtualHostName}",
            objectToManipulateConfig.Username, objectToManipulateConfig.VirtualHostName);

        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"permissions/{EncodePathSegment(objectToManipulateConfig.VirtualHostName!)}/{EncodePathSegment(objectToManipulateConfig.Username!)}");
    }
}
