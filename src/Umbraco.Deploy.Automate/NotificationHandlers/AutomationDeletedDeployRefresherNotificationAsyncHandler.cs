using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class AutomationDeletedDeployRefresherNotificationAsyncHandler(
    IDiskEntityService diskEntityService,
    ISignatureService signatureService,
    IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
    IOptionsMonitor<DeploySettings> deploySettings)
    : AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase<Automation, AutomationDeletedNotification>(
        diskEntityService,
        signatureService,
        deployEntityTypes,
        deploySettings,
        DeployAutomateConstants.UdiEntityType.Automation)
{
    /// <inheritdoc />
    protected override Guid GetEntityId(AutomationDeletedNotification notification) => notification.Automation.Id;
}
