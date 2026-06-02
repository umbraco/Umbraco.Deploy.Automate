using Umbraco.Automate.Core.Actions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
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
    private readonly IFileTypeCollection _fileTypeCollection;

    public AddToQueueAction(ActionInfrastructure infrastructure, ITransferQueue transferQueue, IFileTypeCollection fileTypeCollection)
        : base(infrastructure)
    {
        _transferQueue = transferQueue;
        _fileTypeCollection = fileTypeCollection;
    }

    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<AddToQueueSettings>();

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

        var transferEntityTypes = DeployEntityTypes.GetEntityTypes(_fileTypeCollection, content: true, schema: false, contentFile: false, schemaFile: false);
        if (!transferEntityTypes.Contains(udi.EntityType))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException($"Entity type '{udi.EntityType}' cannot be added to the transfer queue."),
                StepRunErrorCategory.Validation));
        }

        if (string.IsNullOrWhiteSpace(settings.UserId) || !int.TryParse(settings.UserId, out int userId))
        {
            return Task.FromResult(ActionResult.Failed(
                new ArgumentException("User ID must be a whole number (e.g. 1234)."),
                StepRunErrorCategory.Validation));
        }

        var culture = string.IsNullOrWhiteSpace(settings.Culture) ? null : settings.Culture;
        var queueItem = new QueueItem(
            new NamedUdiRange(udi, udi.ToString(), UmbracoConstants.DeploySelector.This),
            culture,
            releaseDate: null);

        _transferQueue.Add(userId, queueItem);

        return Task.FromResult(Success(new AddToQueueOutput
        {
            AddedUdi = udi.ToString(),
            QueueSize = _transferQueue.Get(userId).Count,
        }));
    }
}
