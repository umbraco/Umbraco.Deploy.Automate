using Umbraco.Automate.Core.Settings;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Settings for <see cref="TriggerRestoreAction"/>.
/// </summary>
public sealed class TriggerRestoreSettings
{
    [Field(Label = "Source URL", Description = "The root URL of the source environment to restore from (e.g. https://live.mysite.com).", SupportsBindings = true)]
    public string SourceUrl { get; set; } = string.Empty;

    [Field(Label = "Ignore Dependencies", Description = "Restore only the specified items without following their dependencies.", SortOrder = 1)]
    public bool IgnoreDependencies { get; set; }
}
