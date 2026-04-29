using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a deployment task fails.
/// </summary>
[Trigger("umbracodeploy.taskFailed", "Deployment Failed",
    Description = "Fires when a deployment task fails.",
    Group = "Deploy",
    Icon = "icon-alert")]
public sealed class TaskFailedTrigger
    : NotificationTriggerBase<object, TaskFailedTriggerOutput, TaskFailedNotification>
{
    public TaskFailedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(TaskFailedNotification notification)
    {
        var workItem = notification.WorkItem;
        yield return new TriggerEvent<TaskFailedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new TaskFailedTriggerOutput
            {
                WorkItemId = workItem.Id,
                WorkItemType = notification.WorkItemType,
                OwnerName = workItem.OwnerName,
                OwnerEmail = workItem.OwnerEmail,
                EventTrigger = workItem.EventTrigger,
                Duration = workItem.Duration,
                ExceptionMessage = workItem.Exception?.Message,
                ExceptionType = workItem.Exception?.GetType().Name,
            },
        };
    }
}
