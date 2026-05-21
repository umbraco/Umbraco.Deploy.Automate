using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

using UmbracoConstants = Umbraco.Cms.Core.Constants;
namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires before a content artifact is exported.
/// </summary>
/// <remarks>
/// The underlying Deploy notification is cancelable, but cancellation is not supported
/// through the Automate trigger system. This trigger is for observation only.
/// </remarks>
[Trigger("umbracoDeploy.artifactExporting", "Content Exporting",
    Description = "Fires before a content artifact is exported.",
    Group = "Deploy",
    Icon = "icon-cloud-upload",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class ArtifactExportingTrigger
    : NotificationTriggerBase<object, ArtifactExportingTriggerOutput, ArtifactExportingNotification>
{
    public ArtifactExportingTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(ArtifactExportingNotification notification)
    {
        var artifact = notification.Artifact;
        yield return new TriggerEvent<ArtifactExportingTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new ArtifactExportingTriggerOutput
            {
                ArtifactUdi = artifact.Udi.ToString(),
                ArtifactType = artifact.Udi.EntityType,
            },
        };
    }
}
