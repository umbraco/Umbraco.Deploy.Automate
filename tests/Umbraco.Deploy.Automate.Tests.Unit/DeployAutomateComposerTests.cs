using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Deploy.Automate.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Transfer;

namespace Umbraco.Deploy.Automate.Tests.Unit;

public class DeployAutomateComposerTests
{
    [Fact]
    public void Compose_RegistersTheWorkspaceAndConnectionAsDiskEntityTypes()
    {
        var options = ComposeEntityTypes();

        options.DiskEntityTypes.OrderBy(x => x).ShouldBe(
            new[]
            {
                DeployAutomateConstants.UdiEntityType.Connection,
                DeployAutomateConstants.UdiEntityType.Workspace,
            }.OrderBy(x => x));
    }

    [Fact]
    public void Compose_RegistersTheWorkspaceGroupAndAutomationAsTransferEntityTypes()
    {
        var options = ComposeEntityTypes();

        options.TransferEntityTypes.Keys.OrderBy(x => x).ShouldBe(
            new[]
            {
                DeployAutomateConstants.UdiEntityType.Automation,
                DeployAutomateConstants.UdiEntityType.WorkspaceGroup,
            }.OrderBy(x => x));
    }

    // Deploy throws on the first read of the options when a type is in both sets.
    [Fact]
    public void Compose_DoesNotRegisterAnEntityTypeAsBothDiskAndTransfer()
    {
        var options = ComposeEntityTypes();

        options.TransferEntityTypes.Keys.Intersect(options.DiskEntityTypes).ShouldBeEmpty();
    }

    [Fact]
    public void Compose_AllowsQueueingAWorkspaceGroupWithItsDescendants()
    {
        var options = ComposeEntityTypes().TransferEntityTypes[DeployAutomateConstants.UdiEntityType.WorkspaceGroup];

        options.SupportsQueueForTransferOfDescendents.ShouldBeTrue();
    }

    [Fact]
    public void Compose_DoesNotAllowQueueingAnAutomationWithDescendants()
    {
        var options = ComposeEntityTypes().TransferEntityTypes[DeployAutomateConstants.UdiEntityType.Automation];

        options.SupportsQueueForTransferOfDescendents.ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeployAutomateConstants.UdiEntityType.WorkspaceGroup)]
    [InlineData(DeployAutomateConstants.UdiEntityType.Automation)]
    public void Compose_AllowsTransferEntityTypesToBeQueuedRestoredAndPartiallyRestored(string entityType)
    {
        var options = ComposeEntityTypes().TransferEntityTypes[entityType];

        (options.SupportsQueueForTransfer
            && options.SupportsRestore
            && options.PermittedToRestore
            && options.SupportsPartialRestore
            && options.SupportsImportExport).ShouldBeTrue();
    }

    // Deploy registers UDI types from these attributes at composition, so the composer no longer does.
    [Theory]
    [InlineData(typeof(UmbracoAutomateConnectionServiceConnector), DeployAutomateConstants.UdiEntityType.Connection)]
    [InlineData(typeof(UmbracoAutomateWorkspaceServiceConnector), DeployAutomateConstants.UdiEntityType.Workspace)]
    [InlineData(typeof(UmbracoAutomateWorkspaceGroupServiceConnector), DeployAutomateConstants.UdiEntityType.WorkspaceGroup)]
    [InlineData(typeof(UmbracoAutomateAutomationServiceConnector), DeployAutomateConstants.UdiEntityType.Automation)]
    public void Connector_DeclaresItsGuidUdiType(Type connectorType, string entityType)
    {
        var definition = connectorType.GetCustomAttribute<UdiDefinitionAttribute>();

        (definition?.UdiType, definition?.EntityType).ShouldBe((UdiType.GuidUdi, entityType));
    }

    private static DeployEntityTypesOptions ComposeEntityTypes()
    {
        var builder = new UmbracoBuilder(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            new TypeLoader(Mock.Of<ITypeFinder>(), NullLogger<TypeLoader>.Instance));

        new DeployAutomateComposer().Compose(builder);

        return builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DeployEntityTypesOptions>>().Value;
    }
}
