using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Automations;
using Umbraco.Automate.Core.Notifications;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.NotificationHandlers;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.Tests.Unit.NotificationHandlers;

public class DeployRefresherNotificationHandlerTests
{
    private readonly Mock<IServiceConnectorFactory> _factory = new();
    private readonly Mock<IDiskEntityService> _disk = new();
    private readonly Mock<ISignatureService> _signatures = new();
    private readonly DeployEntityTypesOptions _entityTypes = new();
    private readonly DeploySettings _settings = new();

    public DeployRefresherNotificationHandlerTests()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Workspace, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
        DeployAutomateComposer.ConfigureEntityTypes(_entityTypes);

        var connector = new Mock<IServiceConnector>();
        connector
            .Setup(x => x.GetArtifactAsync(It.IsAny<object?>(), It.IsAny<IContextCache>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IArtifact>());
        _factory.Setup(x => x.GetConnector(It.IsAny<string>())).Returns(connector.Object);
    }

    [Fact]
    public async Task Saving_ADiskEntityType_WritesTheArtifactsToDisk()
    {
        await CreateSavedHandler().HandleAsync(new WorkspaceSavedNotification(new Workspace { Id = Guid.NewGuid(), Alias = "w", Name = "W" }, new EventMessages()), default);

        _disk.Verify(x => x.WriteArtifactsAsync(It.IsAny<IEnumerable<IArtifact>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Saving_ATransferEntityType_DoesNotWriteToDisk()
    {
        await CreateAutomationSavedHandler().HandleAsync(new AutomationSavedNotification(new Automation { Id = Guid.NewGuid(), Alias = "a", Name = "A" }, new EventMessages()), default);

        _disk.Verify(x => x.WriteArtifactsAsync(It.IsAny<IEnumerable<IArtifact>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Saving_AnExcludedDiskEntityType_DoesNotWriteToDisk()
    {
        _settings.ExcludedEntityTypes.Add(DeployAutomateConstants.UdiEntityType.Workspace);

        await CreateSavedHandler().HandleAsync(new WorkspaceSavedNotification(new Workspace { Id = Guid.NewGuid(), Alias = "w", Name = "W" }, new EventMessages()), default);

        _disk.Verify(x => x.WriteArtifactsAsync(It.IsAny<IEnumerable<IArtifact>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Saving_ATransferEntityType_StillSetsSignatures()
    {
        await CreateAutomationSavedHandler().HandleAsync(new AutomationSavedNotification(new Automation { Id = Guid.NewGuid(), Alias = "a", Name = "A" }, new EventMessages()), default);

        _signatures.Verify(x => x.SetSignatures(It.IsAny<IEnumerable<IArtifact>>()), Times.Once);
    }

    [Fact]
    public async Task Deleting_ADiskEntityType_DeletesTheArtifactsFromDisk()
    {
        await CreateDeletedHandler().HandleAsync(new WorkspaceDeletedNotification(new Workspace { Id = Guid.NewGuid(), Alias = "w", Name = "W" }, new EventMessages()), default);

        _disk.Verify(x => x.DeleteArtifacts(It.IsAny<IEnumerable<IArtifact>>()), Times.Once);
    }

    [Fact]
    public async Task Deleting_ATransferEntityType_DoesNotDeleteFromDisk()
    {
        await CreateAutomationDeletedHandler().HandleAsync(new AutomationDeletedNotification(new Automation { Id = Guid.NewGuid(), Alias = "a", Name = "A" }, new EventMessages()), default);

        _disk.Verify(x => x.DeleteArtifacts(It.IsAny<IEnumerable<IArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task Deleting_AnExcludedDiskEntityType_DoesNotDeleteFromDisk()
    {
        _settings.ExcludedEntityTypes.Add(DeployAutomateConstants.UdiEntityType.Workspace);

        await CreateDeletedHandler().HandleAsync(new WorkspaceDeletedNotification(new Workspace { Id = Guid.NewGuid(), Alias = "w", Name = "W" }, new EventMessages()), default);

        _disk.Verify(x => x.DeleteArtifacts(It.IsAny<IEnumerable<IArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task Deleting_ATransferEntityType_StillClearsSignatures()
    {
        await CreateAutomationDeletedHandler().HandleAsync(new AutomationDeletedNotification(new Automation { Id = Guid.NewGuid(), Alias = "a", Name = "A" }, new EventMessages()), default);

        _signatures.Verify(x => x.ClearSignatures(It.IsAny<IEnumerable<Udi>>()), Times.Once);
    }

    private WorkspaceSavedDeployRefresherNotificationAsyncHandler CreateSavedHandler()
        => new(_factory.Object, _disk.Object, _signatures.Object, Monitor(_entityTypes), Monitor(_settings));

    private AutomationSavedDeployRefresherNotificationAsyncHandler CreateAutomationSavedHandler()
        => new(_factory.Object, _disk.Object, _signatures.Object, Monitor(_entityTypes), Monitor(_settings));

    private WorkspaceDeletedDeployRefresherNotificationAsyncHandler CreateDeletedHandler()
        => new(_disk.Object, _signatures.Object, Monitor(_entityTypes), Monitor(_settings));

    private AutomationDeletedDeployRefresherNotificationAsyncHandler CreateAutomationDeletedHandler()
        => new(_disk.Object, _signatures.Object, Monitor(_entityTypes), Monitor(_settings));

    private static IOptionsMonitor<T> Monitor<T>(T value)
        where T : class
    {
        var monitor = new Mock<IOptionsMonitor<T>>();
        monitor.Setup(x => x.CurrentValue).Returns(value);
        return monitor.Object;
    }
}
