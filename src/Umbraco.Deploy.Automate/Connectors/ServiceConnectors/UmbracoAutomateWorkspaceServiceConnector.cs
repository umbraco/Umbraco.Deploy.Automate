using System.Runtime.CompilerServices;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Services;
using Umbraco.Deploy.Automate.Artifacts;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Automate Workspaces, responsible for synchronizing
/// workspace entities during deploy operations. Resolves Connection dependencies.
/// </summary>
[UdiDefinition(DeployAutomateConstants.UdiEntityType.Workspace, UdiType.GuidUdi)]
public class UmbracoAutomateWorkspaceServiceConnector(
    IWorkspaceService workspaceService,
    IWorkspaceGroupService groupService,
    IAutomationService automationService,
    IConnectionService connectionService,
    IUserGroupService userGroupService,
    DeployAutomateSettingsAccessor settingsAccessor)
    : UmbracoAutomateEntityServiceConnectorBase<AutomateWorkspaceArtifact, Workspace>(settingsAccessor)
{
    /// <inheritdoc />
    protected override int[] ProcessPasses => [3];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors =>
    [
        Constants.DeploySelector.This,
        Constants.DeploySelector.ThisAndDescendants,
        Constants.DeploySelector.DescendantsOfThis,
    ];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco Automate Workspaces";

    /// <inheritdoc />
    public override string UdiEntityType => DeployAutomateConstants.UdiEntityType.Workspace;

    /// <inheritdoc />
    public override async Task<Workspace?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => await workspaceService.GetWorkspaceAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<Workspace> GetEntitiesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var workspaces = await workspaceService.GetAllWorkspacesAsync(cancellationToken);
        foreach (var workspace in workspaces)
        {
            yield return workspace;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(Workspace entity) => entity.Name;

    /// <inheritdoc />
    public override Task<AutomateWorkspaceArtifact?> GetArtifactAsync(
        GuidUdi udi,
        Workspace? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AutomateWorkspaceArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        // Connection dependencies — the workspace's allowed-connections list.
        var connectionUdis = new List<GuidUdi>();
        foreach (var connectionId in entity.AllowedConnections)
        {
            var connectionUdi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connectionId);
            dependencies.Add(new UmbracoAutomateArtifactDependency(connectionUdi, ArtifactDependencyMode.Match));
            connectionUdis.Add(connectionUdi);
        }

        // User group dependencies — the workspace's access-control list.
        foreach (var userGroupKey in entity.UserGroups)
        {
            var userGroupUdi = new GuidUdi(Umbraco.Cms.Core.Constants.UdiEntityType.UserGroup, userGroupKey);
            dependencies.Add(new UmbracoAutomateArtifactDependency(userGroupUdi, ArtifactDependencyMode.Match));
        }

        var artifact = new AutomateWorkspaceArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            UserGroups = entity.UserGroups.ToList(),
            AllowedConnectionUdis = connectionUdis,
        };

        return Task.FromResult<AutomateWorkspaceArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AutomateWorkspaceArtifact, Workspace> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 3:
                await Pass3Async(state, cancellationToken);
                break;
        }
    }

    private async Task Pass3Async(
        ArtifactDeployState<AutomateWorkspaceArtifact, Workspace> state,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Resolve AllowedConnection UDIs back to IDs
        var allowedConnectionIds = new List<Guid>();
        foreach (var connectionUdi in artifact.AllowedConnectionUdis)
        {
            connectionUdi.EnsureType(DeployAutomateConstants.UdiEntityType.Connection);

            var connection = await connectionService.GetConnectionAsync(connectionUdi.Guid, cancellationToken);
            if (connection != null)
            {
                allowedConnectionIds.Add(connection.Id);
            }
        }

        // Only keep user groups that actually exist on this environment. Umbraco Deploy
        // doesn't guarantee user groups travel with the workspace (they're often excluded
        // from a schema deploy), so a stale key here would leave the workspace pointing at a
        // group that doesn't resolve, which breaks the picker in the same way an unresolved
        // ServiceAccountKey used to — see the frontend fix in Umbraco.Automate for the
        // matching client-side guard.
        var resolvedUserGroups = artifact.UserGroups.Count == 0
            ? new List<Guid>()
            : (await userGroupService.GetAsync(artifact.UserGroups)).Select(userGroup => userGroup.Key).ToList();

        if (state.Entity != null)
        {
            // Update existing workspace. ServiceAccountKey is deliberately NOT applied from
            // the artifact — see the note on AutomateWorkspaceArtifact.ServiceAccountKey. An
            // admin may have already configured a working service account on this
            // environment; overwriting it with the source environment's key here would
            // silently break every automation in the workspace.
            var workspace = state.Entity;
            workspace.Alias = artifact.Alias!;
            workspace.Name = artifact.Name;
            workspace.UserGroups = resolvedUserGroups;
            workspace.AllowedConnections = allowedConnectionIds;

            state.Entity = await workspaceService.UpdateWorkspaceAsync(workspace, cancellationToken: cancellationToken);
        }
        else
        {
            // Create new workspace, preserving the artifact's UDI so cross-environment
            // references resolve and redeployment stays idempotent. ServiceAccountKey is
            // left unset (Guid.Empty) — see the note on
            // AutomateWorkspaceArtifact.ServiceAccountKey — an admin must configure a real
            // service account for this environment after the first deploy.
            var workspace = new Workspace
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name,
                UserGroups = resolvedUserGroups,
                AllowedConnections = allowedConnectionIds,
            };

            state.Entity = await workspaceService.CreateWorkspaceAsync(workspace, cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Direct children of a workspace are its root-level groups and root-level automations
    /// (those with no parent group).
    /// </remarks>
    protected override async IAsyncEnumerable<GuidUdi> GetChildUdisAsync(
        Workspace entity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rootGroups = await groupService.GetGroupsByWorkspaceAsync(entity.Id, parentId: null, cancellationToken);
        foreach (var group in rootGroups)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, group.Id);
        }

        var (rootAutomations, _) = await automationService.GetAutomationsPagedAsync(
            workspaceIds: new HashSet<Guid> { entity.Id },
            groupId: Guid.Empty,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        foreach (var automation in rootAutomations)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, automation.Id);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Descendants of a workspace are every group and automation it contains, at any depth.
    /// </remarks>
    protected override async IAsyncEnumerable<GuidUdi> GetDescendantUdisAsync(
        Workspace entity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allGroups = await groupService.GetAllGroupsAsync(cancellationToken);
        foreach (var group in allGroups.Where(g => g.WorkspaceId == entity.Id))
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, group.Id);
        }

        var (allAutomations, _) = await automationService.GetAutomationsPagedAsync(
            workspaceIds: new HashSet<Guid> { entity.Id },
            groupId: null,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        foreach (var automation in allAutomations)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, automation.Id);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// At the root (the Workspaces tree) a flat list of workspaces. Below a workspace (the Automations tree) its
    /// root-level groups, then its root-level automations. A filter searches every group and automation in the workspace.
    /// </remarks>
    protected override async Task<PagedModel<TreeItem>> GetTreeLevelAsync(
        GuidUdi? parentUdi,
        string? filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (parentUdi is null)
        {
            var workspaces = await workspaceService.GetAllWorkspacesAsync(cancellationToken);
            var workspaceItems = workspaces
                .Where(x => UmbracoAutomateTreeItems.Matches(x.Name, filter))
                .OrderBy(x => x.Name)
                .Select(x => UmbracoAutomateTreeItems.ForWorkspace(x, false, null))
                .ToList();

            return UmbracoAutomateTreeItems.Page(workspaceItems, skip, take);
        }

        if (parentUdi.EntityType != DeployAutomateConstants.UdiEntityType.Workspace)
        {
            return new PagedModel<TreeItem>();
        }

        var workspaceId = parentUdi.Guid;
        var hasFilter = !string.IsNullOrWhiteSpace(filter);

        IEnumerable<WorkspaceGroup> groups = hasFilter
            ? (await groupService.GetAllGroupsAsync(cancellationToken))
                .Where(x => x.WorkspaceId == workspaceId && UmbracoAutomateTreeItems.Matches(x.Name, filter))
            : await groupService.GetGroupsByWorkspaceAsync(workspaceId, null, cancellationToken);

        var head = groups
            .OrderBy(x => x.Name)
            .Select(x => UmbracoAutomateTreeItems.ForGroup(x, parentUdi))
            .ToList();

        // Guid.Empty is the root-level (ungrouped) automations; null searches the whole workspace.
        return await UmbracoAutomateTreeItems.PageWithTailAsync(
            head,
            async (tailSkip, tailTake, ct) =>
            {
                var (automations, total) = await automationService.GetAutomationsPagedAsync(
                    filter: hasFilter ? filter : null,
                    workspaceIds: new HashSet<Guid> { workspaceId },
                    groupId: hasFilter ? null : Guid.Empty,
                    skip: tailSkip,
                    take: tailTake,
                    cancellationToken: ct);

                return (automations.Select(x => UmbracoAutomateTreeItems.ForAutomation(x, parentUdi)).ToList(), total);
            },
            skip,
            take,
            cancellationToken);
    }
}
