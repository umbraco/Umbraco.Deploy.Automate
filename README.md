<p align="center">
  <img alt="Umbraco Automate" src="./assets/logo-128.png" width="128">
</p>

# Umbraco.Deploy.Automate

The integration package between [Umbraco Deploy](https://umbraco.com/products/umbraco-deploy/) and [Umbraco Automate](https://umbraco.com/products/umbraco-automate/).

This package does three things:

1. **Moves Automate entities between environments with Deploy** — automations and their folders (workspace groups) use the Add to Transfer Queue, Tree restore and Partial restore flows you already use for content. Workspaces and connections are deployed with the schema.
2. **Exposes Deploy events as Automate triggers** — build automation flows that react to deployment activity (task completed/failed, artifact exported/imported, files written/deleted, and more).
3. **Exposes Deploy operations as Automate actions** — call Deploy from an automation step to deploy content, queue items for transfer, or trigger a restore.

## Installation

Version 19 is a pre-release (labelled `-rcN`) while Umbraco CMS 19 is in pre-release. It is on nuget.org, so there is no feed to add. Install it with `--prerelease`:

```bash
dotnet add package Umbraco.Deploy.Automate --prerelease
```

Version 18 installs without the flag: `dotnet add package Umbraco.Deploy.Automate`.

`DeployAutomateComposer` is auto-discovered by Umbraco's composition system; no manual registration is required.

## Requirements

| Dependency | Version |
|---|---|
| .NET | 11 |
| Umbraco CMS | 19 |
| Umbraco Deploy | 19 |
| Umbraco Automate | 19 |

### Upgrading from version 18

**Important:** automations and folders no longer write `.uda` files, and Deploy 19 ignores the ones you already have. Do these steps in order:

1. Before upgrading, get automations and folders in sync across your environments.
2. Upgrade.
3. Delete the `umbraco-automate-automation__<guid>.uda` and `umbraco-automate-workspace-group__<guid>.uda` files from `umbraco/Deploy/Revision`. Keep the `umbraco-automate-workspace__<guid>.uda` and `umbraco-automate-connection__<guid>.uda` files. Don't delete by a `umbraco-automate-workspace*` pattern, as it also matches the workspace files.
4. Move automations and folders from now on with **Add to Transfer Queue**, **Tree restore** or **Partial restore**.

Also:

- Workspaces and connections no longer show Add to Transfer Queue, Tree restore or Partial restore. They are written as `.uda` files and deployed with the schema.
- Deploy 19's UDA file health check lists the workspace and connection `.uda` files that Deploy 18 wrote, until you export the schema again.

---

## Part 1 — Deploying Automate entities

The package registers Deploy service connectors for the four user-defined Automate entities, so each one appears in the Deploy back-office tree alongside content and schema. The entities move in one of two ways:

- **Transfer** (automations and workspace groups): use Add to Transfer Queue, Tree restore and Partial restore, like content. These actions are on the Automations tree. Partial restore from the header shows your workspaces. Open one and pick a folder or automation, not the workspace itself. Tree restore is on the Automations header only.
- **Disk only** (workspaces and connections): saving one writes its `.uda` file; commit it and deploy it with the schema. They have no transfer or restore actions.

Deploy the schema (workspaces and connections) before you transfer automations. Otherwise the import fails with "Ensure the workspace is deployed before the automation".

### Supported entities

| Entity | UDI entity type | Notes |
|---|---|---|
| Workspace (disk) | `umbraco-automate-workspace` | Includes user-group assignments and the allowed-connection list. The service account is environment-specific and isn't transferred. |
| Workspace Group (transfer) | `umbraco-automate-workspace-group` | Folder hierarchy; nested groups are reparented in a second pass after every group exists on the target. |
| Automation (transfer) | `umbraco-automate-automation` | Full trigger / steps / step-connections / notification-settings graph, including conditions and canvas state. |
| Connection (disk) | `umbraco-automate-connection` | Type alias plus the settings dictionary, with sensitive-value filtering (see below). |

Runtime entities (`AutomationRun`, `StepRun`) are intentionally not transferable — execution history is per-environment.

### Dependency resolution

Connectors emit `Match` dependencies so target environments resolve the graph automatically:

- A **workspace** depends on its allowed `Connection`s and on the `UserGroup`s assigned to it.
- A **workspace group** depends on its `Workspace` and (if nested) on its parent group.
- An **automation** depends on its `Workspace`, optional `WorkspaceGroup`, and on every `Connection` referenced by a step.
- A **connection** has no Automate-side dependencies.

Process passes are ordered to honour the graph: Connections (pass 2) → Workspaces (pass 3) → Workspace Groups (passes 4 & 5 — second pass reparents) → Automations (pass 6).

### Preserved settings

All user-facing settings round-trip with two intentional exceptions:

- **`Automation.Status` and `Automation.PublishedVersion`** are not overwritten on update. Redeploying a live automation does not knock it back to draft; the target environment's lifecycle state is preserved. New automations are created as `Draft` so an operator must explicitly publish on the target.
- **Connection settings** are filtered on export and merged (not replaced) on import, applied in this precedence order:
  1. Field names listed in `IgnoreSettings` are stripped unconditionally.
  2. If `IgnoreSensitive` is enabled, every field marked `[Field(IsSensitive = true)]` on the connection type's settings schema is stripped, regardless of its value — including `$` configuration references. Off by default.
  3. Values prefixed with `ENC:` are stripped when `IgnoreEncrypted` is enabled (default). `$`-prefixed configuration references are passed through.
  - Settings already present on the target are preserved for any key not in the artifact.
- **Sensitive trigger / step settings** flagged via the action or trigger's settings schema (`IsSensitive = true`) are stripped from automation artifacts before serialization. The rest of the step (id, alias, name, connection id, input mappings, position, error behaviour, retries) is preserved verbatim.

### Validation on import

Importing fails fast (rather than landing dangling references) when the target site is missing a contributing package:

- An automation referencing an unknown `TriggerAlias` or step `ActionAlias`.
- A connection of an unknown `Type` alias.

The error message names the missing alias and points at the absent package.

### Configuration

```json
{
  "Umbraco": {
    "Deploy": {
      "Automate": {
        "Connections": {
          "IgnoreEncrypted": true,
          "IgnoreSensitive": false,
          "IgnoreSettings": [ "apiKey", "clientSecret" ]
        }
      }
    }
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `Connections.IgnoreEncrypted` | `true` | Strip values starting with `ENC:` from exported connection settings. `$`-prefixed configuration references are always allowed through. |
| `Connections.IgnoreSensitive` | `false` | Strip every field marked `[Field(IsSensitive = true)]` on the connection type's settings schema, regardless of value. Enable for zero sensitive metadata in deployment artifacts. |
| `Connections.IgnoreSettings` | `[]` | Specific connection setting field names to always strip on export. Case-insensitive. |

A JSON schema for these options is shipped at `appsettings-schema.Umbraco.Deploy.Automate.json`.

---

## Part 2 — Deploy events as Automate triggers

Umbraco Deploy publishes its notifications through the standard Umbraco CMS notification pipeline, so the triggers below subscribe to them natively — no bridge handlers are required.

### Must-have triggers

| Trigger | Alias | Description |
|---|---|---|
| `TaskCompletedTrigger` | `umbracoDeploy.taskCompleted` | Fires when a deployment task completes successfully |
| `TaskFailedTrigger` | `umbracoDeploy.taskFailed` | Fires when a deployment task fails |
| `ArtifactExportedTrigger` | `umbracoDeploy.artifactExported` | Fires after a content artifact is exported |
| `ArtifactImportedTrigger` | `umbracoDeploy.artifactImported` | Fires after a content artifact is imported |

> **Note on "deployment started":** Umbraco Deploy does not publish a dedicated task-started notification. `TaskNotification` is the base class only; the pipeline publishes `TaskCompletedNotification` or `TaskFailedNotification` as the terminal events. Use `WorkContextPreparingTrigger` (Advanced) as the closest signal to a "deployment starting" event.

#### TaskCompletedTrigger output

| Property | Type | Description |
|---|---|---|
| `WorkItemId` | `Guid` | Unique identifier of the work item |
| `WorkItemType` | `string` | Full type name of the work item |
| `OwnerName` | `string` | Name of the user who initiated the deployment |
| `OwnerEmail` | `string` | Email of the user who initiated the deployment |
| `EventTrigger` | `string` | What triggered the deployment (e.g. `"manual"`, `"scheduled"`) |
| `Duration` | `double` | Duration in seconds |
| `WorkCount` | `int` | Total items before negotiation |
| `ProcessCount` | `int` | Total items after negotiation |

#### TaskFailedTrigger output

| Property | Type | Description |
|---|---|---|
| `WorkItemId` | `Guid` | Unique identifier of the work item |
| `WorkItemType` | `string` | Full type name of the work item |
| `OwnerName` | `string` | Name of the user who initiated the deployment |
| `OwnerEmail` | `string` | Email of the user who initiated the deployment |
| `EventTrigger` | `string` | What triggered the deployment |
| `Duration` | `double` | Duration in seconds |
| `ExceptionMessage` | `string?` | Exception message if an error occurred |
| `ExceptionType` | `string?` | Exception type name if an error occurred |

#### ArtifactExportedTrigger / ArtifactImportedTrigger output

| Property | Type | Description |
|---|---|---|
| `ArtifactUdi` | `string` | Full UDI string (e.g. `udi://document/abc123`) |
| `ArtifactType` | `string` | Entity type (e.g. `"document"`, `"media"`) |
| `ArtifactAlias` | `string` | Artifact alias |
| `ArtifactName` | `string` | Artifact display name |

### Nice-to-have triggers

| Trigger | Alias | Description |
|---|---|---|
| `FilesWrittenTrigger` | `umbracoDeploy.filesWritten` | Fires when content files are written to disk |
| `FilesDeletedTrigger` | `umbracoDeploy.filesDeleted` | Fires when content files are deleted from disk |
| `RemoteCompletedTrigger` | `umbracoDeploy.remoteCompleted` | Fires when a remote deploy operation completes |
| `DiskTriggeredTrigger` | `umbracoDeploy.diskTriggered` | Fires when a disk-triggered deploy completes |

#### FilesWrittenTrigger / FilesDeletedTrigger / UserUpdatedTrigger output

| Property | Type | Description |
|---|---|---|
| `UserEmail` | `string` | Email of the user who triggered the disk operation |
| `UserName` | `string` | Name of the user who triggered the disk operation |
| `FileCount` | `int` | Number of files written/deleted |

#### RemoteCompletedTrigger output

| Property | Type | Description |
|---|---|---|
| `WorkId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Type of the work item |
| `WorkItemEnvironment` | `string` | Target environment name |
| `WorkCount` | `int` | Total items before negotiation |
| `ProcessCount` | `int` | Total items after negotiation |
| `Duration` | `double` | Duration in seconds |

#### DiskTriggeredTrigger output

| Property | Type | Description |
|---|---|---|
| `Result` | `string` | Result string: `"Succeeded"`, `"Failed"`, or `"Retry"` |
| `ExceptionType` | `string` | Exception type name if a failure occurred |

### Advanced triggers

| Trigger | Alias | Description |
|---|---|---|
| `ArtifactExportingTrigger` | `umbracoDeploy.artifactExporting` | Fires before an artifact is exported (pre-export) |
| `ArtifactImportingTrigger` | `umbracoDeploy.artifactImporting` | Fires before an artifact is imported (pre-import) |
| `ValidateArtifactImportTrigger` | `umbracoDeploy.validateArtifactImport` | Fires when artifacts are being validated before import |
| `UserUpdatedTrigger` | `umbracoDeploy.userUpdated` | Fires when user-related deploy files are updated |
| `WorkContextPreparingTrigger` | `umbracoDeploy.workContextPreparing` | Fires when a deploy work context is being prepared |

> **Cancelable notifications:** `ArtifactExportingTrigger`, `ArtifactImportingTrigger`, and `ValidateArtifactImportTrigger` are backed by cancelable Deploy notifications. Cancellation is not available through the Automate trigger system — these triggers fire for observation only.

#### ArtifactExportingTrigger / ArtifactImportingTrigger output

Same properties as the post-export/import triggers (`ArtifactUdi`, `ArtifactType`, `ArtifactAlias`, `ArtifactName`), but fires **before** the operation occurs.

#### ValidateArtifactImportTrigger output

| Property | Type | Description |
|---|---|---|
| `ArtifactCount` | `int` | Number of artifacts being validated |
| `ArtifactUdis` | `IEnumerable<string>` | UDI strings of all artifacts being validated |

#### WorkContextPreparingTrigger output

| Property | Type | Description |
|---|---|---|
| `WorkItemId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Work item type name |
| `OwnerName` | `string` | Name of the initiating user |
| `OwnerEmail` | `string` | Email of the initiating user |
| `EventTrigger` | `string` | What triggered this work item |

---

## Part 3 — Deploy operations as Automate actions

Run Deploy operations from an Automate flow.

| Action | What it does |
|---|---|
| Deploy Content | Starts a deploy of the specified content to a target environment. It refuses the Baseline environment (Deploy's Baseline workspace) as a target, because Baseline can only be restored from. |
| Add To Queue | Adds a content item to a backoffice user's Deploy transfer queue |
| Trigger Restore | Pulls and restores all deployable content from a source environment. Enter the source environment's URL as it's set up in Deploy, for example `https://live.mysite.com`. The scheme, host and port must match. Don't add a path such as `/umbraco`: it's kept, and the restore fails. A query string or fragment is dropped. |

---

## Usage examples

### Notify team on deployment failure

```
Trigger: Task Failed
  → Condition: ExceptionType is not empty
  → Action: Send Slack message to #deployments
            "Deployment failed for {OwnerEmail}: {ExceptionMessage}"
```

### Log every imported document

```
Trigger: Artifact Imported
  → Condition: ArtifactType == "document"
  → Action: Write to log
            "Imported document '{ArtifactName}' ({ArtifactUdi})"
```

### Monitor deployment duration

```
Trigger: Task Completed
  → Condition: Duration > 60
  → Action: Log performance warning
            "Long deployment by {OwnerEmail}: {Duration}s, processed {ProcessCount} items"
```

## Repository layout

```
src/
  Umbraco.Deploy.Automate/
    Actions/                          # Automate actions that call Deploy operations
    Artifacts/                        # Deploy artifacts for Automate entities
    Configuration/                    # DeployAutomateSettings
    Connectors/ServiceConnectors/     # One service connector per Automate entity (also lists its partial-restore tree level)
    NotificationHandlers/             # Cache refresh handlers for save/delete
    Triggers/                         # Automate triggers backed by Deploy notifications
    Triggers/Outputs/                 # Trigger output types
    Workspaces/                       # WorkspaceGroupDeploySaver (bypasses domain validation)
    DeployAutomateComposer.cs         # DI composition; ConfigureEntityTypes registers the disk and transfer entity types
tests/
  Umbraco.Deploy.Automate.Tests.Unit/
```

---

## Future work

The following Deploy operations could be exposed as Automate actions in a future version:

| Action | Description |
|---|---|
| Export Artifact | Export a specific content item by UDI |
| Import Artifact | Import a specific artifact from a package |
| Restore from Disk | Trigger a disk-based restore operation |
| Validate Schema | Run schema validation against a target environment |
| Queue Deploy Task | Queue a deployment task for later execution |

---

## History

This package started life as a small collection of custom Deploy triggers for Umbraco Automate. The Deploy integration for Automate entities originally lived in the Umbraco.Automate monorepo as `Umbraco.Automate.Deploy`, and was moved into this repo so that the entire Deploy⇄Automate story ships as a single package.

*Parts of this package were built with [Claude](https://claude.ai) by Anthropic.*

## License

MIT — see [LICENSE](LICENSE) for details.
