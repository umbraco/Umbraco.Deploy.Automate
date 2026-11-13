using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

/// <summary>
/// Shared tree-item building and paging for the connectors' <see cref="ITreeServiceConnector"/> implementations.
/// </summary>
internal static class UmbracoAutomateTreeItems
{
    private const string WorkspaceIcon = "icon-workspace";
    private const string GroupIcon = "icon-folder";
    private const string AutomationIcon = "icon-automation";
    private const string ConnectionIcon = "icon-link";

    public static bool Matches(string? name, string? filter)
        => string.IsNullOrWhiteSpace(filter) || (name?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);

    public static TreeItem ForWorkspace(Workspace workspace, bool hasChildren, Udi? parentUdi)
        => new(new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id), workspace.Name, WorkspaceIcon, hasChildren, parentUdi);

    public static TreeItem ForGroup(WorkspaceGroup group, Udi? parentUdi)
        => new(new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, group.Id), group.Name, GroupIcon, true, parentUdi);

    public static TreeItem ForAutomation(Automation automation, Udi? parentUdi)
        => new(new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, automation.Id), automation.Name, AutomationIcon, false, parentUdi);

    public static TreeItem ForConnection(Connection connection, Udi? parentUdi)
        => new(new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id), connection.Name, ConnectionIcon, false, parentUdi);

    /// <summary>
    /// Pages a list that is already in memory.
    /// </summary>
    public static PagedModel<TreeItem> Page(IReadOnlyList<TreeItem> items, int skip, int take)
        => new(items.Count, items.Skip(skip).Take(take).ToList());

    /// <summary>
    /// Pages a small in-memory list (workspaces or groups, listed first) followed by a tail that the service pages itself
    /// (automations), so a level never loads every automation.
    /// </summary>
    /// <param name="head">The items listed first, already ordered.</param>
    /// <param name="fetchTail">Fetches a page of the tail for the given skip and take (take is always greater than zero).</param>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The maximum number of items to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<PagedModel<TreeItem>> PageWithTailAsync(
        IReadOnlyList<TreeItem> head,
        Func<int, int, CancellationToken, Task<(IReadOnlyList<TreeItem> Items, int Total)>> fetchTail,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var items = head.Skip(skip).Take(take).ToList();
        var remaining = take - items.Count;

        // Always ask for the tail so the total is right, even when the head fills the page.
        (IReadOnlyList<TreeItem> tail, var tailTotal) = await fetchTail(Math.Max(0, skip - head.Count), Math.Max(remaining, 1), cancellationToken).ConfigureAwait(false);
        if (remaining > 0)
        {
            items.AddRange(tail.Take(remaining));
        }

        return new PagedModel<TreeItem>(head.Count + tailTotal, items);
    }
}
