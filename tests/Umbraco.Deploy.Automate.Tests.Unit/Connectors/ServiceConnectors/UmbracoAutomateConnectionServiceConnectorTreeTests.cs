using Umbraco.Automate.Core.Automations.Transfer;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.ControlFlow;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Notifications.Channels;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Automate.Core.Triggers.Webhooks;
using Umbraco.Cms.Core;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateConnectionServiceConnectorTreeTests
{
    private readonly Mock<IConnectionService> _connectionServiceMock = new();
    private readonly UmbracoAutomateConnectionServiceConnector _connector;

    public UmbracoAutomateConnectionServiceConnectorTreeTests()
    {
        // Deploy registers UDI types from the connectors' [UdiDefinition] at composition; no composition runs here.
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Connection, UdiType.GuidUdi);

        var settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        var connectionTypeCollection = new ConnectionTypeCollection(() => []);
        var stripper = new SensitiveSettingsStripper(
            new ActionCollection(() => []),
            new TriggerCollection(() => []),
            new ControlFlowCollection(() => []),
            connectionTypeCollection,
            new WebhookAuthenticatorCollection(() => []),
            new NotificationChannelCollection(() => []));

        _connector = new UmbracoAutomateConnectionServiceConnector(
            _connectionServiceMock.Object,
            connectionTypeCollection,
            stripper,
            settingsAccessorMock.Object);
    }

    private static Connection Build(string name) => new() { Id = Guid.NewGuid(), Alias = name.ToLowerInvariant(), Name = name, Type = "httpBasicAuth", Settings = [] };

    private void SetupConnections(params string[] names)
        => _connectionServiceMock
            .Setup(x => x.GetAllConnectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(names.Select(Build).ToList());

    [Fact]
    public async Task GetTreeItemsAsync_AtRoot_ListsConnectionsByNameWithoutChildren()
    {
        SetupConnections("Zeta", "Alpha");

        var result = await _connector.GetTreeItemsAsync();

        result.Total.ShouldBe(2);
        result.Items.Select(x => x.Name).ShouldBe(["Alpha", "Zeta"]);
        result.Items.ShouldAllBe(x => x.HasChildren == false
            && x.ParentUdi == null
            && x.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Connection);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithRootUdi_ListsConnections()
    {
        SetupConnections("Alpha");

        var result = await _connector.GetTreeItemsAsync(Udi.Create(DeployAutomateConstants.UdiEntityType.Connection));

        result.Items.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithFilter_ReturnsMatchingNamesOnly()
    {
        SetupConnections("Slack prod", "Slack dev", "Github");

        var result = await _connector.GetTreeItemsAsync(filter: "SLACK");

        result.Total.ShouldBe(2);
        result.Items.Select(x => x.Name).ShouldBe(["Slack dev", "Slack prod"]);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithSkipAndTake_ReturnsPageAndFullTotal()
    {
        SetupConnections("A", "B", "C", "D");

        var result = await _connector.GetTreeItemsAsync(skip: 1, take: 2);

        result.Total.ShouldBe(4);
        result.Items.Select(x => x.Name).ShouldBe(["B", "C"]);
    }

    [Fact]
    public async Task GetTreeItemsAsync_WithParent_ReturnsNothing()
    {
        SetupConnections("A");
        var parent = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, Guid.NewGuid());

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
