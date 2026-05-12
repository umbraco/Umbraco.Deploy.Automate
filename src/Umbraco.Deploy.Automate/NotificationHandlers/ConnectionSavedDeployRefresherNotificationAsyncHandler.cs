using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class ConnectionSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase<Connection, ConnectionSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        DeployAutomateConstants.UdiEntityType.Connection)
{
    /// <inheritdoc />
    protected override Connection GetEntity(ConnectionSavedNotification notification) => notification.Connection;
}
