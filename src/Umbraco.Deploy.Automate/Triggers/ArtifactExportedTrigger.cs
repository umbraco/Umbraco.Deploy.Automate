using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires after a content artifact has been exported.
/// </summary>
[Trigger("umbracoDeploy.artifactExported", "Content Exported",
    Description = "Fires after a content artifact has been exported.",
    Group = "Deploy",
    Icon = "icon-cloud-upload")]
public sealed class ArtifactExportedTrigger
    : NotificationTriggerBase<object, ArtifactExportedTriggerOutput, ArtifactExportedNotification>
{
    public ArtifactExportedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(ArtifactExportedNotification notification)
    {
        var artifact = notification.Artifact;
        yield return new TriggerEvent<ArtifactExportedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new ArtifactExportedTriggerOutput
            {
                ArtifactUdi = artifact.Udi.ToString(),
                ArtifactType = artifact.Udi.EntityType,
                ArtifactAlias = artifact.Alias,
                ArtifactName = artifact.Name ?? string.Empty,
            },
        };
    }
}
