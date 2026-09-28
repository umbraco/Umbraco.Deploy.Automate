using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.Deploy.Automate.Artifacts;

/// <summary>
/// Represents a deployment artifact for an Automate connection.
/// </summary>
public class AutomateConnectionArtifact(GuidUdi udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    /// <summary>
    /// The connection type alias (e.g. "httpBasicAuth", "oauth2").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The connection settings serialized as JSON, with sensitive values filtered based on deploy settings.
    /// </summary>
    /// <remarks>
    /// Environment-specific fields — OAuth credential references such as Slack's
    /// <c>OAuthCredentialsId</c>, which point at a credentials row that only exists on the
    /// source environment — are always left out on export and deliberately NOT applied on
    /// import, even if an artifact from an older version still carries them (see
    /// <c>UmbracoAutomateConnectionServiceConnector.RemoveEnvironmentSpecificSettings</c>).
    /// An admin must authenticate OAuth connections on each environment.
    /// </remarks>
    public JsonElement? Settings { get; set; }
}
