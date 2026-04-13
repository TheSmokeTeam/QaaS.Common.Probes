using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.RabbitMq;
using QaaS.Framework.SDK.DataSourceObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;

namespace QaaS.Common.Probes.RabbitMqProbes;

/// <summary>
/// Deletes RabbitMQ users through the management API.
/// </summary>
/// <qaas-docs group="RabbitMQ administration" subgroup="Users lifecycle" />
public class DeleteRabbitMqUsers
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDict<DeleteRabbitMqUsersConfig, string>
{
    private CreateRabbitMqUsersConfig? _previousDefaults;
    private UserRecoveryPayload? _previousRecovery;

    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.Usernames!;

    protected override void SaveResolvedConfigurationDefaults()
    {
        if (Configuration.UseGlobalDict)
        {
            _previousRecovery = RabbitMqRecoverySnapshotHelper.TryGetRecoveryPayload<UserRecoveryPayload>(Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Users"));
            _previousDefaults = RabbitMqRecoverySnapshotHelper.TryGetConfigurationDefaults<CreateRabbitMqUsersConfig>(
                Context,
                BuildGlobalDictionaryAliasPath("RabbitMq", "ManagementDefaults"));
        }

        base.SaveResolvedConfigurationDefaults();
    }

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            var usernames = Configuration.Usernames!;
            var users = RabbitMqRecoverySnapshotHelper.FilterByNames(
                _previousRecovery?.Users ?? _previousDefaults?.Users,
                usernames,
                user => user.Username);
            if (users.Length == 0)
            {
                users = usernames
                    .Select(username => new RabbitMqUserConfig { Username = username })
                    .ToArray();
            }

            SaveGlobalDictionaryPayload("recovery",
                new UserRecoveryPayload
                {
                    Users = users
                },
                BuildGlobalDictionaryAliasPath("RabbitMq", "Recovery", "Users"));
        }
    }

    protected override Task ManipulateObjectAsync(HttpClient httpClient, string objectToManipulateConfig)
    {
        Context.Logger.LogDebug("Deleting rabbitmq user {Username}", objectToManipulateConfig);
        return SendManagementRequestAsync(httpClient, HttpMethod.Delete,
            $"users/{EncodePathSegment(objectToManipulateConfig)}");
    }

    private sealed record UserRecoveryPayload
    {
        public RabbitMqUserConfig[]? Users { get; init; }
    }
}
