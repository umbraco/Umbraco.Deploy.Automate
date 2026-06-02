namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Output data produced by <see cref="AddToQueueAction"/>.
/// </summary>
public sealed class AddToQueueOutput
{
    public required string AddedUdi { get; init; }
    public required int QueueSize { get; init; }
}
