// Registers Deploy entity actions (Queue for transfer / Tree restore / Partial restore)
// against Automate's tree entity types. Mirrors the pattern in Umbraco.Forms.Deploy.

const UA_WORKSPACE_ENTITY_TYPE = "ua:workspace";
const UA_WORKSPACE_ROOT_ENTITY_TYPE = "ua:workspace-root";
const UA_AUTOMATION_ENTITY_TYPE = "ua:automation";
const UA_AUTOMATION_ROOT_ENTITY_TYPE = "ua:automation-root";
const UA_AUTOMATION_GROUP_ENTITY_TYPE = "ua:automation-group";

// Automate's tree uses client entity types prefixed with "ua:", but Deploy registers
// transfer options server-side against UDI entity types. This mapping lets Deploy's
// lookup (`DeployContext.getRegisteredEntity`) resolve a tree entity type to the
// matching server registration.
const UA_SERVER_WORKSPACE_ENTITY_TYPE = "umbraco-automate-workspace";
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
          deploy_entityTypes: {
            [UA_SERVER_WORKSPACE_ENTITY_TYPE]: "Automation",
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
        [UA_WORKSPACE_ENTITY_TYPE]: UA_SERVER_WORKSPACE_ENTITY_TYPE,
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
            UA_WORKSPACE_ENTITY_TYPE,
            UA_AUTOMATION_ROOT_ENTITY_TYPE,
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
          entityTypes: [
            UA_WORKSPACE_ROOT_ENTITY_TYPE,
            UA_AUTOMATION_ROOT_ENTITY_TYPE,
          ],
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
            UA_WORKSPACE_ENTITY_TYPE,
            UA_AUTOMATION_ROOT_ENTITY_TYPE,
            UA_AUTOMATION_ENTITY_TYPE,
            UA_AUTOMATION_GROUP_ENTITY_TYPE,
          ],
          conditions: ["default"],
        },
      ],
    },
  ]);
};
