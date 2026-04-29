namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="TaskCompletedTrigger"/>.
/// </summary>
public sealed class TaskCompletedTriggerOutput
{
    public required Guid WorkItemId { get; init; }
    public required string WorkItemType { get; init; }
    public required string OwnerName { get; init; }
    public required string OwnerEmail { get; init; }
    public required string EventTrigger { get; init; }
    public required double Duration { get; init; }
    public required int WorkCount { get; init; }
    public required int ProcessCount { get; init; }
}
