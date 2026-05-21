using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

using UmbracoConstants = Umbraco.Cms.Core.Constants;
namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires after a content artifact has been imported.
/// </summary>
[Trigger("umbracoDeploy.artifactImported", "Content Imported",
    Description = "Fires after a content artifact has been imported.",
    Group = "Deploy",
    Icon = "icon-download",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class ArtifactImportedTrigger
    : NotificationTriggerBase<object, ArtifactImportedTriggerOutput, ArtifactImportedNotification>
{
    public ArtifactImportedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(ArtifactImportedNotification notification)
    {
        var artifact = notification.Artifact;
        yield return new TriggerEvent<ArtifactImportedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new ArtifactImportedTriggerOutput
            {
                ArtifactUdi = artifact.Udi.ToString(),
                ArtifactType = artifact.Udi.EntityType,
                ArtifactAlias = artifact.Alias,
                ArtifactName = artifact.Name ?? string.Empty,
            },
        };
    }
}
