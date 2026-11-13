using Umbraco.Automate.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Deploy.Automate.Extensions;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

/// <summary>
/// Base class for handling deleted notifications for Umbraco.Automate entities.
/// Automatically removes artifacts from disk when entities are deleted, if the entity type is an enabled
/// Deploy disk entity type. Signatures are always cleared.
/// </summary>
public abstract class AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
    : INotificationAsyncHandler<TNotification>
    where TEntity : class, IAutomateEntity
    where TNotification : INotification
{
    private readonly IDiskEntityService _diskEntityService;
    private readonly ISignatureService _signatureService;
    private readonly IOptionsMonitor<DeployEntityTypesOptions> _deployEntityTypes;
    private readonly IOptionsMonitor<DeploySettings> _deploySettings;
    private readonly string _entityType;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    protected AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase(
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
        IOptionsMonitor<DeploySettings> deploySettings,
        string entityType)
    {
        _diskEntityService = diskEntityService;
        _signatureService = signatureService;
        _deployEntityTypes = deployEntityTypes;
        _deploySettings = deploySettings;
        _entityType = entityType;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    [Obsolete("Use the constructor taking IOptionsMonitor<DeployEntityTypesOptions> and IOptionsMonitor<DeploySettings>. Will be removed in v21.0.0.")]
    protected AutomateEntityDeletedDeployRefresherNotificationAsyncHandlerBase(
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        string entityType)
        : this(
            diskEntityService,
            signatureService,
            StaticServiceProvider.Instance.GetRequiredService<IOptionsMonitor<DeployEntityTypesOptions>>(),
            StaticServiceProvider.Instance.GetRequiredService<IOptionsMonitor<DeploySettings>>(),
            entityType)
    {
    }

    /// <summary>
    /// Extracts the entity ID from the notification.
    /// </summary>
    protected abstract Guid GetEntityId(TNotification notification);

    /// <inheritdoc />
    public Task HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var entityId = GetEntityId(notification);
        var udi = Udi.Create(_entityType, entityId);

        // Read at the point of use: the options can change at runtime
        if (_deployEntityTypes.CurrentValue.IsDiskEntityTypeEnabled(_entityType, _deploySettings.CurrentValue))
        {
            _diskEntityService.DeleteArtifacts(
                [entityId],
                id => Udi.Create(_entityType, id),
                _ => $"Deleted {_entityType}");
        }

        _signatureService.ClearSignatures([udi]);

        return Task.CompletedTask;
    }
}
