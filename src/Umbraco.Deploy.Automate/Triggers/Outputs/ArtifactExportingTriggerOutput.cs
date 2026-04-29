namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ArtifactExportingTrigger"/>.
/// Fires before the artifact is exported; the export has not yet occurred.
/// </summary>
public sealed class ArtifactExportingTriggerOutput
{
    public required string ArtifactUdi { get; init; }
    public required string ArtifactType { get; init; }
    public required string ArtifactAlias { get; init; }
    public required string ArtifactName { get; init; }
}
