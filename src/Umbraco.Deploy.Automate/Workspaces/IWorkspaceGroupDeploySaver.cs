using Umbraco.Automate.Core.Workspaces;

namespace Umbraco.Deploy.Automate.Workspaces;

/// <summary>
/// Persists a <see cref="WorkspaceGroup"/> for Deploy restore, bypassing the interactive
/// validators (workspace existence, parent existence, unique name) that
/// <see cref="IWorkspaceGroupService.CreateGroupAsync"/> and
/// <see cref="IWorkspaceGroupService.UpdateGroupAsync"/> enforce. Deploy sequences artifacts
/// across processing passes, so a nested group may briefly land at root before being
/// re-parented and several same-named groups may coexist at root within a pass.
/// </summary>
public interface IWorkspaceGroupDeploySaver
{
    /// <summary>
    /// Saves the group via the repository and publishes the standard save notifications.
    /// Assigns a new ID when <see cref="WorkspaceGroup.Id"/> is empty.
    /// </summary>
    Task<WorkspaceGroup> SaveAsync(WorkspaceGroup group, CancellationToken cancellationToken = default);
}
