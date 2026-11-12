using Microsoft.Extensions.DependencyInjection;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.NotificationHandlers;
using Umbraco.Deploy.Automate.Workspaces;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Transfer;

namespace Umbraco.Deploy.Automate;

/// <summary>
/// Registers Deploy Automate with the Umbraco composition pipeline:
/// configuration, the Deploy disk/transfer entity type registrations, and notification handlers
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

        builder.Services.Configure<DeployEntityTypesOptions>(ConfigureEntityTypes);

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

    /// <summary>
    /// Registers the Automate entity types with Deploy. An entity type can't be both a disk and a
    /// transfer type, so each is in exactly one set: workspaces and connections are settings-like and
    /// deploy as schema files (disk); automations and workspace groups (automation folders) travel
    /// like content (transfer).
    /// </summary>
    /// <remarks>
    /// UDI types need no registration here: Deploy registers them from the connectors' <c>[UdiDefinition]</c> attributes.
    /// </remarks>
    internal static void ConfigureEntityTypes(DeployEntityTypesOptions options)
    {
        options
            .AddDiskEntityType(DeployAutomateConstants.UdiEntityType.Workspace)
            .AddDiskEntityType(DeployAutomateConstants.UdiEntityType.Connection)
            .AddTransferEntityType(
                DeployAutomateConstants.UdiEntityType.WorkspaceGroup,
                new DeployTransferEntityTypeOptions
                {
                    SupportsQueueForTransfer = true,
                    SupportsQueueForTransferOfDescendents = true,
                    SupportsRestore = true,
                    PermittedToRestore = true,
                    SupportsPartialRestore = true,
                    SupportsImportExport = true,
                })
            .AddTransferEntityType(
                DeployAutomateConstants.UdiEntityType.Automation,
                new DeployTransferEntityTypeOptions
                {
                    SupportsQueueForTransfer = true,
                    SupportsRestore = true,
                    PermittedToRestore = true,
                    SupportsPartialRestore = true,
                    SupportsImportExport = true,
                });
    }
}
