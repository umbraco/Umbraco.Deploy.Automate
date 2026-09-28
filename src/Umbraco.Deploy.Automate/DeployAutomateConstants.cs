namespace Umbraco.Deploy.Automate;

/// <summary>
/// Constants used throughout Umbraco Deploy Automate.
/// </summary>
public static class DeployAutomateConstants
{
    /// <summary>
    /// Constants for the Deploy Environment API.
    /// </summary>
    internal static class EnvironmentApi
    {
        /// <summary>
        /// The API root path (mirrors the internal Umbraco.Deploy.Infrastructure.ApiConstants.EnvironmentApi.RootPath).
        /// </summary>
        internal const string RootPath = "/umbraco/backoffice/deploy/environment";
    }

    /// <summary>
    /// Markers used to recognise connection settings fields that hold environment-specific
    /// values (currently OAuth credential references) and must never be transferred.
    /// </summary>
    /// <remarks>
    /// OAuth-based connection types (e.g. Slack) store a Guid that points at a row in the
    /// OpenIddict credentials table on the environment where the user authenticated. That
    /// table is never deployed, so the id is meaningless on any other environment. These
    /// values mirror Umbraco.Automate's OAuth field conventions without taking a dependency
    /// on Umbraco.Automate.OpenIddict or any provider package.
    /// </remarks>
    internal static class EnvironmentSpecificConnectionSettings
    {
        /// <summary>
        /// The backoffice property editor UI alias Automate uses for OAuth credential fields
        /// (<c>[Field(EditorUiAlias = "Umb.Automate.OAuth")]</c>).
        /// </summary>
        internal const string OAuthEditorUiAlias = "Umb.Automate.OAuth";

        /// <summary>
        /// The conventional settings property name for an OAuth credential reference. Used as a
        /// fallback when the connection type's settings schema is unavailable, and matched
        /// case-insensitively (stored settings keys are typically camelCased).
        /// </summary>
        internal const string OAuthCredentialsIdPropertyName = "OAuthCredentialsId";
    }

    /// <summary>
    /// UDI entity type identifiers for Umbraco.Automate entities.
    /// </summary>
    public static class UdiEntityType
    {
        /// <summary>
        /// UDI entity type for automations.
        /// </summary>
        public const string Automation = "umbraco-automate-automation";

        /// <summary>
        /// UDI entity type for workspaces.
        /// </summary>
        public const string Workspace = "umbraco-automate-workspace";

        /// <summary>
        /// UDI entity type for connections.
        /// </summary>
        public const string Connection = "umbraco-automate-connection";

        /// <summary>
        /// UDI entity type for workspace groups (folders that organize automations within a workspace).
        /// </summary>
        public const string WorkspaceGroup = "umbraco-automate-workspace-group";
    }
}
