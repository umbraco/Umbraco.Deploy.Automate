using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.Deploy.Automate.Workspaces;

/// <inheritdoc />
internal sealed class WorkspaceGroupDeploySaver(
    IWorkspaceGroupRepository groupRepository,
    ICoreScopeProvider scopeProvider,
    IEventMessagesFactory eventMessagesFactory) : IWorkspaceGroupDeploySaver
{
    /// <inheritdoc />
    public async Task<WorkspaceGroup> SaveAsync(WorkspaceGroup group, CancellationToken cancellationToken = default)
    {
        if (group.Id == Guid.Empty)
        {
            group.Id = Guid.NewGuid();
        }

        using ICoreScope scope = scopeProvider.CreateCoreScope();

        var eventMessages = eventMessagesFactory.Get();

        var savingNotification = new WorkspaceGroupSavingNotification(group, eventMessages);
        if (scope.Notifications.PublishCancelable(savingNotification))
        {
            throw new OperationCanceledException("Workspace group save was cancelled by a notification handler.");
        }

        var saved = await groupRepository.SaveAsync(group, cancellationToken);

        scope.Notifications.Publish(new WorkspaceGroupSavedNotification(saved, eventMessages));
        scope.Complete();

        return saved;
    }
}
