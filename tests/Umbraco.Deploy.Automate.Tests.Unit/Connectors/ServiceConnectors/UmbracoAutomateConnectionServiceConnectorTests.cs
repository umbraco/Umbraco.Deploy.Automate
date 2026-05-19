using System.Text.Json;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Automations.Transfer;
using Umbraco.Automate.Core.Connections;
using Umbraco.Automate.Core.ControlFlow;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Automate.Artifacts;
using Umbraco.Deploy.Automate.Configuration;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAutomateConnectionServiceConnectorTests
{
    private readonly Mock<IConnectionService> _connectionServiceMock = new();
    private readonly Mock<DeployAutomateSettingsAccessor> _settingsAccessorMock;
    private readonly List<IConnectionType> _registeredConnectionTypes = [];
    private readonly UmbracoAutomateConnectionServiceConnector _connector;

    public UmbracoAutomateConnectionServiceConnectorTests()
    {
        _settingsAccessorMock = new Mock<DeployAutomateSettingsAccessor>(MockBehavior.Strict, null!);
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings());

        // Default: httpBasicAuth is registered on the target with a settings schema that
        // marks ApiKey as sensitive. Individual tests can clear or replace this to simulate
        // a missing provider package or a different schema.
        _registeredConnectionTypes.Add(BuildConnectionTypeMock("httpBasicAuth", BuildSchema(sensitiveProperty: "ApiKey")));

        var connectionTypeCollection = new ConnectionTypeCollection(() => _registeredConnectionTypes);
        var stripper = new SensitiveSettingsStripper(
            new ActionCollection(() => []),
            new TriggerCollection(() => []),
            new ControlFlowCollection(() => []),
            connectionTypeCollection);

        _connector = new UmbracoAutomateConnectionServiceConnector(
            _connectionServiceMock.Object,
            connectionTypeCollection,
            stripper,
            _settingsAccessorMock.Object);
    }

    private static IConnectionType BuildConnectionTypeMock(string alias, EditableModelSchema? schema = null)
    {
        var mock = new Mock<IConnectionType>();
        mock.SetupGet(x => x.Alias).Returns(alias);
        mock.Setup(x => x.GetSettingsSchema()).Returns(schema);
        return mock.Object;
    }

    private static EditableModelSchema BuildSchema(string sensitiveProperty) => new()
    {
        Fields =
        [
            new EditableModelFieldDescriptor
            {
                Key = sensitiveProperty,
                Label = sensitiveProperty,
                PropertyName = sensitiveProperty,
                PropertyType = typeof(string),
                IsSensitive = true,
            },
        ],
    };

    private Connection BuildConnection(Dictionary<string, object?>? settings = null) => new()
    {
        Alias = "test-connection",
        Name = "Test Connection",
        Type = "httpBasicAuth",
        Settings = settings ?? [],
    };

    [Fact]
    public async Task GetArtifactAsync_WithConfigurationReference_PreservesValue()
    {
        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "$MyService:ApiKey",
            ["Endpoint"] = "https://api.example.com",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");
        // $ config refs pass through — IgnoreEncrypted only filters ENC: prefixes.
        settings.ShouldContainKey("ApiKey");
        settings["ApiKey"]!.ToString().ShouldBe("$MyService:ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithEncryptedValue_FiltersWhenIgnoreEncryptedTrue()
    {
        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "ENC:abc123encrypted",
            ["Endpoint"] = "https://api.example.com",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");
        settings.ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreEncryptedFalse_PreservesEncryptedValue()
    {
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings
        {
            Connections = new DeployAutomateConnectionSettings { IgnoreEncrypted = false },
        });

        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "ENC:abc123encrypted",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreSettingsList_FiltersNamedFields()
    {
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings
        {
            Connections = new DeployAutomateConnectionSettings
            {
                IgnoreEncrypted = false,
                IgnoreSettings = ["ApiKey"],
            },
        });

        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "plain-secret",
            ["Endpoint"] = "https://api.example.com",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");
        settings.ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreSettingsCaseInsensitive_FiltersField()
    {
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings
        {
            Connections = new DeployAutomateConnectionSettings
            {
                IgnoreEncrypted = false,
                IgnoreSettings = ["apikey"],
            },
        });

        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "plain-secret",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        // Only the filtered field was present, so Settings becomes null after filtering.
        artifact.Settings.ShouldBeNull();
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreSensitiveTrue_StripsSensitiveFieldRegardlessOfValue()
    {
        // IgnoreSensitive removes fields by schema (IsSensitive=true) regardless of value —
        // including $ config references that IgnoreEncrypted would otherwise pass through.
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings
        {
            Connections = new DeployAutomateConnectionSettings
            {
                IgnoreSensitive = true,
                IgnoreEncrypted = false,
            },
        });

        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "$MyService:ApiKey",
            ["Endpoint"] = "https://api.example.com",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("Endpoint");
        settings.ShouldNotContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithIgnoreSensitiveFalse_KeepsSensitiveConfigReferences()
    {
        // Default: IgnoreSensitive=false. Sensitive fields holding $ refs should pass through,
        // since the source secret is in IConfiguration on the target, not in the artifact.
        var connection = BuildConnection(new Dictionary<string, object?>
        {
            ["ApiKey"] = "$MyService:ApiKey",
        });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithUnknownConnectionType_AndIgnoreSensitive_DoesNotThrow()
    {
        // The export path runs even when the connection type isn't registered (the deploy
        // pass that validates registration runs at import time). IgnoreSensitive should
        // degrade to a no-op rather than blowing up.
        _registeredConnectionTypes.Clear();
        _settingsAccessorMock.Setup(x => x.Settings).Returns(new DeployAutomateSettings
        {
            Connections = new DeployAutomateConnectionSettings { IgnoreSensitive = true },
        });

        var connection = BuildConnection(new Dictionary<string, object?> { ["ApiKey"] = "plain" });
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldNotBeNull();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(artifact.Settings.Value);
        settings.ShouldNotBeNull();
        settings.ShouldContainKey("ApiKey");
    }

    [Fact]
    public async Task GetArtifactAsync_WithEmptySettings_ReturnsNullSettings()
    {
        var connection = BuildConnection();
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Settings.ShouldBeNull();
    }

    [Fact]
    public async Task GetArtifactAsync_CopiesAliasNameAndType()
    {
        var connection = BuildConnection();
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, connection.Id);

        var artifact = await _connector.GetArtifactAsync(udi, connection);

        artifact.ShouldNotBeNull();
        artifact.Alias.ShouldBe("test-connection");
        artifact.Name.ShouldBe("Test Connection");
        artifact.Type.ShouldBe("httpBasicAuth");
    }

    [Fact]
    public async Task GetArtifactAsync_WithNullEntity_ReturnsNull()
    {
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, Guid.NewGuid());

        var artifact = await _connector.GetArtifactAsync(udi, null);

        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityAsync_DelegatesToConnectionService()
    {
        var id = Guid.NewGuid();
        var connection = BuildConnection();
        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        var result = await _connector.GetEntityAsync(id);

        result.ShouldBe(connection);
    }

    [Fact]
    public void GetEntityName_ReturnsConnectionName()
    {
        var connection = BuildConnection();

        _connector.GetEntityName(connection).ShouldBe("Test Connection");
    }

    [Fact]
    public void UdiEntityType_ReturnsConnectionUdiType()
    {
        _connector.UdiEntityType.ShouldBe(DeployAutomateConstants.UdiEntityType.Connection);
    }

    [Fact]
    public async Task ProcessAsync_WithUnknownConnectionType_ThrowsWithActionableMessage()
    {
        _registeredConnectionTypes.Clear();

        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, Guid.NewGuid());
        var artifact = new AutomateConnectionArtifact(udi, new ArtifactDependencyCollection())
        {
            Alias = "slack-team",
            Name = "Slack Team",
            Type = "slack",
        };
        var state = ArtifactDeployState.Create<AutomateConnectionArtifact, Connection>(artifact, null, _connector, 2);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2));

        ex.Message.ShouldContain("'slack'");
        ex.Message.ShouldContain("Slack Team");
        _connectionServiceMock.Verify(
            x => x.CreateConnectionAsync(It.IsAny<Connection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _connectionServiceMock.Verify(
            x => x.UpdateConnectionAsync(It.IsAny<Connection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithRegisteredConnectionType_CreatesConnection()
    {
        var udi = new GuidUdi(DeployAutomateConstants.UdiEntityType.Connection, Guid.NewGuid());
        var artifact = new AutomateConnectionArtifact(udi, new ArtifactDependencyCollection())
        {
            Alias = "basic",
            Name = "Basic Auth",
            Type = "httpBasicAuth",
        };
        var state = ArtifactDeployState.Create<AutomateConnectionArtifact, Connection>(artifact, null, _connector, 2);
        _connectionServiceMock
            .Setup(x => x.CreateConnectionAsync(It.IsAny<Connection>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection c, Guid? _, CancellationToken _) => c);

        await _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        _connectionServiceMock.Verify(
            x => x.CreateConnectionAsync(It.Is<Connection>(c => c.Type == "httpBasicAuth"), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
