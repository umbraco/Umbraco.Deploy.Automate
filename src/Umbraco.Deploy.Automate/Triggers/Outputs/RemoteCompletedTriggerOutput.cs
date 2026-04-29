namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="RemoteCompletedTrigger"/>.
/// </summary>
public sealed class RemoteCompletedTriggerOutput
{
    public required Guid WorkId { get; init; }
    public required string WorkItemType { get; init; }
    public required string WorkItemEnvironment { get; init; }
    public required int WorkCount { get; init; }
    public required int ProcessCount { get; init; }
    public required double Duration { get; init; }
}
