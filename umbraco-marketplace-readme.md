## Umbraco.Deploy.Automate

Umbraco Deploy triggers and actions for Umbraco Automate - react to deployment events and run Deploy operations from your automations.

### Features

- **13 Triggers** - React to deployment task, artifact export/import, file, and remote deploy events (e.g. Task Completed, Task Failed, Artifact Imported, Files Written)
- **3 Actions** - Deploy content to a target environment, add items to a user's transfer queue, and trigger restores from automation steps
- **Rich Trigger Outputs** - Work item IDs, owners, durations, artifact UDIs, and exception details
- **Zero Configuration** - Deploy publishes standard CMS notifications, so triggers subscribe natively; no further wiring required

Example: notify a channel when a deployment fails, or log every imported document.

### Requirements

- Umbraco CMS 19
- Umbraco Deploy 19
- Umbraco.Automate 19
- .NET 11.0

Upgrading from version 18? Automations and folders no longer use `.uda` files. See "Upgrading from version 18" in the [README](https://github.com/umbraco/Umbraco.Deploy.Automate/blob/main/README.md).
