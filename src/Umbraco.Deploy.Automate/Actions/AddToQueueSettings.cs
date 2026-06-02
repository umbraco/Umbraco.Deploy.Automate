using Umbraco.Automate.Core.Settings;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Settings for <see cref="AddToQueueAction"/>.
/// </summary>
public sealed class AddToQueueSettings
{
    [Field(Label = "UDI", Description = "The UDI of the item to add to the queue (e.g. umb://document/abc123).", SupportsBindings = true)]
    public string Udi { get; set; } = string.Empty;

    [Field(Label = "User ID", Description = "The integer ID of the Umbraco backoffice user whose queue to add the item to.", SortOrder = 1, SupportsBindings = true)]
    public string UserId { get; set; } = string.Empty;

    [Field(Label = "Culture", Description = "Optional specific culture (language ISO code) to queue. Leave empty to queue all cultures.", SortOrder = 2, SupportsBindings = true)]
    public string? Culture { get; set; }
}
