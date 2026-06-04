namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Output data produced by <see cref="DeployContentAction"/>.
/// </summary>
public sealed class DeployContentOutput
{
    public required Guid SessionId { get; init; }
    public required string Status { get; init; }
}
