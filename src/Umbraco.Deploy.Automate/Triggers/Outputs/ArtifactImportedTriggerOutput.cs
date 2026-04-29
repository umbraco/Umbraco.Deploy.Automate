namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ArtifactImportedTrigger"/>.
/// </summary>
public sealed class ArtifactImportedTriggerOutput
{
    public required string ArtifactUdi { get; init; }
    public required string ArtifactType { get; init; }
    public required string ArtifactAlias { get; init; }
    public required string ArtifactName { get; init; }
}
