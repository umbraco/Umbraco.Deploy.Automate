using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Deploy.Automate.Actions;
using Umbraco.Deploy.Core.Configuration;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Configuration.DeployProjectConfiguration;
using Umbraco.Deploy.Core.Environments;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Environments;
using Umbraco.Deploy.Infrastructure.Work;

namespace Umbraco.Deploy.Automate.Tests.Unit.Actions;

public class DeployEnvironmentActionTests
{
    private static readonly Workspace Baseline = new("Baseline", "baseline", "https://baseline.example.com")
    {
        Id = Guid.NewGuid(),
        Kind = WorkspaceKind.Baseline,
    };

    private static readonly Workspace Live = new("Live", "live", "https://live.example.com/")
    {
        Id = Guid.NewGuid(),
    };

    private readonly Mock<IWorkspaces> _workspaces = new();
    private readonly Mock<IWorkItemFactory> _workItemFactory = new();
    private readonly Mock<IHostingEnvironment> _hostingEnvironment = new();
    private readonly DeployEntityTypesOptions _entityTypes = new();

    public DeployEnvironmentActionTests()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
        DeployAutomateComposer.ConfigureEntityTypes(_entityTypes);
        _workspaces.Setup(x => x.GetRestoreWorkspaces()).Returns([Baseline, Live]);
    }

    [Fact]
    public async Task DeployContent_ToTheBaselineWorkspace_FailsValidation()
    {
        ActionResult result = await ExecuteDeployContentAsync(Baseline.Url);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    // Control for the test above: a hosted workspace passes the Baseline check and fails on the next precondition.
    [Fact]
    public async Task DeployContent_ToAHostedWorkspace_IsNotRejectedAsBaseline()
    {
        ActionResult result = await ExecuteDeployContentAsync(Live.Url);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    [Fact]
    public async Task TriggerRestore_FromAWorkspace_UsesTheConfiguredWorkspaceUrlForTheEnvironmentApi()
    {
        IEnvironmentInfo? source = null;
        _workItemFactory
            .Setup(x => x.CreateTargetRestore(
                It.IsAny<CurrentEnvironment>(),
                It.IsAny<IEnvironmentInfo>(),
                It.IsAny<ISet<string>>(),
                It.IsAny<ISet<string>>(),
                It.IsAny<ISet<string>>(),
                It.IsAny<ISet<string>>(),
                It.IsAny<bool>()))
            .Callback<CurrentEnvironment, IEnvironmentInfo, ISet<string>, ISet<string>, ISet<string>, ISet<string>, bool>(
                (_, environment, _, _, _, _, _) => source = environment)
            .Throws(new InvalidOperationException("stop"));

        await ExecuteTriggerRestoreAsync("https://LIVE.example.com/?token=abc");

        source!.Uri.ToString().ShouldBe("https://live.example.com" + DeployAutomateConstants.EnvironmentApi.RootPath);
    }

    private async Task<ActionResult> ExecuteDeployContentAsync(string targetUrl)
        => await ActionTestHarness.For<DeployContentAction>()
            .WithSettings(new DeployContentSettings
            {
                Udi = Udi.Create(DeployAutomateConstants.UdiEntityType.Automation, Guid.NewGuid()).ToString(),
                TargetUrl = targetUrl,
            })
            .WithService(CreateCurrentEnvironment())
            .WithService(_workItemFactory.Object)
            .WithService<IExtractEnvironmentInfo>(new ConfigurationEnvironmentInfoExtractor(_workspaces.Object))
            .WithService(_hostingEnvironment.Object)
            .WithService(CreateFileTypes())
            .WithService(Options.Create(new DeploySettings()))
            .WithService(Monitor(_entityTypes))
            .WithService(_workspaces.Object)
            .ExecuteAsync();

    private async Task ExecuteTriggerRestoreAsync(string sourceUrl)
    {
        try
        {
            await ActionTestHarness.For<TriggerRestoreAction>()
                .WithSettings(new TriggerRestoreSettings { SourceUrl = sourceUrl })
                .WithService(CreateCurrentEnvironment())
                .WithService(_workItemFactory.Object)
                .WithService<IExtractEnvironmentInfo>(new ConfigurationEnvironmentInfoExtractor(_workspaces.Object))
                .WithService(CreateFileTypes())
                .WithService(Options.Create(new DeploySettings()))
                .WithService(Monitor(_entityTypes))
                .ExecuteAsync();
        }
        catch (InvalidOperationException)
        {
            // Thrown by the work item factory once the source environment has been captured
        }
    }

    private static IFileTypeCollection CreateFileTypes()
    {
        var fileTypes = new Mock<IFileTypeCollection>();
        fileTypes.Setup(x => x.GetEntityTypes()).Returns(new List<string>());
        return fileTypes.Object;
    }

    // The actions only pass the environment on, and these paths never reach it
    private static CurrentEnvironment CreateCurrentEnvironment()
        => (CurrentEnvironment)RuntimeHelpers.GetUninitializedObject(typeof(CurrentEnvironment));

    private static IOptionsMonitor<T> Monitor<T>(T value)
        where T : class
    {
        var monitor = new Mock<IOptionsMonitor<T>>();
        monitor.Setup(x => x.CurrentValue).Returns(value);
        return monitor.Object;
    }
}
