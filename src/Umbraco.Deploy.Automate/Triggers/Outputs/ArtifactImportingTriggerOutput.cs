namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ArtifactImportingTrigger"/>.
/// Fires before the artifact is imported; the import has not yet occurred.
/// </summary>
/// <remarks>
/// Only <see cref="ArtifactUdi"/> and <see cref="ArtifactType"/> are available because the
/// pre-import notification exposes <c>IArtifactSignature</c> rather than <c>IArtifact</c>,
/// which does not carry <c>Name</c> or <c>Alias</c>.
/// </remarks>
public sealed class ArtifactImportingTriggerOutput
{
    public required string ArtifactUdi { get; init; }
    public required string ArtifactType { get; init; }
}
