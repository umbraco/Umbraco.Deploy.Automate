# Umbraco.Deploy.Automate

The integration package between [Umbraco Deploy](https://umbraco.com/products/umbraco-deploy/) and [Umbraco Automate](https://umbraco.com/products/umbraco-automate/).

This package does two things:

1. **Transfers Automate entities through Deploy** — workspaces, workspace groups, automations, and connections move between environments using the same Queue / Restore / Partial Restore flows you already use for content and schema.
2. **Exposes Deploy events as Automate triggers** — build automation flows that react to deployment activity (task completed/failed, artifact exported/imported, files written/deleted, and more).

## Installation

```bash
dotnet add package Umbraco.Deploy.Automate
```

`DeployAutomateComposer` is auto-discovered by Umbraco's composition system; no manual registration is required.

## Requirements

| Dependency | Version |
|---|---|
| .NET | 10 |
| Umbraco CMS | 17.x |
| Umbraco Deploy | 17.x |
| Umbraco Automate | 0.1+ |

---

## Part 1 — Deploying Automate entities

The package registers Deploy service connectors for the four user-defined Automate entities, so each one appears in the Deploy back-office tree alongside content and schema. Queue for Transfer, Restore, and Partial Restore all work as expected.

### Supported entities

| Entity | UDI entity type | Notes |
|---|---|---|
| Workspace | `umbraco-automate-workspace` | Includes service account key, user-group assignments, and allowed-connection list. |
| Workspace Group | `umbraco-automate-workspace-group` | Folder hierarchy; nested groups are reparented in a second pass after every group exists on the target. |
| Automation | `umbraco-automate-automation` | Full trigger / steps / step-connections / notification-settings graph, including conditions and canvas state. |
| Connection | `umbraco-automate-connection` | Type alias plus the settings dictionary, with sensitive-value filtering (see below). |

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
- **Connection settings** are filtered on export and merged (not replaced) on import:
  - Values prefixed with `ENC:` are stripped when `IgnoreEncrypted` is enabled (default).
  - Field names listed in `IgnoreSettings` are stripped unconditionally.
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

## Usage examples

### Notify team on deployment failure

```
Trigger: Deployment Failed (umbracoDeploy.taskFailed)
  → Condition: ExceptionType is not empty
  → Action: Send Slack message to #deployments
             "Deployment failed for {OwnerEmail}: {ExceptionMessage}"
```

### Log every imported document

```
Trigger: Content Imported (umbracoDeploy.artifactImported)
  → Condition: ArtifactType == "document"
  → Action: Write to log
             "Imported document '{ArtifactName}' ({ArtifactUdi})"
```

### Alert on disk deploy completion

```
Trigger: Disk Deploy Completed (umbracoDeploy.diskTriggered)
  → Condition: Result != "Succeeded"
  → Action: Send email to operations team
```

### React when a specific content type is deployed

```
Trigger: Content Imported (umbracoDeploy.artifactImported)
  → Condition: ArtifactAlias starts with "news"
  → Action: Trigger cache invalidation
```

### Monitor deployment duration

```
Trigger: Deployment Succeeded (umbracoDeploy.taskCompleted)
  → Condition: Duration > 60
  → Action: Log performance warning
             "Long deployment by {OwnerEmail}: {Duration}s, processed {ProcessCount} items"
```

---

## Repository layout

```
src/
  Umbraco.Deploy.Automate/
    Artifacts/                       # Deploy artifacts for Automate entities
    Configuration/                   # DeployAutomateSettings
    Connectors/ServiceConnectors/    # One service connector per Automate entity
    NotificationHandlers/            # Cache refresh handlers for save/delete
    Triggers/                        # Automate triggers backed by Deploy notifications
    Triggers/Outputs/                # Trigger output types
    Tree/                            # Back-office tree helpers for Deploy dialogs
    Workspaces/                      # WorkspaceGroupDeploySaver (bypasses domain validation)
    DeployAutomateComponent.cs       # Registers UDI, disk, and transfer entity types
    DeployAutomateComposer.cs        # DI composition
tests/
  Umbraco.Deploy.Automate.Tests.Unit/
```

---

## Future work

The following Deploy operations could be exposed as Automate actions in a future version:

| Action | Description |
|---|---|
| Trigger Deployment | Programmatically initiate a Deploy task to a target environment |
| Export Artifact | Export a specific content item by UDI |
| Import Artifact | Import a specific artifact from a package |
| Restore from Disk | Trigger a disk-based restore operation |
| Validate Schema | Run schema validation against a target environment |
| Queue Deploy Task | Queue a deployment task for later execution |

---

## History

This package started life as a small collection of custom Deploy triggers for Umbraco Automate. The Deploy integration for Automate entities originally lived in the Umbraco.Automate monorepo as `Umbraco.Automate.Deploy`, and was moved into this repo so that the entire Deploy⇄Automate story ships as a single package.

*Parts of this package were built with [Claude](https://claude.ai) by Anthropic.*
