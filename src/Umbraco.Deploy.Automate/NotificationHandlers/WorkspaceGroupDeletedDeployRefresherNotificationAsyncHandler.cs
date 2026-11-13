using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class WorkspaceGroupDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService,
    IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
    IOptionsMonitor<DeploySettings> deploySettings)
    : AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase<WorkspaceGroup, WorkspaceGroupDeletedNotification>(
        diskEntityService,
        signatureService,
        deployEntityTypes,
        deploySettings,
        DeployAutomateConstants.UdiEntityType.WorkspaceGroup)
{
    /// <inheritdoc />
    protected override Guid GetEntityId(WorkspaceGroupDeletedNotification notification) => notification.WorkspaceGroup.Id;
}
