using Umbraco.Cms.Core;

namespace Umbraco.Deploy.Automate.Tests.Unit.Connectors.ServiceConnectors;

/// <summary>
/// Registers the Automate UDI types in <see cref="UdiParser"/>'s static state, so a test that creates
/// or parses an Automate UDI doesn't depend on another test having composed <c>DeployAutomateComposer</c> first.
/// </summary>
internal static class AutomateUdiTypes
{
    public static void Register()
    {
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Workspace, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.WorkspaceGroup, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(DeployAutomateConstants.UdiEntityType.Automation, UdiType.GuidUdi);
    }
}
