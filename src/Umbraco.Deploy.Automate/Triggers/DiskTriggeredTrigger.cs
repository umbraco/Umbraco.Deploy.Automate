using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a disk-triggered deploy operation completes.
/// </summary>
[Trigger("umbracoDeploy.diskTriggered", "Disk Deploy Completed",
    Description = "Fires when a disk-triggered deploy operation completes.",
    Group = "Deploy",
    Icon = "icon-hard-drive")]
public sealed class DiskTriggeredTrigger
    : NotificationTriggerBase<object, DiskTriggeredTriggerOutput, DiskTriggeredNotification>
{
    public DiskTriggeredTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(DiskTriggeredNotification notification)
    {
        yield return new TriggerEvent<DiskTriggeredTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new DiskTriggeredTriggerOutput
            {
                Result = notification.Result.ToString(),
                ExceptionType = notification.ExceptionType,
            },
        };
    }
}
