using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Environments;
using Umbraco.Deploy.Core.Exceptions;
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

        if (settings.IgnoreDependencies &&
            !_deploySettings.AllowIgnoreDependenciesOperations.HasFlag(IgnoreDependenciesOperations.Restore))
        {
            return Task.FromResult(ActionResult.Failed(
                new InvalidOperationException("Ignoring dependencies is not allowed for restore operations. Set Umbraco:Deploy:Settings:AllowIgnoreDependenciesOperations to allow it."),
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

        // Build the restore entity type sets from the registered transfer entity types,
        // honoring each registration's restore options — the public equivalent of the
        // internal sets Deploy itself passes when starting a backoffice restore:
        // - select: types that can be selected for restore (SupportsRestore)
        // - deploy: types permitted to deploy during restore, plus content files (PermittedToRestore)
        var restoreSelectTypes = new HashSet<string>(
            DeployEntityTypes.GetEntityTypes(
                _fileTypeCollection,
                DeployEntityTypeCategories.Content,
                _deploySettings,
                detail => detail.Options.SupportsRestore));
        var restoreDeployTypes = new HashSet<string>(
            DeployEntityTypes.GetEntityTypes(
                _fileTypeCollection,
                DeployEntityTypeCategories.Content | DeployEntityTypeCategories.ContentFile,
                _deploySettings,
                detail => detail.Options.PermittedToRestore));

        var work = _workItemFactory.CreateTargetRestore(
            _currentEnvironment,
            sourceEnvironment,
            restoreSelectTypes,
            restoreDeployTypes,
            new HashSet<string>(),
            _deploySettings.ExcludedEntityTypes,
            settings.IgnoreDependencies);

        work.OwnerName = "Umbraco Automate";

        try
        {
            if (_currentEnvironment.Worker.Begin(work, _deploySettings.SourceDeployTimeout) is null)
            {
                return Task.FromResult(ActionResult.Failed(
                    new InvalidOperationException("The restore could not be started because the environment is not available."),
                    StepRunErrorCategory.ServiceUnavailable));
            }
        }
        catch (EnvironmentBusyException ex)
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
