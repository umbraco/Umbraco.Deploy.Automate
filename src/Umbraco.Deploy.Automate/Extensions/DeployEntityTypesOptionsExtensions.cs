using Umbraco.Deploy.Core.Configuration.DeployConfiguration;
using Umbraco.Deploy.Infrastructure;

namespace Umbraco.Deploy.Automate.Extensions;

internal static class DeployEntityTypesOptionsExtensions
{
    /// <summary>
    /// Determines whether the entity type is a disk entity type that is not excluded, the same rule
    /// Deploy applies before writing UDA files for its own entity types.
    /// </summary>
    public static bool IsDiskEntityTypeEnabled(this DeployEntityTypesOptions options, string entityType, DeploySettings deploySettings)
        => options.DiskEntityTypes.Contains(entityType) && deploySettings.ExcludedEntityTypes.Contains(entityType) is false;
}
