// Registers Deploy entity actions (Queue for transfer / Tree restore / Partial restore)
// against Automate's tree entity types. Mirrors the pattern in Umbraco.Forms.Deploy.

// Automate's visible "Automations" root is `ua:workspace-root` (the sidebar header menu); its tree is
// root > `ua:workspace` > `ua:automation-group` > `ua:automation`. The separate `ua:automation-root`
// tree has no menu, so nothing is registered on it.
const UA_WORKSPACE_ROOT_ENTITY_TYPE = "ua:workspace-root";
const UA_WORKSPACE_ENTITY_TYPE = "ua:workspace";
const UA_AUTOMATION_ENTITY_TYPE = "ua:automation";
const UA_AUTOMATION_GROUP_ENTITY_TYPE = "ua:automation-group";

// Automate's tree uses client entity types prefixed with "ua:", but Deploy registers
// transfer options server-side against UDI entity types. This mapping lets Deploy's
// lookup (`DeployContext.getTransferEntityType`) resolve a tree entity type to the
// matching server registration. Only automations and folders (workspace groups) are
// transfer types; workspaces and connections are disk only, so they get no Queue / Restore
// actions. `ua:workspace` is mapped to the automation type only because Deploy strips
// "-root" before the lookup, so the header's `ua:workspace-root` resolves through it. No
// Deploy action is registered on `ua:workspace` itself, so workspace nodes have no actions.
const UA_SERVER_WORKSPACE_ENTITY_TYPE = "umbraco-automate-workspace";
const UA_SERVER_CONNECTION_ENTITY_TYPE = "umbraco-automate-connection";
const UA_SERVER_WORKSPACE_GROUP_ENTITY_TYPE = "umbraco-automate-workspace-group";
const UA_SERVER_AUTOMATION_ENTITY_TYPE = "umbraco-automate-automation";

export const onInit = async (host, extensionRegistry) => {
  extensionRegistry.registerMany([
    {
      type: "localization",
      alias: "DeployAutomate.Localization.En",
      weight: -100,
      name: "English",
      meta: {
        culture: "en",
        localizations: {
          // Keys are Deploy's server-side UDI entity types — that's what
          // Deploy passes to `deploy_entityTypes_<entityType>` when resolving
          // labels in dialogs (e.g. "This will put all <name> items in the queue").
          // Singular, sentence case like Deploy's own values: the partial restore
          // picker lowercases it into "Select <name> to restore".
          // The workspace and connection labels are for queue items left over on sites
          // upgraded from v18; Deploy still reads them for queue rows.
          deploy_entityTypes: {
            [UA_SERVER_WORKSPACE_ENTITY_TYPE]: "Automation workspace",
            [UA_SERVER_CONNECTION_ENTITY_TYPE]: "Automation connection",
            [UA_SERVER_WORKSPACE_GROUP_ENTITY_TYPE]: "Automation folder",
            [UA_SERVER_AUTOMATION_ENTITY_TYPE]: "Automation",
          },
        },
      },
    },
    {
      type: "deployEntityTypeMapping",
      alias: "DeployAutomate.EntityTypeMapping",
      name: "Deploy Automate Entity Type Mapping",
      entityTypes: {
        [UA_WORKSPACE_ENTITY_TYPE]: UA_SERVER_AUTOMATION_ENTITY_TYPE,
        [UA_AUTOMATION_ENTITY_TYPE]: UA_SERVER_AUTOMATION_ENTITY_TYPE,
        [UA_AUTOMATION_GROUP_ENTITY_TYPE]: UA_SERVER_WORKSPACE_GROUP_ENTITY_TYPE,
      },
    },
    {
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.EnvironmentRestore",
      alias: "DeployAutomate.EnvironmentRestore.Registrar",
      name: "Deploy Automate Environment Restore Entity Action Registrar",
      forEntityTypes: [
        {
          entityTypes: [UA_WORKSPACE_ROOT_ENTITY_TYPE],
          conditions: ["default"],
        },
      ],
    },
    {
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.EnvironmentExport",
      alias: "DeployAutomate.EnvironmentExport.Registrar",
      name: "Deploy Automate Environment Export Entity Action Registrar",
      forEntityTypes: [
        {
          entityTypes: [UA_WORKSPACE_ROOT_ENTITY_TYPE],
          conditions: ["default"],
        },
      ],
    },
    {
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.Queue",
      alias: "DeployAutomate.Queue.Registrar",
      name: "Deploy Automate Queue Entity Action Registrar",
      forEntityTypes: [
        {
          entityTypes: [
            UA_WORKSPACE_ROOT_ENTITY_TYPE,
            UA_AUTOMATION_ENTITY_TYPE,
            UA_AUTOMATION_GROUP_ENTITY_TYPE,
          ],
          conditions: ["default"],
        },
      ],
    },
    {
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.TreeRestore",
      alias: "DeployAutomate.TreeRestore.Registrar",
      name: "Deploy Automate Tree Restore Entity Action Registrar",
      forEntityTypes: [
        {
          entityTypes: [UA_WORKSPACE_ROOT_ENTITY_TYPE],
          conditions: ["default"],
        },
      ],
    },
    {
      type: "deployEntityActionRegistrar",
      actionAlias: "Deploy.EntityAction.PartialRestore",
      alias: "DeployAutomate.PartialRestore.Registrar",
      name: "Deploy Automate Partial Restore Entity Action Registrar",
      forEntityTypes: [
        {
          entityTypes: [
            UA_WORKSPACE_ROOT_ENTITY_TYPE,
            UA_AUTOMATION_ENTITY_TYPE,
            UA_AUTOMATION_GROUP_ENTITY_TYPE,
          ],
          conditions: ["default"],
        },
      ],
    },
  ]);
};
