namespace Umbraco.Deploy.Automate.Triggers.Outputs;

/// <summary>
/// Output data produced by <see cref="DiskTriggeredTrigger"/>.
/// </summary>
public sealed class DiskTriggeredTriggerOutput
{
    public required string Result { get; init; }
    public required string ExceptionType { get; init; }
}
