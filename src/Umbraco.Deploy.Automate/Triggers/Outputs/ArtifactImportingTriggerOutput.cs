namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ArtifactImportingTrigger"/>.
/// Fires before the artifact is imported; the import has not yet occurred.
/// </summary>
public sealed class ArtifactImportingTriggerOutput
{
    public required string ArtifactUdi { get; init; }
    public required string ArtifactType { get; init; }
    public required string ArtifactAlias { get; init; }
    public required string ArtifactName { get; init; }
}
