using Umbraco.Automate.Core.Settings;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Settings for <see cref="DeployContentAction"/>.
/// </summary>
public sealed class DeployContentSettings
{
    [Field(Label = "UDI", Description = "The UDI of the content to deploy (e.g. umb://document/abc123).", SupportsBindings = true)]
    public string Udi { get; set; } = string.Empty;

    [Field(Label = "Target URL", Description = "The root URL of the target environment (e.g. https://live.mysite.com).", SortOrder = 1, SupportsBindings = true)]
    public string TargetUrl { get; set; } = string.Empty;

    [Field(Label = "Ignore Dependencies", Description = "Deploy only the specified item without following its dependencies.", SortOrder = 2)]
    public bool IgnoreDependencies { get; set; }
}
