namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="UserUpdatedTrigger"/>.
/// </summary>
public sealed class UserUpdatedTriggerOutput
{
    public required string UserEmail { get; init; }
    public required string UserName { get; init; }
    public required int FileCount { get; init; }
}
