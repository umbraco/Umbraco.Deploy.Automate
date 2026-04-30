using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires before a content artifact is imported.
/// </summary>
/// <remarks>
/// The underlying Deploy notification is cancelable, but cancellation is not supported
/// through the Automate trigger system. This trigger is for observation only.
/// </remarks>
[Trigger("umbracodeploy.artifactImporting", "Content Importing",
    Description = "Fires before a content artifact is imported.",
    Group = "Deploy",
    Icon = "icon-download")]
public sealed class ArtifactImportingTrigger
    : NotificationTriggerBase<object, ArtifactImportingTriggerOutput, ArtifactImportingNotification>
{
    public ArtifactImportingTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(ArtifactImportingNotification notification)
    {
        var artifact = notification.Artifact;
        yield return new TriggerEvent<ArtifactImportingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new ArtifactImportingTriggerOutput
            {
                ArtifactUdi = artifact.Udi.ToString(),
                ArtifactType = artifact.Udi.EntityType,
            },
        };
    }
}
