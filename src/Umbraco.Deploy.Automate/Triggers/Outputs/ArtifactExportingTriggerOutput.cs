namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ArtifactExportingTrigger"/>.
/// Fires before the artifact is exported; the export has not yet occurred.
/// </summary>
/// <remarks>
/// Only <see cref="ArtifactUdi"/> and <see cref="ArtifactType"/> are available because the
/// pre-export notification exposes <c>IArtifactSignature</c> rather than <c>IArtifact</c>,
/// which does not carry <c>Name</c> or <c>Alias</c>.
/// </remarks>
public sealed class ArtifactExportingTriggerOutput
{
    public required string ArtifactUdi { get; init; }
    public required string ArtifactType { get; init; }
}
