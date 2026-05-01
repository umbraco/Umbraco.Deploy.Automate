using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a deploy work context is being prepared before execution begins.
/// </summary>
[Trigger("umbracoDeploy.workContextPreparing", "Deploy Work Context Preparing",
    Description = "Fires when a deploy work context is being prepared before execution begins.",
    Group = "Deploy",
    Icon = "icon-settings")]
public sealed class WorkContextPreparingTrigger
    : NotificationTriggerBase<object, WorkContextPreparingTriggerOutput, WorkContextPreparingNotification>
{
    public WorkContextPreparingTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(WorkContextPreparingNotification notification)
    {
        var workItem = notification.WorkItem;
        yield return new TriggerEvent<WorkContextPreparingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new WorkContextPreparingTriggerOutput
            {
                WorkItemId = workItem.Id,
                WorkItemType = workItem.GetType().FullName!,
                OwnerName = workItem.OwnerName,
                OwnerEmail = workItem.OwnerEmail,
                EventTrigger = workItem.EventTrigger,
            },
        };
    }
}
