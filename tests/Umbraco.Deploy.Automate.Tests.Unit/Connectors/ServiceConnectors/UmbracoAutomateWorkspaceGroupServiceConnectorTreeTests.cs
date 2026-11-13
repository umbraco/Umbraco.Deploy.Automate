using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;
using Umbraco.Deploy.Automate.Workspaces;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateWorkspaceGroupServiceConnectorTreeTests
{
    private readonly Mock<IWorkspaceGroupService> _groupServiceMock = new();
    private readonly Mock<IAutomationService> _automationServiceMock = new();
    private readonly UmbracoAutomateWorkspaceGroupServiceConnector _connector;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly WorkspaceGroup _parentGroup;

    public UmbracoAutomateWorkspaceGroupServiceConnectorTreeTests()
    {
        var settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        _connector = new UmbracoAutomateWorkspaceGroupServiceConnector(
            _groupServiceMock.Object,
            _automationServiceMock.Object,
            new Mock<IWorkspaceGroupDeploySaver>().Object,
            settingsAccessorMock.Object);

        _parentGroup = BuildGroup("Parent");
        _groupServiceMock
            .Setup(x => x.GetGroupAsync(_parentGroup.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_parentGroup);
    }

    private GuidUdi ParentUdi => new(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, _parentGroup.Id);

    private WorkspaceGroup BuildGroup(string name, Guid? parentId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorkspaceId = _workspaceId,
        ParentId = parentId,
    };

    private Automation BuildAutomation(string name, Guid? groupId) => new()
    {
        Id = Guid.NewGuid(),
        Alias = name.ToLowerInvariant(),
        Name = name,
        WorkspaceId = _workspaceId,
        GroupId = groupId,
        Steps = [],
    };

    private void SetupAutomations(List<Automation> automations)
        => _automationServiceMock
            .Setup(x => x.GetAutomationsPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlySet<Guid>?>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? _, IReadOnlySet<Guid>? _, Guid? groupId, int skip, int take, CancellationToken _)
                => ((IReadOnlyList<Automation>)automations
                    .Where(a => groupId is null || a.GroupId == groupId)
                    .Skip(skip).Take(take).ToList(),
                    automations.Count(a => groupId is null || a.GroupId == groupId)));

    [Fact]
    public async Task GetTreeItemsAsync_AtRoot_ReturnsNothing()
    {
        var result = await _connector.GetTreeItemsAsync();

        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
    }

    [Fact]
    public async Task GetTreeItemsAsync_BelowGroup_ListsChildGroupsThenAutomationsOfThatGroup()
    {
        var child = BuildGroup("Child", _parentGroup.Id);
        _groupServiceMock
            .Setup(x => x.GetGroupsByWorkspaceAsync(_workspaceId, _parentGroup.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([child]);
        SetupAutomations([BuildAutomation("In parent", _parentGroup.Id), BuildAutomation("Elsewhere", null)]);

        var result = await _connector.GetTreeItemsAsync(ParentUdi);

        result.Total.ShouldBe(2);
        var items = result.Items.ToList();
        items[0].Udi.ShouldBe(new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, child.Id));
        items[0].HasChildren.ShouldBeTrue();
        items[0].ParentUdi.ShouldBe(ParentUdi);
        items[1].Name.ShouldBe("In parent");
        items[1].HasChildren.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTreeItemsAsync_BelowGroup_PagesAutomationsAfterGroups()
    {
        _groupServiceMock
            .Setup(x => x.GetGroupsByWorkspaceAsync(_workspaceId, _parentGroup.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildGroup("G1", _parentGroup.Id)]);
        SetupAutomations([BuildAutomation("A1", _parentGroup.Id), BuildAutomation("A2", _parentGroup.Id), BuildAutomation("A3", _parentGroup.Id)]);

        var result = await _connector.GetTreeItemsAsync(ParentUdi, skip: 2, take: 5);

        result.Total.ShouldBe(4);
        result.Items.Select(x => x.Name).ShouldBe(["A2", "A3"]);
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            It.IsAny<string?>(), It.IsAny<IReadOnlySet<Guid>?>(), _parentGroup.Id, 1, 5, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithFilter_SearchesNestedGroupsAndAutomationsOnly()
    {
        var child = BuildGroup("Report child", _parentGroup.Id);
        var grandChild = BuildGroup("Report grandchild", child.Id);
        var outside = BuildGroup("Report outside");
        _groupServiceMock
            .Setup(x => x.GetAllGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_parentGroup, child, grandChild, outside]);
        SetupAutomations(
        [
            BuildAutomation("Report deep", grandChild.Id),
            BuildAutomation("Report outside", outside.Id),
            BuildAutomation("Report ungrouped", null),
        ]);

        var result = await _connector.GetTreeItemsAsync(ParentUdi, filter: "report");

        result.Total.ShouldBe(3);
        result.Items.Select(x => x.Name).ShouldBe(["Report child", "Report grandchild", "Report deep"]);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithUnknownGroup_ReturnsNothing()
    {
        _groupServiceMock
            .Setup(x => x.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceGroup?)null);

        var result = await _connector.GetTreeItemsAsync(new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, Guid.NewGuid()));

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithParentOfAnotherType_ReturnsNothing()
    {
        var result = await _connector.GetTreeItemsAsync(new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, _workspaceId));

        result.Items.ShouldBeEmpty();
        _groupServiceMock.Verify(x => x.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithInvalidPaging_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _connector.GetTreeItemsAsync(ParentUdi, skip: -1));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _connector.GetTreeItemsAsync(ParentUdi, take: 0));
    }
}
