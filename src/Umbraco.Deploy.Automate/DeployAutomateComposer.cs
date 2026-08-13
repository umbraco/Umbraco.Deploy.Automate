using Microsoft.Extensions.DependencyInjection;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.NotificationHandlers;
using Umbraco.Deploy.Automate.Workspaces;

namespace Umbraco.Deploy.Automate;

/// <summary>
/// Registers Deploy Automate with the Umbraco composition pipeline:
/// UDI types, configuration, the disk/transfer registration component, and notification handlers
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
        RegisterUdiTypes();

        // Configuration
        builder.Services.AddOptions<DeployAutomateSettings>()
            .Bind(builder.Config.GetSection("Umbraco:Deploy:Automate"));

        builder.Services.AddSingleton<DeployAutomateSettingsAccessor>();

        // Persists workspace groups during Deploy restore, bypassing the interactive
        // validators (workspace existence, parent existence, unique name).
        builder.Services.AddTransient<IWorkspaceGroupDeploySaver, WorkspaceGroupDeploySaver>();

        // Register component for disk and transfer entity type registration
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

    /// <remarks>
    /// Registered during composition rather than from <see cref="DeployAutomateComponent"/>, because
    /// components do not initialize until after migrations on an upgrade boot
    /// (<c>CoreRuntime.StartAsync</c> returns early at <c>RuntimeLevel.Upgrading</c>, leaving
    /// <c>UnattendedUpgradeBackgroundService</c> to initialize them). Deploy's work-item worker starts
    /// in that window, so registering later meant it could read an Automate artifact before
    /// <see cref="UdiParser"/> knew these types and throw <see cref="FormatException"/> on the UDI.
    /// These calls are static and need no services, so composition is early enough to close that gap.
    /// </remarks>
    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Workspace, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
    }
}
