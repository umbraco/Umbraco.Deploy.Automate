namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="FilesDeletedTrigger"/>.
/// </summary>
public sealed class FilesDeletedTriggerOutput
{
    public required string UserEmail { get; init; }
    public required string UserName { get; init; }
    public required int FileCount { get; init; }
}
