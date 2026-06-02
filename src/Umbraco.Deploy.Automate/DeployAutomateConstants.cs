namespace Umbraco.Deploy.Automate;

/// <summary>
/// Constants used throughout Umbraco Deploy Automate.
/// </summary>
public static class DeployAutomateConstants
{
    /// <summary>
    /// UDI entity type identifiers for Umbraco.Automate entities.
    /// </summary>
    internal static class EnvironmentApi
    {
        internal const string RootPath = "/umbraco/backoffice/deploy/environment";
    }

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
