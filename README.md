<p align="center">
  <img alt="Umbraco Automate" src="https://raw.githubusercontent.com/umbraco/Umbraco.Deploy.Automate/main/assets/logo-128.png" width="128">
</p>

# Umbraco.Deploy.Automate

Umbraco Deploy triggers and actions for [Umbraco Automate](https://umbraco.com/products/umbraco-automate/).

## Overview

Umbraco.Deploy.Automate is a provider package that connects [Umbraco Deploy](https://umbraco.com/products/umbraco-deploy/) to Umbraco Automate, exposing deployment events as first-class triggers and Deploy operations as actions in Automate flows — content exports and imports, task completions and failures, disk operations, and more.

## Key Features

- **13 triggers** — react to deployment task, artifact export/import, file, and remote deploy events
- **3 actions** — deploy content, add items to the transfer queue, and trigger restores from automation steps
- **Rich trigger outputs** — work item IDs, owners, durations, artifact UDIs, and exception details
- **Native notifications** — Deploy publishes standard CMS notifications, so no bridge handlers are required
- **Zero configuration** — `DeployAutomateComposer` self-registers with Umbraco's composition pipeline

## Installation

```bash
dotnet add package Umbraco.Deploy.Automate
```

No further wiring is required — the composer is auto-discovered by Umbraco's composition system.

## Requirements

- .NET 10.0
- Umbraco CMS 17.x
- Umbraco Deploy 17.x
- Umbraco.Automate 17.0+

## Triggers

Fire an Automate flow when something happens in Deploy.

**Deployment tasks**

| Trigger | Fires when… |
|---|---|
| Task Completed | A deployment task completes successfully |
| Task Failed | A deployment task fails |
| Remote Completed | A remote deploy operation completes |
| Disk Triggered | A disk-triggered deploy completes |
| Work Context Preparing | A deploy work context is being prepared (closest equivalent to "deployment starting") |

**Artifacts**

| Trigger | Fires when… |
|---|---|
| Artifact Exported | A content artifact has been exported |
| Artifact Imported | A content artifact has been imported |
| Artifact Exporting | An artifact is about to be exported (pre-export) |
| Artifact Importing | An artifact is about to be imported (pre-import) |
| Validate Artifact Import | Artifacts are being validated before import |

**Files & users**

| Trigger | Fires when… |
|---|---|
| Files Written | Content files are written to disk |
| Files Deleted | Content files are deleted from disk |
| User Updated | User-related deploy files are updated |

> **Cancelable notifications:** Artifact Exporting, Artifact Importing, and Validate Artifact Import are backed by cancelable Deploy notifications. Cancellation is not available through the Automate trigger system — these triggers fire for observation only.

## Actions

Run Deploy operations from an Automate flow.

| Action | What it does |
|---|---|
| Deploy Content | Starts a deploy of the specified content to a target environment |
| Add To Queue | Adds a content item to a backoffice user's Deploy transfer queue |
| Trigger Restore | Pulls and restores all deployable content from a source environment |

## Trigger Outputs

### Task Completed

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

### Task Failed

Same as Task Completed (minus counts), plus:

| Property | Type | Description |
|---|---|---|
| `ExceptionMessage` | `string?` | Exception message if an error occurred |
| `ExceptionType` | `string?` | Exception type name if an error occurred |

### Artifact Exported / Imported / Exporting / Importing

| Property | Type | Description |
|---|---|---|
| `ArtifactUdi` | `string` | Full UDI string (e.g. `udi://document/abc123`) |
| `ArtifactType` | `string` | Entity type (e.g. `"document"`, `"media"`) |
| `ArtifactAlias` | `string` | Artifact alias |
| `ArtifactName` | `string` | Artifact display name |

### Files Written / Files Deleted / User Updated

| Property | Type | Description |
|---|---|---|
| `UserEmail` | `string` | Email of the user who triggered the disk operation |
| `UserName` | `string` | Name of the user who triggered the disk operation |
| `FileCount` | `int` | Number of files written/deleted |

### Remote Completed

| Property | Type | Description |
|---|---|---|
| `WorkId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Type of the work item |
| `WorkItemEnvironment` | `string` | Target environment name |
| `WorkCount` | `int` | Total items before negotiation |
| `ProcessCount` | `int` | Total items after negotiation |
| `Duration` | `double` | Duration in seconds |

### Disk Triggered

| Property | Type | Description |
|---|---|---|
| `Result` | `string` | Result string: `"Succeeded"`, `"Failed"`, or `"Retry"` |
| `ExceptionType` | `string` | Exception type name if a failure occurred |

### Validate Artifact Import

| Property | Type | Description |
|---|---|---|
| `ArtifactCount` | `int` | Number of artifacts being validated |
| `ArtifactUdis` | `IEnumerable<string>` | UDI strings of all artifacts being validated |

### Work Context Preparing

| Property | Type | Description |
|---|---|---|
| `WorkItemId` | `Guid` | Work item identifier |
| `WorkItemType` | `string` | Work item type name |
| `OwnerName` | `string` | Name of the initiating user |
| `OwnerEmail` | `string` | Email of the initiating user |
| `EventTrigger` | `string` | What triggered this work item |

## Usage Examples

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

## How It Works

Umbraco Deploy publishes notifications directly through Umbraco CMS's standard notification pipeline (as `INotification`), so triggers subscribe to Deploy notifications natively — no bridge handlers are required. `DeployAutomateComposer` is minimal and exists only to ensure the assembly is discovered by Umbraco.

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

### Project layout

```
src/
  Umbraco.Deploy.Automate/           # Package source
    Actions/                         # Automate actions
    Triggers/                        # Automate triggers
    Triggers/Outputs/                # Trigger output types
tests/
  Umbraco.Deploy.Automate.Tests.Unit/
```

## License

MIT — see [LICENSE](LICENSE) for details.
