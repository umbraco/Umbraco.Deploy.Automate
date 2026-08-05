using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Automate.Artifacts;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateWorkspaceServiceConnectorTests
{
    private readonly Mock<IWorkspaceService> _workspaceServiceMock = new();
    private readonly Mock<IWorkspaceGroupService> _groupServiceMock = new();
    private readonly Mock<IAutomationService> _automationServiceMock = new();
    private readonly Mock<IConnectionService> _connectionServiceMock = new();
    private readonly Mock<DeployAutomateSettingsAccessor> _settingsAccessorMock;
    private readonly UmbracoAutomateWorkspaceServiceConnector _connector;

    public UmbracoAutomateWorkspaceServiceConnectorTests()
    {
        _settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        _connector = new UmbracoAutomateWorkspaceServiceConnector(
            _workspaceServiceMock.Object,
            _groupServiceMock.Object,
            _automationServiceMock.Object,
            _connectionServiceMock.Object,
            _settingsAccessorMock.Object);
    }

    private static Workspace BuildWorkspace(
        Guid? serviceAccountKey = null,
        IList<Guid>? userGroups = null,
        IList<Guid>? allowedConnections = null) => new()
    {
        Alias = "marketing",
        Name = "Marketing",
        ServiceAccountKey = serviceAccountKey ?? Guid.NewGuid(),
        UserGroups = userGroups ?? [],
        AllowedConnections = allowedConnections ?? [],
    };

    [Fact]
    public async Task GetArtifactAsync_CopiesCoreFields()
    {
        var serviceAccountKey = Guid.NewGuid();
        var workspace = BuildWorkspace(serviceAccountKey: serviceAccountKey);
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);

        var artifact = await _connector.GetArtifactAsync(udi, workspace);

        artifact.ShouldNotBeNull();
        artifact.Alias.ShouldBe("marketing");
        artifact.Name.ShouldBe("Marketing");
        artifact.ServiceAccountKey.ShouldBe(serviceAccountKey);
    }

    [Fact]
    public async Task GetArtifactAsync_DoesNotAddUserDependencyForServiceAccount()
    {
        // Users are not a deployable entity type in Umbraco Deploy — declaring a
        // user dependency throws "No connector registered for entity type 'user'".
        var serviceAccountKey = Guid.NewGuid();
        var workspace = BuildWorkspace(serviceAccountKey: serviceAccountKey);
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);

        var artifact = await _connector.GetArtifactAsync(udi, workspace);

        artifact.ShouldNotBeNull();
        artifact.Dependencies.ShouldNotContain(d =>
            d.Udi.EntityType == Umbraco.Cms.Core.Constants.UdiEntityType.User);
    }

    [Fact]
    public async Task GetArtifactAsync_AddsUserGroupDependencyPerGroup()
    {
        var group1 = Guid.NewGuid();
        var group2 = Guid.NewGuid();
        var workspace = BuildWorkspace(userGroups: [group1, group2]);
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);

        var artifact = await _connector.GetArtifactAsync(udi, workspace);

        artifact.ShouldNotBeNull();
        artifact.UserGroups.ShouldBe(new[] { group1, group2 });
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == Umbraco.Cms.Core.Constants.UdiEntityType.UserGroup &&
            ((GuidUdi)d.Udi).Guid == group1);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == Umbraco.Cms.Core.Constants.UdiEntityType.UserGroup &&
            ((GuidUdi)d.Udi).Guid == group2);
    }

    [Fact]
    public async Task GetArtifactAsync_AddsConnectionDependencyPerAllowedConnection()
    {
        var connection1 = Guid.NewGuid();
        var connection2 = Guid.NewGuid();
        var workspace = BuildWorkspace(allowedConnections: [connection1, connection2]);
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);

        var artifact = await _connector.GetArtifactAsync(udi, workspace);

        artifact.ShouldNotBeNull();
        artifact.AllowedConnectionUdis.Count.ShouldBe(2);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Connection &&
            ((GuidUdi)d.Udi).Guid == connection1);
        artifact.Dependencies.ShouldContain(d =>
            d.Udi.EntityType == DeployAutomateConstants.UdiEntityType.Connection &&
            ((GuidUdi)d.Udi).Guid == connection2);
    }

    [Fact]
    public async Task GetArtifactAsync_WithNoConnectionsOrGroups_HasNoDependencies()
    {
        var workspace = BuildWorkspace();
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);

        var artifact = await _connector.GetArtifactAsync(udi, workspace);

        artifact.ShouldNotBeNull();
        artifact.Dependencies.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullEntity_ReturnsNull()
    {
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, Guid.NewGuid());

        var artifact = await _connector.GetArtifactAsync(udi, null);

        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_DelegatesToWorkspaceService()
    {
        var id = Guid.NewGuid();
        var workspace = BuildWorkspace();
        _workspaceServiceMock
            .Setup(x => x.GetWorkspaceAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await _connector.GetEntityAsync(id);

        result.ShouldBe(workspace);
    }

    [Fact]
    public void GetEntityName_ReturnsWorkspaceName()
    {
        var workspace = BuildWorkspace();

        _connector.GetEntityName(workspace).ShouldBe("Marketing");
    }

    [Fact]
    public void UdiEntityType_ReturnsWorkspaceUdiType()
    {
        _connector.UdiEntityType.ShouldBe(DeployAutomateConstants.UdiEntityType.Workspace);
    }

    [Fact]
    public async Task ProcessAsync_WithExistingWorkspace_DoesNotOverwriteServiceAccountKey()
    {
        // The source environment's service account key almost never resolves to a real user
        // on the target (Umbraco Deploy does not transfer IUser entities), so redeploying an
        // existing workspace must leave whatever service account is already configured here
        // untouched rather than clobbering it with the source's key.
        var existingServiceAccountKey = Guid.NewGuid();
        var workspace = BuildWorkspace(serviceAccountKey: existingServiceAccountKey);
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, workspace.Id);
        var artifact = new AutomateWorkspaceArtifact(udi, new ArtifactDependencyCollection())
        {
            Alias = "marketing",
            Name = "Marketing",
            ServiceAccountKey = Guid.NewGuid(), // the source environment's key — must be ignored
        };
        var state = ArtifactDeployState.Create<AutomateWorkspaceArtifact, Workspace>(artifact, workspace, _connector, 3);
        _workspaceServiceMock
            .Setup(x => x.UpdateWorkspaceAsync(It.IsAny<Workspace>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace w, Guid? _, CancellationToken _) => w);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        _workspaceServiceMock.Verify(
            x => x.UpdateWorkspaceAsync(
                It.Is<Workspace>(w => w.ServiceAccountKey == existingServiceAccountKey),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithNoExistingWorkspace_CreatesWithEmptyServiceAccountKey()
    {
        // On first deploy there is nothing configured locally yet — leave the field unset
        // rather than seeding it with the source environment's (almost certainly wrong) key.
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Workspace, Guid.NewGuid());
        var artifact = new AutomateWorkspaceArtifact(udi, new ArtifactDependencyCollection())
        {
            Alias = "marketing",
            Name = "Marketing",
            ServiceAccountKey = Guid.NewGuid(),
        };
        var state = ArtifactDeployState.Create<AutomateWorkspaceArtifact, Workspace>(artifact, null, _connector, 3);
        _workspaceServiceMock
            .Setup(x => x.CreateWorkspaceAsync(It.IsAny<Workspace>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace w, Guid? _, CancellationToken _) => w);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 3);

        _workspaceServiceMock.Verify(
            x => x.CreateWorkspaceAsync(
                It.Is<Workspace>(w => w.ServiceAccountKey == Guid.Empty),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
