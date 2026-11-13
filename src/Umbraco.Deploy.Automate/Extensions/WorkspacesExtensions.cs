using Umbraco.Deploy.Core.Configuration;
using Umbraco.Deploy.Core.Configuration.DeployProjectConfiguration;
using Umbraco.Deploy.Core.Environments;

namespace Umbraco.Deploy.Automate.Extensions;

internal static class WorkspacesExtensions
{
    /// <summary>
    /// Determines whether the environment is the Baseline workspace, which can only be restored from,
    /// the same rule Deploy applies to the targets of a transfer.
    /// </summary>
    public static bool IsBaseline(this IWorkspaces workspaces, IEnvironmentInfo environmentInfo)
        => workspaces.GetRestoreWorkspaces().OfType<Workspace>().Any(x =>
            x.Kind == WorkspaceKind.Baseline &&
            x.Id.ToString() == environmentInfo.Id &&
            x.Type == environmentInfo.Type &&
            x.Name == environmentInfo.Name);
}
