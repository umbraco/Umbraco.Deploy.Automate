using Microsoft.Extensions.Options;

namespace Umbraco.Deploy.Automate.Configuration;

/// <summary>
/// Provides access to the current Umbraco Deploy Automate settings.
/// </summary>
public class DeployAutomateSettingsAccessor(IOptionsMonitor<DeployAutomateSettings> optionsMonitor)
{
    /// <summary>
    /// Gets the current Umbraco Deploy Automate settings.
    /// </summary>
    public virtual DeployAutomateSettings Settings => optionsMonitor.CurrentValue;
}
