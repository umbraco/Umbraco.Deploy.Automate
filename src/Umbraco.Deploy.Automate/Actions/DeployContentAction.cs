using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Environments;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Core;
using Umbraco.Deploy.Infrastructure.Environments;
using Umbraco.Deploy.Infrastructure.Work;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Starts a content transfer (deploy) of a specific item to a target environment.
/// </summary>
[Action("umbracoDeploy.deployContent", "Start Content Transfer",
    Description = "Starts a deploy of the specified content to a target environment.",
    Group = "Deploy",
    Icon = "icon-arrow-right",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class DeployContentAction : ActionBase<DeployContentSettings, DeployContentOutput>
{

    private readonly CurrentEnvironment _currentEnvironment;
    private readonly IWorkItemFactory _workItemFactory;
    private readonly IExtractEnvironmentInfo _environmentInfoExtractor;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly IFileTypeCollection _fileTypeCollection;
    private readonly DeploySettings _deploySettings;

    public DeployContentAction(
        ActionInfrastructure infrastructure,
        CurrentEnvironment currentEnvironment,
        IWorkItemFactory workItemFactory,
        IExtractEnvironmentInfo environmentInfoExtractor,
        IHostingEnvironment hostingEnvironment,
        IFileTypeCollection fileTypeCollection,
        IOptions<DeploySettings> deploySettings)
        : base(infrastructure)
    {
        _currentEnvironment = currentEnvironment;
        _workItemFactory = workItemFactory;
        _environmentInfoExtractor = environmentInfoExtractor;
        _hostingEnvironment = hostingEnvironment;
        _fileTypeCollection = fileTypeCollection;
        _deploySettings = deploySettings.Value;
    }

    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<DeployContentSettings>();

        if (string.IsNullOrWhiteSpace(settings.Udi))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException("A UDI is required."),
                StepRunErrorCategory.Validation));
        }

        if (!UdiParser.TryParse(settings.Udi, out Udi? udi) || udi is null)
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException($"'{settings.Udi}' is not a valid UDI."),
                StepRunErrorCategory.Validation));
        }

        if (string.IsNullOrWhiteSpace(settings.TargetUrl))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException("A target URL is required."),
                StepRunErrorCategory.Validation));
        }

        IEnvironmentInfo? targetEnvironment = _environmentInfoExtractor.ResolveEnvironmentInfo(settings.TargetUrl);
        if (targetEnvironment is null)
        {
            return Task.FromResult(ActionResult.Failed(
                new InvalidOperationException($"Could not resolve target environment from '{settings.TargetUrl}'. Verify it is a configured Deploy environment."),
                StepRunErrorCategory.Validation));
        }

        Uri? applicationMainUrl = _hostingEnvironment.ApplicationMainUrl;
        if (applicationMainUrl is null)
        {
            return Task.FromResult(ActionResult.Failed(
                new InvalidOperationException("The application main URL is not configured. Set Umbraco:CMS:WebRouting:UmbracoApplicationUrl in appsettings.json."),
                StepRunErrorCategory.ConfigurationError));
        }

        var sourceUrl = new Uri(applicationMainUrl.AbsoluteUri.TrimEnd('/') + DeployAutomateConstants.EnvironmentApi.RootPath);

        // GetEntityTypes with Content flag returns all registered transfer entity types —
        // the public equivalent of the internal DeployEntityTypes.GetRegisteredTransferEntityTypes().
        ISet<string> deployEntityTypes = new HashSet<string>(
            DeployEntityTypes.GetEntityTypes(_fileTypeCollection, DeployEntityTypeCategories.Content));

        var work = _workItemFactory.CreateSourceDeploy(
            _currentEnvironment,
            sourceUrl,
            targetEnvironment,
            [new UdiWithOptions(udi)],
            deployEntityTypes,
            new HashSet<string>(),
            _deploySettings.ExcludedEntityTypes,
            settings.IgnoreDependencies);

        try
        {
            _currentEnvironment.Worker.Begin(work, _deploySettings.SourceDeployTimeout);
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(ActionResult.Failed(ex, StepRunErrorCategory.ServiceUnavailable));
        }

        return Task.FromResult(Success(new DeployContentOutput
        {
            SessionId = work.Id,
            Status = "Started",
        }));
    }
}
