using System.Runtime.CompilerServices;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Automate.Artifacts;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Workspaces;
using Umbraco.Cms.Core.Models;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for workspace groups (folders). Resolves Workspace and parent-group dependencies
/// so nested groups deploy in the correct order.
/// </summary>
[UdiDefinition(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, UdiType.GuidUdi)]
public class UmbracoAutomateWorkspaceGroupServiceConnector(
    IWorkspaceGroupService groupService,
    IAutomationService automationService,
    IWorkspaceGroupDeploySaver deploySaver,
    DeployAutomateSettingsAccessor settingsAccessor)
    : UmbracoAutomateEntityServiceConnectorBase<AutomateWorkspaceGroupArtifact, WorkspaceGroup>(settingsAccessor)
{
    /// <inheritdoc />
    /// <remarks>
    /// Two passes to handle nested folders: Deploy processes artifacts within a pass in
    /// package order, not dependency order, so a child group can appear before its parent.
    /// Pass 4 creates/updates every group at the root; pass 5 re-parents once every group
    /// in the package exists.
    /// </remarks>
    protected override int[] ProcessPasses => [4, 5];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors =>
    [
        Constants.DeploySelector.This,
        Constants.DeploySelector.ThisAndDescendants,
        Constants.DeploySelector.DescendantsOfThis,
    ];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco Automate Workspace Groups";

    /// <inheritdoc />
    public override string UdiEntityType => DeployAutomateConstants.UdiEntityType.WorkspaceGroup;

    /// <inheritdoc />
    public override async Task<WorkspaceGroup?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => await groupService.GetGroupAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<WorkspaceGroup> GetEntitiesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var groups = await groupService.GetAllGroupsAsync(cancellationToken);
        foreach (var group in groups)
        {
            yield return group;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(WorkspaceGroup entity) => entity.Name;

    /// <inheritdoc />
    public override Task<AutomateWorkspaceGroupArtifact?> GetArtifactAsync(
        GuidUdi udi,
        WorkspaceGroup? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AutomateWorkspaceGroupArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        // The owning workspace must exist in the target environment before this group.
        var workspaceUdi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, entity.WorkspaceId);
        dependencies.Add(new UmbracoAutomateArtifactDependency(workspaceUdi, ArtifactDependencyMode.Match));

        // Nested groups depend on their parent group being deployed first.
        GuidUdi? parentUdi = null;
        if (entity.ParentId.HasValue)
        {
            parentUdi = new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, entity.ParentId.Value);
            dependencies.Add(new UmbracoAutomateArtifactDependency(parentUdi, ArtifactDependencyMode.Match));
        }

        var artifact = new AutomateWorkspaceGroupArtifact(udi, dependencies)
        {
            Name = entity.Name,
            WorkspaceUdi = workspaceUdi,
            ParentUdi = parentUdi,
        };

        return Task.FromResult<AutomateWorkspaceGroupArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AutomateWorkspaceGroupArtifact, WorkspaceGroup> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 4:
                await Pass4Async(state, cancellationToken);
                break;
            case 5:
                await Pass5Async(state, cancellationToken);
                break;
        }
    }

    private async Task Pass4Async(
        ArtifactDeployState<AutomateWorkspaceGroupArtifact, WorkspaceGroup> state,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        artifact.WorkspaceUdi.EnsureType(DeployAutomateConstants.UdiEntityType.Workspace);

        // Save every group at the root first. The final parent is applied in pass 5
        // once every group in the package exists, so intra-pass ordering doesn't matter.
        var group = state.Entity ?? new WorkspaceGroup
        {
            Id = artifact.Udi.Guid,
            Name = artifact.Name,
        };
        group.Name = artifact.Name;
        group.WorkspaceId = artifact.WorkspaceUdi.Guid;
        group.ParentId = null;

        state.Entity = await deploySaver.SaveAsync(group, cancellationToken);
    }

    private async Task Pass5Async(
        ArtifactDeployState<AutomateWorkspaceGroupArtifact, WorkspaceGroup> state,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        if (artifact.ParentUdi is null)
        {
            return;
        }

        artifact.ParentUdi.EnsureType(DeployAutomateConstants.UdiEntityType.WorkspaceGroup);

        var group = state.Entity!;
        group.ParentId = artifact.ParentUdi.Guid;

        state.Entity = await deploySaver.SaveAsync(group, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Direct children of a group are its sub-groups and the automations assigned
    /// to this group.
    /// </remarks>
    protected override async IAsyncEnumerable<GuidUdi> GetChildUdisAsync(
        WorkspaceGroup entity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var childGroups = await groupService.GetGroupsByWorkspaceAsync(entity.WorkspaceId, entity.Id, cancellationToken);
        foreach (var group in childGroups)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, group.Id);
        }

        var (automations, _) = await automationService.GetAutomationsPagedAsync(
            workspaceIds: new HashSet<Guid> { entity.WorkspaceId },
            groupId: entity.Id,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        foreach (var automation in automations)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, automation.Id);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Descendants of a group are every nested group and every automation that lives
    /// anywhere within the group's subtree.
    /// </remarks>
    protected override async IAsyncEnumerable<GuidUdi> GetDescendantUdisAsync(
        WorkspaceGroup entity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allGroupsInWorkspace = (await groupService.GetAllGroupsAsync(cancellationToken))
            .Where(g => g.WorkspaceId == entity.WorkspaceId)
            .ToList();

        var descendantGroupIds = CollectDescendantGroupIds(entity.Id, allGroupsInWorkspace);

        foreach (var groupId in descendantGroupIds)
        {
            yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, groupId);
        }

        // Automations directly under this group OR any descendant group.
        var scopeGroupIds = new HashSet<Guid>(descendantGroupIds) { entity.Id };
        var (allAutomations, _) = await automationService.GetAutomationsPagedAsync(
            workspaceIds: new HashSet<Guid> { entity.WorkspaceId },
            groupId: null,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        foreach (var automation in allAutomations)
        {
            if (automation.GroupId.HasValue && scopeGroupIds.Contains(automation.GroupId.Value))
            {
                yield return new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, automation.Id);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Below a group, its child groups then its automations. A group is never listed at the root: the picker starts at
    /// the workspaces of the Automations tree. A filter searches every nested group and automation of the group; the
    /// automation query cannot be limited to a set of groups, so the filtered level is paged in memory.
    /// </remarks>
    protected override async Task<PagedModel<TreeItem>> GetTreeLevelAsync(
        GuidUdi? parentUdi,
        string? filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (parentUdi is null || parentUdi.EntityType != DeployAutomateConstants.UdiEntityType.WorkspaceGroup)
        {
            return new PagedModel<TreeItem>();
        }

        var group = await groupService.GetGroupAsync(parentUdi.Guid, cancellationToken);
        if (group is null)
        {
            return new PagedModel<TreeItem>();
        }

        var workspaceIds = new HashSet<Guid> { group.WorkspaceId };

        if (string.IsNullOrWhiteSpace(filter))
        {
            var childGroups = await groupService.GetGroupsByWorkspaceAsync(group.WorkspaceId, group.Id, cancellationToken);
            var head = childGroups
                .OrderBy(x => x.Name)
                .Select(x => UmbracoAutomateTreeItems.ForGroup(x, parentUdi))
                .ToList();

            return await UmbracoAutomateTreeItems.PageWithTailAsync(
                head,
                async (tailSkip, tailTake, ct) =>
                {
                    var (automations, total) = await automationService.GetAutomationsPagedAsync(
                        filter: null,
                        workspaceIds: workspaceIds,
                        groupId: group.Id,
                        skip: tailSkip,
                        take: tailTake,
                        cancellationToken: ct);

                    return (automations.Select(x => UmbracoAutomateTreeItems.ForAutomation(x, parentUdi)).ToList(), total);
                },
                skip,
                take,
                cancellationToken);
        }

        var allGroups = (await groupService.GetAllGroupsAsync(cancellationToken))
            .Where(x => x.WorkspaceId == group.WorkspaceId)
            .ToList();
        var descendantGroupIds = CollectDescendantGroupIds(group.Id, allGroups);
        var scopeGroupIds = new HashSet<Guid>(descendantGroupIds) { group.Id };

        var matchingGroups = allGroups
            .Where(x => descendantGroupIds.Contains(x.Id) && UmbracoAutomateTreeItems.Matches(x.Name, filter))
            .OrderBy(x => x.Name)
            .Select(x => UmbracoAutomateTreeItems.ForGroup(x, parentUdi));

        var (matchingAutomations, _) = await automationService.GetAutomationsPagedAsync(
            filter: filter,
            workspaceIds: workspaceIds,
            groupId: null,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        var items = matchingGroups
            .Concat(matchingAutomations
                .Where(x => x.GroupId.HasValue && scopeGroupIds.Contains(x.GroupId.Value))
                .OrderBy(x => x.Name)
                .Select(x => UmbracoAutomateTreeItems.ForAutomation(x, parentUdi)))
            .ToList();

        return UmbracoAutomateTreeItems.Page(items, skip, take);
    }

    private static List<Guid> CollectDescendantGroupIds(Guid rootId, IReadOnlyCollection<WorkspaceGroup> allGroups)
    {
        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var group in allGroups.Where(g => g.ParentId == parentId))
            {
                result.Add(group.Id);
                queue.Enqueue(group.Id);
            }
        }

        return result;
    }
}
