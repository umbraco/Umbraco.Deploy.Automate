using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when user-related deploy files are updated on disk.
/// </summary>
[Trigger("umbracoDeploy.userUpdated", "User Files Updated",
    Description = "Fires when user-related deploy files are updated on disk.",
    Group = "Deploy",
    Icon = "icon-user")]
public sealed class UserUpdatedTrigger
    : NotificationTriggerBase<object, UserUpdatedTriggerOutput, UserUpdatedNotification>
{
    public UserUpdatedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(UserUpdatedNotification notification)
    {
        yield return new TriggerEvent<UserUpdatedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "user",
            Output = new UserUpdatedTriggerOutput
            {
                UserEmail = notification.UserEmail,
                UserName = notification.UserName,
                FileCount = notification.References.Count(),
            },
        };
    }
}
