using System.Data;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Deploy.Automate.Workspaces;

namespace Umbraco.Deploy.Automate.Tests.Unit.Workspaces;

public class WorkspaceGroupDeploySaverTests
{
    private readonly Mock<IWorkspaceGroupRepository> _repoMock = new();
    private readonly Mock<ICoreScopeProvider> _scopeProviderMock = new();
    private readonly Mock<ICoreScope> _scopeMock = new();
    private readonly Mock<IScopedNotificationPublisher> _notificationsMock = new();
    private readonly WorkspaceGroupDeploySaver _saver;

    public WorkspaceGroupDeploySaverTests()
    {
        _scopeMock.Setup(s => s.Notifications).Returns(_notificationsMock.Object);
        _scopeProviderMock.Setup(p => p.CreateCoreScope(
                It.IsAny<IsolationLevel>(),
                It.IsAny<RepositoryCacheMode>(),
                It.IsAny<IEventDispatcher?>(),
                It.IsAny<IScopedNotificationPublisher?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(_scopeMock.Object);

        _saver = new WorkspaceGroupDeploySaver(
            _repoMock.Object,
            _scopeProviderMock.Object,
            Mock.Of<IEventMessagesFactory>());
    }

    [Fact]
    public async Task SaveAsync_PersistsViaRepository_AndFiresNotifications()
    {
        // The saver must skip the interactive validators that CreateGroupAsync/UpdateGroupAsync
        // enforce — Deploy lands artifacts across passes, so a nested group may briefly land
        // at root and several same-named groups may coexist there in pass 4.
        var group = new WorkspaceGroup
        {
            Id = Guid.NewGuid(),
            Name = "Campaigns",
            WorkspaceId = Guid.NewGuid(),
        };

        _repoMock.Setup(r => r.SaveAsync(It.IsAny<WorkspaceGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceGroup g, CancellationToken _) => g);

        var result = await _saver.SaveAsync(group);

        result.Id.ShouldBe(group.Id);
        _repoMock.Verify(r => r.SaveAsync(group, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsMock.Verify(n =>
            n.PublishCancelable(It.IsAny<WorkspaceGroupSavingNotification>()), Times.Once);
        _notificationsMock.Verify(n =>
            n.Publish(It.IsAny<WorkspaceGroupSavedNotification>()), Times.Once);
        _scopeMock.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_AssignsNewId_WhenEmpty()
    {
        var group = new WorkspaceGroup { Name = "Campaigns", WorkspaceId = Guid.NewGuid() };

        _repoMock.Setup(r => r.SaveAsync(It.IsAny<WorkspaceGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceGroup g, CancellationToken _) => g);

        var result = await _saver.SaveAsync(group);

        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task SaveAsync_WhenSavingNotificationCancelled_Throws()
    {
        var group = new WorkspaceGroup
        {
            Id = Guid.NewGuid(),
            Name = "Campaigns",
            WorkspaceId = Guid.NewGuid(),
        };

        _notificationsMock.Setup(n => n.PublishCancelable(It.IsAny<WorkspaceGroupSavingNotification>()))
            .Returns(true);

        await Should.ThrowAsync<OperationCanceledException>(() => _saver.SaveAsync(group));

        _repoMock.Verify(r => r.SaveAsync(It.IsAny<WorkspaceGroup>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
