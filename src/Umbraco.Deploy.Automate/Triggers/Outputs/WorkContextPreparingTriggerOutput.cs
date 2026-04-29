namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="WorkContextPreparingTrigger"/>.
/// </summary>
public sealed class WorkContextPreparingTriggerOutput
{
    public required Guid WorkItemId { get; init; }
    public required string WorkItemType { get; init; }
    public required string OwnerName { get; init; }
    public required string OwnerEmail { get; init; }
    public required string EventTrigger { get; init; }
}
