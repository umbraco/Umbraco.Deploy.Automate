using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a deployment task completes successfully.
/// </summary>
[Trigger("umbracodeploy.taskCompleted", "Deployment Succeeded",
    Description = "Fires when a deployment task completes successfully.",
    Group = "Deploy",
    Icon = "icon-check")]
public sealed class TaskCompletedTrigger
    : NotificationTriggerBase<object, TaskCompletedTriggerOutput, TaskCompletedNotification>
{
    public TaskCompletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(TaskCompletedNotification notification)
    {
        var workItem = notification.WorkItem;
        yield return new TriggerEvent<TaskCompletedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new TaskCompletedTriggerOutput
            {
                WorkItemId = workItem.Id,
                WorkItemType = notification.WorkItemType,
                OwnerName = workItem.OwnerName,
                OwnerEmail = workItem.OwnerEmail,
                EventTrigger = workItem.EventTrigger,
                Duration = workItem.Duration,
                WorkCount = workItem.WorkCount,
                ProcessCount = workItem.ProcessCount,
            },
        };
    }
}
