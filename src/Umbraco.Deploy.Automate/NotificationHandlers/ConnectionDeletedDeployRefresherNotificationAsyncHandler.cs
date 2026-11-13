using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class ConnectionDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService,
    IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
    IOptionsMonitor<DeploySettings> deploySettings)
    : AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase<Connection, ConnectionDeletedNotification>(
        diskEntityService,
        signatureService,
        deployEntityTypes,
        deploySettings,
        DeployAutomateConstants.UdiEntityType.Connection)
{
    /// <inheritdoc />
    protected override Guid GetEntityId(ConnectionDeletedNotification notification) => notification.Connection.Id;
}
