using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Deploy.Automate;

/// <summary>
/// Registers Deploy Automate with the Umbraco composition pipeline.
/// </summary>
/// <remarks>
/// Unlike Engage.Automate or Commerce.Automate, no bridge handlers are required here
/// because Deploy notifications already implement <c>INotification</c> and are published
/// directly through the Umbraco CMS notification pipeline. The Automate framework
/// auto-discovers trigger classes via the <c>[Trigger]</c> attribute.
/// </remarks>
public sealed class DeployAutomateComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
    }
}
