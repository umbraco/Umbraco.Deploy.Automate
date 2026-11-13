using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

internal sealed class AutomationSavedDeployRefresherNotificationAsyncHandler(
    IServiceConnectorFactory serviceConnectorFactory,
    IDiskEntityService diskEntityService,
    ISignatureService signatureService,
    IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
    IOptionsMonitor<DeploySettings> deploySettings)
    : AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase<Automation, AutomationSavedNotification>(
        serviceConnectorFactory,
        diskEntityService,
        signatureService,
        deployEntityTypes,
        deploySettings,
        DeployAutomateConstants.UdiEntityType.Automation)
{
    /// <inheritdoc />
    protected override Automation GetEntity(AutomationSavedNotification notification) => notification.Automation;
}
