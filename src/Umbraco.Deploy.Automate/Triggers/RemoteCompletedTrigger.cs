using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

using UmbracoConstants = Umbraco.Cms.Core.Constants;
namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a remote deploy operation completes.
/// </summary>
[Trigger("umbracoDeploy.remoteCompleted", "Remote Deploy Completed",
    Description = "Fires when a remote deploy operation completes.",
    Group = "Deploy",
    Icon = "icon-cloud",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class RemoteCompletedTrigger
    : NotificationTriggerBase<object, RemoteCompletedTriggerOutput, RemoteCompletedNotification>
{
    public RemoteCompletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(RemoteCompletedNotification notification)
    {
        var infos = notification.Infos;
        yield return new TriggerEvent<RemoteCompletedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new RemoteCompletedTriggerOutput
            {
                WorkId = infos.WorkId,
                WorkItemType = infos.WorkItemType,
                WorkItemEnvironment = infos.WorkItemEnvironment,
                WorkCount = infos.WorkCount,
                ProcessCount = infos.ProcessCount,
                Duration = infos.Duration,
            },
        };
    }
}
