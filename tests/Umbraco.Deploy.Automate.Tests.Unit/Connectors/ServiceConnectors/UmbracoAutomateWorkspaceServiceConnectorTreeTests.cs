using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateWorkspaceServiceConnectorTreeTests
{
    private readonly Mock<IWorkspaceService> _workspaceServiceMock = new();
    private readonly Mock<IWorkspaceGroupService> _groupServiceMock = new();
    private readonly Mock<IAutomationService> _automationServiceMock = new();
    private readonly UmbracoAutomateWorkspaceServiceConnector _connector;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public UmbracoAutomateWorkspaceServiceConnectorTreeTests()
    {
        AutomateUdiTypes.Register();
        var settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        _connector = new UmbracoAutomateWorkspaceServiceConnector(
            _workspaceServiceMock.Object,
            _groupServiceMock.Object,
            _automationServiceMock.Object,
            new Mock<IConnectionService>().Object,
            new Mock<IUserGroupService>().Object,
            settingsAccessorMock.Object);
    }

    private GuidUdi WorkspaceUdi => new(DeployAutomateConstants.UdiEntityType.Workspace, _workspaceId);

    private static Workspace BuildWorkspace(string name) => new()
    {
        Id = Guid.NewGuid(),
        Alias = name.ToLowerInvariant(),
        Name = name,
        ServiceAccountKey = Guid.NewGuid(),
        UserGroups = [],
        AllowedConnections = [],
    };

    private WorkspaceGroup BuildGroup(string name, Guid? parentId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        WorkspaceId = _workspaceId,
        ParentId = parentId,
    };

    private Automation BuildAutomation(string name, Guid? groupId = null) => new()
    {
        Id = Guid.NewGuid(),
        Alias = name.ToLowerInvariant(),
        Name = name,
        WorkspaceId = _workspaceId,
        GroupId = groupId,
        Steps = [],
    };

    private void SetupWorkspaces(params string[] names)
        => _workspaceServiceMock
            .Setup(x => x.GetAllWorkspacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(names.Select(BuildWorkspace).ToList());

    private void SetupAutomations(List<Automation> automations)
        => _automationServiceMock
            .Setup(x => x.GetAutomationsPagedAsync(
                It.IsAny<string?>(),
                It.IsAny<IReadOnlySet<Guid>?>(),
                It.IsAny<Guid?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? _, IReadOnlySet<Guid>? _, Guid? _, int skip, int take, CancellationToken _)
                => ((IReadOnlyList<Automation>)automations.Skip(skip).Take(take).ToList(), automations.Count));

    [Fact]
    public async Task GetTreeItemsAsync_AtRoot_ListsWorkspacesByNameWithoutChildren()
    {
        SetupWorkspaces("Zeta", "Alpha");

        var result = await _connector.GetTreeItemsAsync();

        result.Total.ShouldBe(2);
        result.Items.Select(x => x.Name).ShouldBe(["Alpha", "Zeta"]);
        result.Items.ShouldAllBe(x => x.HasChildren == false
            && x.ParentUdi == null
            && x.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Workspace);
    }

    [Fact]
    public async Task GetTreeItemsAsync_AtRootWithFilterAndPaging_PagesMatchingWorkspaces()
    {
        SetupWorkspaces("Sales A", "Sales B", "Sales C", "Marketing");

        var result = await _connector.GetTreeItemsAsync(filter: "sales", skip: 1, take: 1);

        result.Total.ShouldBe(3);
        result.Items.Select(x => x.Name).ShouldBe(["Sales B"]);
    }

    [Fact]
    public async Task GetTreeItemsAsync_BelowWorkspace_ListsRootGroupsThenRootAutomations()
    {
        var group = BuildGroup("Folder");
        _groupServiceMock
            .Setup(x => x.GetGroupsByWorkspaceAsync(_workspaceId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([group]);
        SetupAutomations([BuildAutomation("Digest")]);

        var result = await _connector.GetTreeItemsAsync(WorkspaceUdi);

        result.Total.ShouldBe(2);
        var items = result.Items.ToList();
        items[0].Udi.ShouldBe(new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, group.Id));
        items[0].HasChildren.ShouldBeTrue();
        items[0].ParentUdi.ShouldBe(WorkspaceUdi);
        items[1].Udi.EntityType.ShouldBe(DeployAutomateConstants.UdiEntityType.Automation);
        items[1].HasChildren.ShouldBeFalse();
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            null,
            It.Is<IReadOnlySet<Guid>?>(s => s != null && s.Contains(_workspaceId)),
            Guid.Empty,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_BelowWorkspace_PagesAcrossGroupsAndAutomations()
    {
        _groupServiceMock
            .Setup(x => x.GetGroupsByWorkspaceAsync(_workspaceId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildGroup("G1"), BuildGroup("G2")]);
        SetupAutomations([BuildAutomation("A1"), BuildAutomation("A2"), BuildAutomation("A3")]);

        var result = await _connector.GetTreeItemsAsync(WorkspaceUdi, skip: 1, take: 3);

        result.Total.ShouldBe(5);
        result.Items.Select(x => x.Name).ShouldBe(["G2", "A1", "A2"]);
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            It.IsAny<string?>(), It.IsAny<IReadOnlySet<Guid>?>(), It.IsAny<Guid?>(), 0, 2, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_BelowWorkspaceWithFilter_SearchesAllGroupsAndAutomationsInWorkspace()
    {
        var nested = BuildGroup("Report archive", parentId: Guid.NewGuid());
        var otherWorkspaceGroup = new WorkspaceGroup { Id = Guid.NewGuid(), Name = "Report other", WorkspaceId = Guid.NewGuid() };
        _groupServiceMock
            .Setup(x => x.GetAllGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([nested, BuildGroup("Unrelated"), otherWorkspaceGroup]);
        SetupAutomations([BuildAutomation("Report digest")]);

        var result = await _connector.GetTreeItemsAsync(WorkspaceUdi, filter: "report");

        result.Items.Select(x => x.Name).ShouldBe(["Report archive", "Report digest"]);
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            "report", It.IsAny<IReadOnlySet<Guid>?>(), null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithParentOfAnotherType_ReturnsNothing()
    {
        SetupWorkspaces("Alpha");
        var parent = new GuidUdi(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, Guid.NewGuid());

        var result = await _connector.GetTreeItemsAsync(parent);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithInvalidPaging_Throws()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _connector.GetTreeItemsAsync(skip: -1));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _connector.GetTreeItemsAsync(take: 0));
    }
}
