using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when content files are deleted from disk.
/// </summary>
[Trigger("umbracodeploy.filesDeleted", "Files Deleted from Disk",
    Description = "Fires when content files are deleted from disk.",
    Group = "Deploy",
    Icon = "icon-trash")]
public sealed class FilesDeletedTrigger
    : NotificationTriggerBase<object, FilesDeletedTriggerOutput, FilesDeletedNotification>
{
    public FilesDeletedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(FilesDeletedNotification notification)
    {
        yield return new TriggerEvent<FilesDeletedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "user",
            Output = new FilesDeletedTriggerOutput
            {
                UserEmail = notification.UserEmail,
                UserName = notification.UserName,
                FileCount = notification.References.Count(),
            },
        };
    }
}
