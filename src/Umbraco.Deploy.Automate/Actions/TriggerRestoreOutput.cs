namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Output data produced by <see cref="TriggerRestoreAction"/>.
/// </summary>
public sealed class TriggerRestoreOutput
{
    public required Guid SessionId { get; init; }
    public required string Status { get; init; }
}
