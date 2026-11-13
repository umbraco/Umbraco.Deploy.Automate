using Umbraco.Automate.Core.Automations.Transfer;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.ControlFlow;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateAutomationServiceConnectorTreeTests
{
    private readonly Mock<IAutomationService> _automationServiceMock = new();
    private readonly Mock<IWorkspaceService> _workspaceServiceMock = new();
    private readonly UmbracoAutomateAutomationServiceConnector _connector;

    public UmbracoAutomateAutomationServiceConnectorTreeTests()
    {
        AutomateUdiTypes.Register();
        var settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        _connector = new UmbracoAutomateAutomationServiceConnector(
            _automationServiceMock.Object,
            _workspaceServiceMock.Object,
            new ActionCollection(() => []),
            new TriggerCollection(() => []),
            new ControlFlowCollection(() => []),
            new Mock<ISensitiveSettingsStripper>().Object,
            settingsAccessorMock.Object);
    }

    private static Workspace BuildWorkspace(string name) => new()
    {
        Id = Guid.NewGuid(),
        Alias = name.ToLowerInvariant(),
        Name = name,
        ServiceAccountKey = Guid.NewGuid(),
        UserGroups = [],
        AllowedConnections = [],
    };

    private static Automation BuildAutomation(string name) => new()
    {
        Id = Guid.NewGuid(),
        Alias = name.ToLowerInvariant(),
        Name = name,
        WorkspaceId = Guid.NewGuid(),
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
    public async Task GetTreeItemsAsync_AtRoot_ListsWorkspacesByNameWithChildren()
    {
        SetupWorkspaces("Zeta", "Alpha");

        var result = await _connector.GetTreeItemsAsync();

        result.Total.ShouldBe(2);
        result.Items.Select(x => x.Name).ShouldBe(["Alpha", "Zeta"]);
        result.Items.ShouldAllBe(x => x.HasChildren
            && x.ParentUdi == null
            && x.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Workspace);
    }

    [Fact]
    public async Task GetTreeItemsAsync_AtRootWithoutFilter_ListsNoAutomations()
    {
        SetupWorkspaces("Alpha");
        SetupAutomations([BuildAutomation("Digest")]);

        var result = await _connector.GetTreeItemsAsync();

        result.Total.ShouldBe(1);
        result.Items.ShouldAllBe(x => x.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Workspace);
    }

    [Fact]
    public async Task GetTreeItemsAsync_AtRootWithFilter_ListsMatchingWorkspacesThenAutomations()
    {
        SetupWorkspaces("Reports", "Sales");
        SetupAutomations([BuildAutomation("Report digest")]);

        var result = await _connector.GetTreeItemsAsync(filter: "report");

        result.Total.ShouldBe(2);
        var items = result.Items.ToList();
        items[0].Name.ShouldBe("Reports");
        items[1].Name.ShouldBe("Report digest");
        items[1].HasChildren.ShouldBeFalse();
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            "report", null, null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_AtRootWithFilterAndSkip_PagesIntoAutomations()
    {
        SetupWorkspaces("Reports");
        SetupAutomations([BuildAutomation("Report A"), BuildAutomation("Report B"), BuildAutomation("Report C")]);

        var result = await _connector.GetTreeItemsAsync(filter: "report", skip: 2, take: 2);

        result.Total.ShouldBe(4);
        result.Items.Select(x => x.Name).ShouldBe(["Report B", "Report C"]);
        _automationServiceMock.Verify(x => x.GetAutomationsPagedAsync(
            It.IsAny<string?>(), It.IsAny<IReadOnlySet<Guid>?>(), It.IsAny<Guid?>(), 1, 2, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithParent_ReturnsNothing()
    {
        SetupWorkspaces("Alpha");
        var parent = new GuidUdi(DeployAutomateConstants.UdiEntityType.Automation, Guid.NewGuid());

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
