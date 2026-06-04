using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Core.Transfer.Queue;
using Umbraco.Deploy.Infrastructure;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Deploy.Automate.Actions;

/// <summary>
/// Adds a content item to a backoffice user's Deploy transfer queue.
/// </summary>
[Action("umbracoDeploy.addToQueue", "Add to Deploy Queue",
    Description = "Adds a content item to a backoffice user's Deploy transfer queue.",
    Group = "Deploy",
    Icon = "icon-add",
    RequiredSections = [UmbracoConstants.Applications.Settings])]
public sealed class AddToQueueAction : ActionBase<AddToQueueSettings, AddToQueueOutput>
{
    private readonly ITransferQueue _transferQueue;
    private readonly IServiceConnectorFactory _serviceConnectorFactory;
    private readonly IFileTypeCollection _fileTypeCollection;
    private readonly DeploySettings _deploySettings;

    public AddToQueueAction(
        ActionInfrastructure infrastructure,
        ITransferQueue transferQueue,
        IServiceConnectorFactory serviceConnectorFactory,
        IFileTypeCollection fileTypeCollection,
        IOptions<DeploySettings> deploySettings)
        : base(infrastructure)
    {
        _transferQueue = transferQueue;
        _serviceConnectorFactory = serviceConnectorFactory;
        _fileTypeCollection = fileTypeCollection;
        _deploySettings = deploySettings.Value;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<AddToQueueSettings>();

        if (string.IsNullOrWhiteSpace(settings.Udi))
        {
            return ActionResult.Failed(
                new ArgumentException("A UDI is required."),
                StepRunErrorCategory.Validation);
        }

        if (!UdiParser.TryParse(settings.Udi, out Udi? udi) || udi is null)
        {
            return ActionResult.Failed(
                new ArgumentException($"'{settings.Udi}' is not a valid UDI."),
                StepRunErrorCategory.Validation);
        }

        // Only entity types registered with queue-for-transfer support (and not excluded
        // by configuration) can be added to the queue, matching the backoffice behavior.
        var transferEntityTypes = DeployEntityTypes.GetEntityTypes(
            _fileTypeCollection,
            DeployEntityTypeCategories.Content,
            _deploySettings,
            detail => detail.Options.SupportsQueueForTransfer);
        if (!transferEntityTypes.Contains(udi.EntityType))
        {
            return ActionResult.Failed(
                new ArgumentException($"Entity type '{udi.EntityType}' cannot be added to the transfer queue."),
                StepRunErrorCategory.Validation);
        }

        if (string.IsNullOrWhiteSpace(settings.UserId))
        {
            return ActionResult.Failed(
                new ArgumentException("A user ID is required."),
                StepRunErrorCategory.Validation);
        }

        if (!int.TryParse(settings.UserId, out int userId))
        {
            return ActionResult.Failed(
                new ArgumentException($"'{settings.UserId}' is not a valid user ID. It must be a whole number (e.g. 1234)."),
                StepRunErrorCategory.Validation);
        }

        // Resolve the item via its service connector to validate it exists
        // and get its actual name for display in the queue.
        NamedUdiRange udiRange;
        try
        {
            IServiceConnector serviceConnector = _serviceConnectorFactory.GetConnector(udi.EntityType);
            udiRange = await serviceConnector.GetRangeAsync(udi, UmbracoConstants.DeploySelector.This, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            return ActionResult.Failed(
                new ArgumentException($"Could not resolve '{udi}'. Verify the item exists.", ex),
                StepRunErrorCategory.Validation);
        }

        var culture = string.IsNullOrWhiteSpace(settings.Culture) ? null : settings.Culture;
        var queueItem = new QueueItem(udiRange, culture, releaseDate: null);

        _transferQueue.Add(userId, queueItem);

        return Success(new AddToQueueOutput
        {
            AddedUdi = udi.ToString(),
            QueueSize = _transferQueue.Get(userId).Count,
        });
    }
}
