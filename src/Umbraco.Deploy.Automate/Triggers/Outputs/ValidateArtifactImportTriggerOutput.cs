namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="ValidateArtifactImportTrigger"/>.
/// </summary>
public sealed class ValidateArtifactImportTriggerOutput
{
    public required int ArtifactCount { get; init; }
    public required IEnumerable<string> ArtifactUdis { get; init; }
}
