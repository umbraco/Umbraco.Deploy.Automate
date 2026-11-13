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
using Umbraco.Deploy.Infrastructure.Disk;
using Umbraco.Deploy.Infrastructure.Environments;
using Umbraco.Deploy.Infrastructure.Transfer;
using Umbraco.Deploy.Infrastructure.Work;

namespace Umbraco.Deploy.Automate.Tests.Unit.Actions;

public class DeployContentActionTests
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

    public DeployContentActionTests()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
        RegisterEntityTypes();
        _workspaces.Setup(x => x.GetRestoreWorkspaces()).Returns([Baseline, Live]);
    }

    [Fact]
    public async Task DeployContent_ToTheBaselineWorkspace_FailsValidation()
    {
        ActionResult result = await ExecuteDeployContentAsync(Baseline.Url);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task DeployContent_ToTheBaselineWorkspace_SaysItIsTheBaselineWorkspace()
    {
        ActionResult result = await ExecuteDeployContentAsync(Baseline.Url);

        result.Exception.ShouldNotBeNull().Message.ShouldContain("Baseline");
    }

    // Control for the tests above: a hosted workspace passes the Baseline check and fails on the next precondition.
    [Fact]
    public async Task DeployContent_ToAHostedWorkspace_IsNotRejectedAsBaseline()
    {
        ActionResult result = await ExecuteDeployContentAsync(Live.Url);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }

    // The registry Deploy keeps the transfer types in is process-wide, and the Automation type is only
    // registered when Deploy Automate's own component runs.
    private static void RegisterEntityTypes()
    {
        var transferEntityService = (ITransferEntityService)Activator.CreateInstance(
            typeof(ITransferEntityService).Assembly.GetType("Umbraco.Deploy.Infrastructure.Transfer.TransferEntityService")!,
            nonPublic: true)!;

        new DeployAutomateComponent(new Mock<IDiskEntityService>().Object, transferEntityService)
            .InitializeAsync(false, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
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
            .WithService(_workspaces.Object)
            .ExecuteAsync();

    private static IFileTypeCollection CreateFileTypes()
    {
        var fileTypes = new Mock<IFileTypeCollection>();
        fileTypes.Setup(x => x.GetEntityTypes()).Returns(new List<string>());
        return fileTypes.Object;
    }

    // The action only passes the environment on, and these paths never reach it
    private static CurrentEnvironment CreateCurrentEnvironment()
        => (CurrentEnvironment)RuntimeHelpers.GetUninitializedObject(typeof(CurrentEnvironment));
}
