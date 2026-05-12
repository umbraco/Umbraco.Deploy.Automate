using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class ConnectionDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase<Connection, ConnectionDeletedNotification>(
        diskEntityService,
        signatureService,
        DeployAutomateConstants.UdiEntityType.Connection)
{
    /// <inheritdoc />
    protected override Guid GetEntityId(ConnectionDeletedNotification notification) => notification.Connection.Id;
}
