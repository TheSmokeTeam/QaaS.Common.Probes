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
    private UpsertRabbitMqPermissionsConfig? _previousDefaults;
    private PermissionRecoveryPayload? _previousRecovery;

    protected override IEnumerable<RabbitMqPermissionTargetConfig> GetObjectsToManipulateConfigurations()
        => Configuration.Permissions!;

    protected override void SaveResolvedConfigurationDefaults()
    {
        if (Configuration.UseGlobalDict)
        {
            _previousRecovery = RabbitMqRecoverySnapshotHelper.TryGetRecoveryPayload<PermissionRecoveryPayload>(Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Permissions"));
            _previousDefaults =
                RabbitMqRecoverySnapshotHelper.TryGetConfigurationDefaults<UpsertRabbitMqPermissionsConfig>(Context,
                    BuildGlobalDictionaryAliasPath("RabbitMq", "ManagementDefaults"));
        }

        base.SaveResolvedConfigurationDefaults();
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            var requestedTargets = Configuration.Permissions!;
            var requestedKeys = requestedTargets
                .Select(permission => $"{permission.VirtualHostName}::{permission.Username}")
                .ToHashSet(StringComparer.Ordinal);
            var permissions = (_previousRecovery?.Permissions ?? _previousDefaults?.Permissions ?? [])
                .Where(permission =>
                    !string.IsNullOrWhiteSpace(permission.VirtualHostName) &&
                    !string.IsNullOrWhiteSpace(permission.Username) &&
                    requestedKeys.Contains($"{permission.VirtualHostName}::{permission.Username}"))
                .ToArray();
            if (permissions.Length == 0)
            {
                permissions = requestedTargets
                    .Select(permission => new RabbitMqPermissionConfig
                    {
                        Username = permission.Username,
                        VirtualHostName = permission.VirtualHostName
                    })
                    .ToArray();
            }

            SaveGlobalDictionaryPayload("recovery",
                new PermissionRecoveryPayload
                {
                    Permissions = permissions
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

    private sealed record PermissionRecoveryPayload
    {
        public RabbitMqPermissionConfig[]? Permissions { get; init; }
    }
}
