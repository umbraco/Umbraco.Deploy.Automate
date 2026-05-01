using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class WorkspaceGroupSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService)
    : AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase<WorkspaceGroup, WorkspaceGroupSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        DeployAutomateConstants.UdiEntityType.WorkspaceGroup)
{
    /// <inheritdoc />
    protected override WorkspaceGroup GetEntity(WorkspaceGroupSavedNotification notification) => notification.WorkspaceGroup;
}
