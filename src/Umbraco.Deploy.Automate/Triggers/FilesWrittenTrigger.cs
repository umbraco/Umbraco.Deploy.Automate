using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when content files are written to disk.
/// </summary>
[Trigger("umbracoDeploy.filesWritten", "Files Written to Disk",
    Description = "Fires when content files are written to disk.",
    Group = "Deploy",
    Icon = "icon-document")]
public sealed class FilesWrittenTrigger
    : NotificationTriggerBase<object, FilesWrittenTriggerOutput, FilesWrittenNotification>
{
    public FilesWrittenTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(FilesWrittenNotification notification)
    {
        yield return new TriggerEvent<FilesWrittenTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "user",
            Output = new FilesWrittenTriggerOutput
            {
                UserEmail = notification.UserEmail,
                UserName = notification.UserName,
                FileCount = notification.References.Count(),
            },
        };
    }
}
