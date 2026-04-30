using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Triggers;

/// <summary>
/// Fires when a set of artifacts is being validated before import.
/// </summary>
/// <remarks>
/// The underlying Deploy notification is cancelable and supports adding validation differences,
/// but these advanced features are not available through the Automate trigger system.
/// This trigger is for observation only.
/// </remarks>
[Trigger("umbracodeploy.validateArtifactImport", "Artifact Import Validation",
    Description = "Fires when a set of artifacts is being validated before import.",
    Group = "Deploy",
    Icon = "icon-shield")]
public sealed class ValidateArtifactImportTrigger
    : NotificationTriggerBase<object, ValidateArtifactImportTriggerOutput, ValidateArtifactImportNotification>
{
    public ValidateArtifactImportTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(ValidateArtifactImportNotification notification)
    {
        var artifacts = notification.Artifacts.ToList();
        yield return new TriggerEvent<ValidateArtifactImportTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "system",
            Output = new ValidateArtifactImportTriggerOutput
            {
                ArtifactCount = artifacts.Count,
                ArtifactUdis = artifacts.Select(a => a.Udi.ToString()).ToList(),
            },
        };
    }
}
