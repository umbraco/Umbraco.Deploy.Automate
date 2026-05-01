using Microsoft.Extensions.DependencyInjection;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.NotificationHandlers;
using Umbraco.Deploy.Automate.Workspaces;

namespace Umbraco.Deploy.Automate;

/// <summary>
/// Registers Deploy Automate with the Umbraco composition pipeline:
/// configuration, the disk/UDI/transfer registration component, and notification handlers
/// that keep on-disk artifacts in sync as Automate entities are saved or deleted.
/// </summary>
/// <remarks>
/// Unlike Engage.Automate or Commerce.Automate, no bridge handlers are required here
/// because Deploy notifications already implement <c>INotification</c> and are published
/// directly through the Umbraco CMS notification pipeline. The Automate framework
/// auto-discovers trigger classes via the <c>[Trigger]</c> attribute.
/// </remarks>
public sealed class DeployAutomateComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Configuration
        builder.Services.AddOptions<DeployAutomateSettings>()
            .Bind(builder.Config.GetSection("Umbraco:Deploy:Automate"));

        builder.Services.AddSingleton<DeployAutomateSettingsAccessor>();

        // Persists workspace groups during Deploy restore, bypassing the interactive
        // validators (workspace existence, parent existence, unique name).
        builder.Services.AddTransient<IWorkspaceGroupDeploySaver, WorkspaceGroupDeploySaver>();

        // Register component for UDI and disk entity type registration
        builder.Components()
            .Append<DeployAutomateComponent>();

        // Register notification handlers for automatic artifact management — Saved
        builder.AddNotificationAsyncHandler<ConnectionSavedNotification,
            ConnectionSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<WorkspaceSavedNotification,
            WorkspaceSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<WorkspaceGroupSavedNotification,
            WorkspaceGroupSavedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AutomationSavedNotification,
            AutomationSavedDeployRefresherNotificationAsyncHandler>();

        // Register notification handlers for automatic artifact management — Deleted
        builder.AddNotificationAsyncHandler<ConnectionDeletedNotification,
            ConnectionDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<WorkspaceDeletedNotification,
            WorkspaceDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<WorkspaceGroupDeletedNotification,
            WorkspaceGroupDeletedDeployRefresherNotificationAsyncHandler>();
        builder.AddNotificationAsyncHandler<AutomationDeletedNotification,
            AutomationDeletedDeployRefresherNotificationAsyncHandler>();
    }
}
