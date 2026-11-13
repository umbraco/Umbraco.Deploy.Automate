using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.Deploy.Automate.Artifacts;

/// <summary>
/// Represents a deployment artifact for an Automate workspace.
/// </summary>
public class AutomateWorkspaceArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The service account key (UserKind.Api user) of the workspace on the source environment.
    /// No longer exported: the service account is environment-specific, and exporting it made
    /// the workspace checksum differ between environments, which broke every transfer of an
    /// automation or group that depends on the workspace. Never applied on import either. The
    /// property remains so older artifacts that carry it still deserialize.
    /// </summary>
    [Obsolete("Not exported: the service account is environment-specific. Will be removed in v21.0.0.")]
    public Guid ServiceAccountKey { get; set; }

    /// <summary>
    /// User group keys that have access to this workspace.
    /// Stored as raw GUIDs since these are CMS-level entities.
    /// </summary>
    public IList<Guid> UserGroups { get; set; } = [];

    /// <summary>
    /// UDIs of connections allowed in this workspace.
    /// </summary>
    public IList<GuidUdi> AllowedConnectionUdis { get; set; } = [];
}
