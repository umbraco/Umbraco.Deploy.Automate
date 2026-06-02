using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Environments;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Environments;
using Umbraco.Deploy.Infrastructure.Work;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Triggers a full content restore from a source environment into the current environment.
/// </summary>
[Action("umbracoDeploy.triggerRestore", "Trigger Restore",
    Description = "Pulls and restores all deployable content from a source environment.",
    Group = "Deploy",
    Icon = "icon-arrow-down",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class TriggerRestoreAction : ActionBase<TriggerRestoreSettings, TriggerRestoreOutput>
{

    private readonly CurrentEnvironment _currentEnvironment;
    private readonly IWorkItemFactory _workItemFactory;
    private readonly IExtractEnvironmentInfo _environmentInfoExtractor;
    private readonly IFileTypeCollection _fileTypeCollection;
    private readonly DeploySettings _deploySettings;

    public TriggerRestoreAction(
        ActionInfrastructure infrastructure,
        CurrentEnvironment currentEnvironment,
        IWorkItemFactory workItemFactory,
        IExtractEnvironmentInfo environmentInfoExtractor,
        IFileTypeCollection fileTypeCollection,
        IOptions<DeploySettings> deploySettings)
        : base(infrastructure)
    {
        _currentEnvironment = currentEnvironment;
        _workItemFactory = workItemFactory;
        _environmentInfoExtractor = environmentInfoExtractor;
        _fileTypeCollection = fileTypeCollection;
        _deploySettings = deploySettings.Value;
    }

    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<TriggerRestoreSettings>();

        if (string.IsNullOrWhiteSpace(settings.SourceUrl))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException("A source URL is required."),
                StepRunErrorCategory.Validation));
        }

        IEnvironmentInfo? sourceEnvironment = _environmentInfoExtractor.ResolveEnvironmentInfo(settings.SourceUrl);
        if (sourceEnvironment is null)
        {
            return Task.FromResult(ActionResult.Failed(
                new InvalidOperationException($"Could not resolve source environment from '{settings.SourceUrl}'. Verify it is a configured Deploy environment."),
                StepRunErrorCategory.Validation));
        }

        var sourceApiUrl = new Uri(settings.SourceUrl.TrimEnd('/') + DeployAutomateConstants.EnvironmentApi.RootPath);
        sourceEnvironment.SetUri(sourceApiUrl);

        // content=true, contentFile=false → Document/Media/Blueprint (no MediaFile) = RestoreSelectEntityTypes
        // content=true, contentFile=true  → above + MediaFile                         = RestoreDeployEntityTypes
        var restoreSelectTypes = new HashSet<string>(
            DeployEntityTypes.GetEntityTypes(_fileTypeCollection, content: true, schema: false, contentFile: false, schemaFile: false));
        var restoreDeployTypes = new HashSet<string>(
            DeployEntityTypes.GetEntityTypes(_fileTypeCollection, content: true, schema: false, contentFile: true, schemaFile: false));

        var work = _workItemFactory.CreateTargetRestore(
            _currentEnvironment,
            sourceEnvironment,
            restoreSelectTypes,
            restoreDeployTypes,
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

        return Task.FromResult(Success(new TriggerRestoreOutput
        {
            SessionId = work.Id,
            Status = "Started",
        }));
    }
}
