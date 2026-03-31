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
    : BaseRabbitMqManagementObjectsManipulationWithGlobalDictDefaults<DeleteRabbitMqUsersConfig, string>
{
    protected override IEnumerable<string> GetObjectsToManipulateConfigurations() => Configuration.Usernames!;

    public override void Run(IImmutableList<SessionData> sessionDataList, IImmutableList<DataSource> dataSourceList)
    {
        base.Run(sessionDataList, dataSourceList);
        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery",
                new
                {
                    Users = Configuration.Usernames!
                        .Select(username => new RabbitMqUserConfig { Username = username })
                        .ToArray()
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
}
