using Umbraco.Deploy.Automate.Configuration;

// Schema wrapper consumed by the JsonSchemaGenerate MSBuild task at build time.
// Describes the appsettings.json shape below Umbraco:Deploy:Automate so tooling
// can give editors IntelliSense against appsettings-schema.Umbraco.Deploy.Automate.json.
internal sealed class DeployAutomateSchema
{
    /// <summary>
    /// Configuration container for all Umbraco products.
    /// </summary>
    public required UmbracoDefinition Umbraco { get; set; }

    public sealed class UmbracoDefinition
    {
        /// <summary>
        /// Configuration of Umbraco Deploy.
        /// </summary>
        public required UmbracoDeployDefinition Deploy { get; set; }
    }

    public sealed class UmbracoDeployDefinition
    {
        /// <summary>
        /// Umbraco Deploy integration for Umbraco Automate.
        /// </summary>
        public required DeployAutomateSettings Automate { get; set; }
    }
}
