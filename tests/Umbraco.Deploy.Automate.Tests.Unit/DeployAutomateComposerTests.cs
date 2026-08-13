using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Deploy.Automate.Tests.Unit;

public class DeployAutomateComposerTests
{
    // Registration has to happen during composition, not from DeployAutomateComponent: on an upgrade
    // boot components don't initialize until after migrations, and Deploy's work-item worker can read
    // an Automate artifact before then. Composing without ever initializing the component is what
    // reproduces that window, so these assertions fail if the calls move back into the component.
    [Theory]
    [InlineData(DeployAutomateConstants.UdiEntityType.Connection)]
    [InlineData(DeployAutomateConstants.UdiEntityType.Workspace)]
    [InlineData(DeployAutomateConstants.UdiEntityType.WorkspaceGroup)]
    [InlineData(DeployAutomateConstants.UdiEntityType.Automation)]
    public void Compose_RegistersUdiType(string entityType)
    {
        new DeployAutomateComposer().Compose(CreateBuilder());

        var key = Guid.NewGuid();

        UdiParser.Parse($"umb://{entityType}/{key:N}").ShouldBe(new GuidUdi(entityType, key));
    }

    private static IUmbracoBuilder CreateBuilder() => new UmbracoBuilder(
        new ServiceCollection(),
        new ConfigurationBuilder().Build(),
        new TypeLoader(Mock.Of<ITypeFinder>(), NullLogger<TypeLoader>.Instance));
}
