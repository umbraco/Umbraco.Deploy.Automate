using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Deploy.Automate.Tree;
using Umbraco.Deploy.Infrastructure.Disk;
using Umbraco.Deploy.Infrastructure.Transfer;

namespace Umbraco.Deploy.Automate;

/// <summary>
/// Component for registering Umbraco Deploy Automate UDI types, disk entity types
/// and transfer entity types (which enable Queue / Tree Restore / Partial Restore options in the back-office).
/// </summary>
public class DeployAutomateComponent(
    IDiskEntityService diskEntityService,
    ITransferEntityService transferEntityService) : IAsyncComponent
{
    /// <inheritdoc />
    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        RegisterUdiTypes();
        RegisterDiskEntityTypes();
        RegisterTransferEntityTypes();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Workspace, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
    }

    private void RegisterDiskEntityTypes()
    {
        diskEntityService.RegisterDiskEntityType(DeployAutomateConstants.UdiEntityType.Connection);
        diskEntityService.RegisterDiskEntityType(DeployAutomateConstants.UdiEntityType.Workspace);
        diskEntityService.RegisterDiskEntityType(DeployAutomateConstants.UdiEntityType.WorkspaceGroup);
        diskEntityService.RegisterDiskEntityType(DeployAutomateConstants.UdiEntityType.Automation);
    }

    private void RegisterTransferEntityTypes()
    {
        transferEntityService.RegisterTransferEntityType(
            DeployAutomateConstants.UdiEntityType.Workspace,
            new DeployRegisteredEntityTypeDetailOptions
            {
                SupportsQueueForTransfer = true,
                SupportsQueueForTransferOfDescendents = true,
                SupportsRestore = true,
                PermittedToRestore = true,
                SupportsPartialRestore = true,
                SupportsImportExport = true,
            },
            null,
            new DeployTransferRegisteredEntityTypeDetail.RemoteTreeDetail(
                UmbracoAutomateTreeHelper.GetWorkspaceTree,
                "Automate workspaces",
                DeployAutomateConstants.UdiEntityType.Workspace));

        transferEntityService.RegisterTransferEntityType(
            DeployAutomateConstants.UdiEntityType.WorkspaceGroup,
            new DeployRegisteredEntityTypeDetailOptions
            {
                SupportsQueueForTransfer = true,
                SupportsQueueForTransferOfDescendents = true,
                SupportsRestore = true,
                PermittedToRestore = true,
                SupportsPartialRestore = true,
                SupportsImportExport = true,
            },
            null,
            new DeployTransferRegisteredEntityTypeDetail.RemoteTreeDetail(
                UmbracoAutomateTreeHelper.GetAutomationTree,
                "Automate folders",
                DeployAutomateConstants.UdiEntityType.WorkspaceGroup));

        transferEntityService.RegisterTransferEntityType(
            DeployAutomateConstants.UdiEntityType.Automation,
            new DeployRegisteredEntityTypeDetailOptions
            {
                SupportsQueueForTransfer = true,
                SupportsRestore = true,
                PermittedToRestore = true,
                SupportsPartialRestore = true,
                SupportsImportExport = true,
            },
            null,
            new DeployTransferRegisteredEntityTypeDetail.RemoteTreeDetail(
                UmbracoAutomateTreeHelper.GetAutomationTree,
                "Automations",
                DeployAutomateConstants.UdiEntityType.Automation));

        transferEntityService.RegisterTransferEntityType(
            DeployAutomateConstants.UdiEntityType.Connection,
            new DeployRegisteredEntityTypeDetailOptions
            {
                SupportsQueueForTransfer = true,
                SupportsRestore = true,
                PermittedToRestore = true,
                SupportsPartialRestore = true,
                SupportsImportExport = true,
            },
            null,
            new DeployTransferRegisteredEntityTypeDetail.RemoteTreeDetail(
                UmbracoAutomateTreeHelper.GetConnectionTree,
                "Automate connections",
                DeployAutomateConstants.UdiEntityType.Connection));
    }
}
