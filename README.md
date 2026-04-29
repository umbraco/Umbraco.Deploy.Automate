# Umbraco.Deploy.Automate

Umbraco Deploy triggers for [Umbraco Automate](https://umbraco.com/products/umbraco-automate/), enabling automation flows based on deployment events.

## Overview

This package exposes Umbraco Deploy notifications as Automate triggers, allowing you to build automation flows that react to deployment activity — content exports and imports, task completions and failures, disk operations, and more.

Because Umbraco Deploy publishes notifications directly through the Umbraco CMS notification pipeline (as `INotification`), no bridge handlers are required. Triggers subscribe to Deploy notifications natively.

## Installation

```bash
dotnet add package Umbraco.Deploy.Automate
```

The `DeployAutomateComposer` is auto-discovered by Umbraco's composition system. No manual registration is required.

---

## Implemented Triggers

### Must-Have Triggers

| Trigger | Alias | Description |
|---------|-------|-------------|
| `TaskCompletedTrigger` | `umbracodeploy.taskCompleted` | Fires when a deployment task completes successfully |
| `TaskFailedTrigger` | `umbracodeploy.taskFailed` | Fires when a deployment task fails |
| `ArtifactExportedTrigger` | `umbracodeploy.artifactExported` | Fires after a content artifact is exported |
| `ArtifactImportedTrigger` | `umbracodeploy.artifactImported` | Fires after a content artifact is imported |

> **Note on Deployment Started:** Umbraco Deploy does not publish a dedicated "task started" notification. `TaskNotification` is the base class only; the pipeline publishes `TaskCompletedNotification` or `TaskFailedNotification` as the terminal events. Use `WorkContextPreparingTrigger` (Advanced) as the closest equivalent to a "deployment starting" signal.

#### TaskCompletedTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `WorkItemId` | `Guid` | Unique identifier of the work item |
| `WorkItemType` | `string` | Full type name of the work item |
| `OwnerName` | `string` | Name of the user who initiated the deployment |
| `OwnerEmail` | `string` | Email of the user who initiated the deployment |
| `EventTrigger` | `string` | What triggered the deployment (e.g. `"manual"`, `"scheduled"`) |
| `Duration` | `double` | Duration in seconds |
| `WorkCount` | `int` | Total items before negotiation |
| `ProcessCount` | `int` | Total items after negotiation |

#### TaskFailedTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `WorkItemId` | `Guid` | Unique identifier of the work item |
| `WorkItemType` | `string` | Full type name of the work item |
| `OwnerName` | `string` | Name of the user who initiated the deployment |
| `OwnerEmail` | `string` | Email of the user who initiated the deployment |
| `EventTrigger` | `string` | What triggered the deployment |
| `Duration` | `double` | Duration in seconds |
| `ExceptionMessage` | `string?` | Exception message if an error occurred |
| `ExceptionType` | `string?` | Exception type name if an error occurred |

#### ArtifactExportedTrigger / ArtifactImportedTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `ArtifactUdi` | `string` | Full UDI string (e.g. `udi://document/abc123`) |
| `ArtifactType` | `string` | Entity type (e.g. `"document"`, `"media"`) |
| `ArtifactAlias` | `string` | Artifact alias |
| `ArtifactName` | `string` | Artifact display name |

---

### Nice-to-Have Triggers

| Trigger | Alias | Description |
|---------|-------|-------------|
| `FilesWrittenTrigger` | `umbracodeploy.filesWritten` | Fires when content files are written to disk |
| `FilesDeletedTrigger` | `umbracodeploy.filesDeleted` | Fires when content files are deleted from disk |
| `RemoteCompletedTrigger` | `umbracodeploy.remoteCompleted` | Fires when a remote deploy operation completes |
| `DiskTriggeredTrigger` | `umbracodeploy.diskTriggered` | Fires when a disk-triggered deploy completes |

#### FilesWrittenTrigger / FilesDeletedTrigger / UserUpdatedTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `UserEmail` | `string` | Email of the user who triggered the disk operation |
| `UserName` | `string` | Name of the user who triggered the disk operation |
| `FileCount` | `int` | Number of files written/deleted |

#### RemoteCompletedTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `WorkId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Type of the work item |
| `WorkItemEnvironment` | `string` | Target environment name |
| `WorkCount` | `int` | Total items before negotiation |
| `ProcessCount` | `int` | Total items after negotiation |
| `Duration` | `double` | Duration in seconds |

#### DiskTriggeredTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `string` | Result string: `"Succeeded"`, `"Failed"`, or `"Retry"` |
| `ExceptionType` | `string` | Exception type name if a failure occurred |

---

### Advanced Triggers

| Trigger | Alias | Description |
|---------|-------|-------------|
| `ArtifactExportingTrigger` | `umbracodeploy.artifactExporting` | Fires before an artifact is exported (pre-export) |
| `ArtifactImportingTrigger` | `umbracodeploy.artifactImporting` | Fires before an artifact is imported (pre-import) |
| `ValidateArtifactImportTrigger` | `umbracodeploy.validateArtifactImport` | Fires when artifacts are being validated before import |
| `UserUpdatedTrigger` | `umbracodeploy.userUpdated` | Fires when user-related deploy files are updated |
| `WorkContextPreparingTrigger` | `umbracodeploy.workContextPreparing` | Fires when a deploy work context is being prepared |

> **Cancelable notifications:** `ArtifactExportingTrigger`, `ArtifactImportingTrigger`, and `ValidateArtifactImportTrigger` are backed by cancelable Deploy notifications. However, cancellation is not available through the Automate trigger system — these triggers fire for observation only.

#### ArtifactExportingTrigger / ArtifactImportingTrigger Output

Same properties as the post-export/import triggers (`ArtifactUdi`, `ArtifactType`, `ArtifactAlias`, `ArtifactName`), but fires **before** the operation occurs.

#### ValidateArtifactImportTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `ArtifactCount` | `int` | Number of artifacts being validated |
| `ArtifactUdis` | `IEnumerable<string>` | UDI strings of all artifacts being validated |

#### WorkContextPreparingTrigger Output

| Property | Type | Description |
|----------|------|-------------|
| `WorkItemId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Work item type name |
| `OwnerName` | `string` | Name of the initiating user |
| `OwnerEmail` | `string` | Email of the initiating user |
| `EventTrigger` | `string` | What triggered this work item |

---

## Usage Examples

### Notify team on deployment failure

```
Trigger: Deployment Failed (umbracodeploy.taskFailed)
  → Condition: ExceptionType is not empty
  → Action: Send Slack message to #deployments
             "Deployment failed for {OwnerEmail}: {ExceptionMessage}"
```

### Log every imported document

```
Trigger: Content Imported (umbracodeploy.artifactImported)
  → Condition: ArtifactType == "document"
  → Action: Write to log
             "Imported document '{ArtifactName}' ({ArtifactUdi})"
```

### Alert on disk deploy completion

```
Trigger: Disk Deploy Completed (umbracodeploy.diskTriggered)
  → Condition: Result != "Succeeded"
  → Action: Send email to operations team
```

### React when a specific content type is deployed

```
Trigger: Content Imported (umbracodeploy.artifactImported)
  → Condition: ArtifactAlias starts with "news"
  → Action: Trigger cache invalidation
```

### Monitor deployment duration

```
Trigger: Deployment Succeeded (umbracodeploy.taskCompleted)
  → Condition: Duration > 60
  → Action: Log performance warning
             "Long deployment by {OwnerEmail}: {Duration}s, processed {ProcessCount} items"
```

---

## Future Triggers (Not Yet Implemented)

The following Umbraco Deploy notifications exist in the system and **could** be exposed as triggers in a future version:

| Notification | Description | Notes |
|-------------|-------------|-------|
| `TaskNotification` | Base notification for all task events | Not published directly; only subclasses are |
| `FilesWrittenNotification` (per-file variant) | Single-file variant of files written | Currently aggregated; could expose individual file paths |
| `FilesDeletedNotification` (per-file variant) | Single-file variant of files deleted | Same as above |

All currently available Deploy notifications are already implemented as triggers. The list above reflects edge cases and variants rather than missing coverage.

---

## Future Actions (Not Yet Implemented)

The following Deploy operations could be exposed as Automate actions in a future version:

| Action | Description |
|--------|-------------|
| Trigger Deployment | Programmatically initiate a Deploy task to a target environment |
| Export Artifact | Export a specific content item by UDI |
| Import Artifact | Import a specific artifact from a package |
| Restore from Disk | Trigger a disk-based restore operation |
| Validate Schema | Run schema validation against a target environment |
| Queue Deploy Task | Queue a deployment task for later execution |

---

## Architecture Notes

Umbraco Deploy notifications implement `INotification` (via `ObjectNotification<T>` and `EnumerableObjectNotification<T>`) and are published through the standard Umbraco CMS notification pipeline. This means:

- **No bridge handlers required** — unlike Umbraco.Engage.Automate or Umbraco.Commerce.Automate, Deploy events are already native CMS notifications
- **No IComponent registration** — triggers are discovered automatically by the Automate framework via the `[Trigger]` attribute
- **DeployAutomateComposer** is minimal — it implements `IComposer` only to ensure the assembly is initialized by Umbraco

---

## Requirements

- Umbraco CMS 17.x
- Umbraco Deploy 17.x
- Umbraco Automate 0.1.x or later
- .NET 10.0
