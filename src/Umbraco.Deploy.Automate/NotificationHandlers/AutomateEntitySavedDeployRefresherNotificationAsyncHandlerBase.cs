using Umbraco.Automate.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Deploy.Automate.Extensions;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure;
using Umbraco.Deploy.Infrastructure.Disk;
using Umbraco.Extensions;

namespace Umbraco.Deploy.Automate.NotificationHandlers;

/// <summary>
/// Base class for handling saved notifications for Umbraco.Automate entities.
/// Automatically writes artifacts to disk when entities are saved, if the entity type is an enabled
/// Deploy disk entity type. Signatures are always updated.
/// </summary>
public abstract class AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
    : INotificationAsyncHandler<TNotification>
    where TEntity : class, IAutomateEntity
    where TNotification : INotification
{
    private readonly IServiceConnectorFactory _serviceConnectorFactory;
    private readonly IDiskEntityService _diskEntityService;
    private readonly ISignatureService _signatureService;
    private readonly IOptionsMonitor<DeployEntityTypesOptions> _deployEntityTypes;
    private readonly IOptionsMonitor<DeploySettings> _deploySettings;
    private readonly string _entityType;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    protected AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase(
        IServiceConnectorFactory serviceConnectorFactory,
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        IOptionsMonitor<DeployEntityTypesOptions> deployEntityTypes,
        IOptionsMonitor<DeploySettings> deploySettings,
        string entityType)
    {
        _serviceConnectorFactory = serviceConnectorFactory;
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
    protected AutomateEntitySavedDeployRefresherNotificationAsyncHandlerBase(
        IServiceConnectorFactory serviceConnectorFactory,
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        string entityType)
        : this(
            serviceConnectorFactory,
            diskEntityService,
            signatureService,
            StaticServiceProvider.Instance.GetRequiredService<IOptionsMonitor<DeployEntityTypesOptions>>(),
            StaticServiceProvider.Instance.GetRequiredService<IOptionsMonitor<DeploySettings>>(),
            entityType)
    {
    }

    /// <summary>
    /// Extracts the entity from the notification.
    /// </summary>
    protected abstract TEntity GetEntity(TNotification notification);

    /// <inheritdoc />
    public async Task HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var entity = GetEntity(notification);

        var artifacts = await _serviceConnectorFactory
            .GetArtifactsAsync(_entityType, [entity], new DictionaryCache(), cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Read at the point of use: the options can change at runtime
        if (_deployEntityTypes.CurrentValue.IsDiskEntityTypeEnabled(_entityType, _deploySettings.CurrentValue))
        {
            await _diskEntityService.WriteArtifactsAsync(artifacts, cancellationToken).ConfigureAwait(false);
        }

        _signatureService.SetSignatures(artifacts);
    }
}
