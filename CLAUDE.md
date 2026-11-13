# CLAUDE.md

Guidance for Claude Code (and other agentic tools) working in this repository.

## Architecture

Umbraco.Deploy.Automate is the integration package between **Umbraco Deploy** and **Umbraco Automate**. It is a satellite add-on — one of six `Umbraco.*.Automate` packages that each bolt Automate support onto a different Umbraco product line (this one: Deploy). It does three distinct jobs in one assembly:

1. **Deploy transport for Automate entities** — `IServiceConnector` implementations that carry Automate's four entity types between environments. On Deploy 19 they split in two: **automations and workspace groups are transfer types** (Queue for transfer, Restore, Partial restore, like content), and **workspaces and connections are disk types** (written to `.uda` files and restored from schema, never queued or partially restored). An entity type can't be both: Deploy 19's `ValidateDeployEntityTypesOptions` throws on the first read.
2. **Automate triggers backed by Deploy notifications** — `[Trigger]`-annotated classes that turn Deploy's CMS notifications (task completed/failed, artifact exported/imported, etc.) into automation-flow triggers.
3. **Automate actions that call Deploy** — `[Action]`-annotated classes that let an automation step kick off a Deploy content transfer, queue an item, or trigger a restore.

- **Target framework**: `net11.0` on `v19/dev` (`Directory.Build.props:3`), SDK pinned via `global.json` to `11.0.100` (`rollForward: latestFeature`). `main` (v18) and `support/17.x` stay on `net10.0`.
- **Requires** (`v19/dev`): .NET 11, Umbraco CMS 19 (currently `19.0.0-rc1`), Umbraco Deploy `[19.0.0-rc1, 19.999.999)`, Umbraco.Automate `[19.0.0-rc1, 19.999.999)`. `main` (v18) needs .NET 10, CMS 18.x, Deploy `[18.2.0, 18.999.999)` and Automate 18.0+.
- **Project SDK**: `Microsoft.NET.Sdk.Razor` (not the plain SDK) — this package ships static web assets (`wwwroot/`) under `App_Plugins/Umbraco.Deploy.Automate`, even though there's no Razor markup; the Razor SDK is what makes `StaticWebAssetBasePath` and the client-asset build targets work.
- **Nullable + implicit usings**: enabled repo-wide (`Directory.Build.props:8-9`).
- **No demo site** — unlike some sibling Automate satellites, this repo does not carry a `demo/` Umbraco instance. There's nothing to `dotnet run`; verification is unit tests plus manual testing against a real Umbraco Deploy + Automate install.

### Solution structure

```
src/Umbraco.Deploy.Automate/
  Actions/                          # IAction implementations that call Deploy
  Artifacts/                        # DeployArtifactBase<GuidUdi> subtypes (on-disk/serialized shape)
  Configuration/                    # DeployAutomateSettings (Umbraco:Deploy:Automate config section)
  Connectors/ServiceConnectors/     # One IServiceConnector (and ITreeServiceConnector) per Automate entity type
  Extensions/                       # UdiExtensions (internal)
  NotificationHandlers/             # Automate Saved/Deleted -> Deploy disk-refresher handlers
  Triggers/                         # [Trigger] classes wrapping Deploy notifications
  Triggers/Outputs/                 # POCO output shapes exposed to automation flows
  Workspaces/                       # WorkspaceGroupDeploySaver (bypasses interactive validators)
  DeployAutomateComposer.cs         # IComposer — DI wiring, disk/transfer entity type registration (DeployEntityTypesOptions), notification handlers
  DeployAutomateConstants.cs        # UDI entity type string constants
  DeployAutomateSchema.cs           # Root type used to generate the JSON config schema
tests/Umbraco.Deploy.Automate.Tests.Unit/
  Actions/                          # DeployEnvironmentActionTests (DeployContentAction, TriggerRestoreAction)
  Connectors/ServiceConnectors/     # Connector unit tests (dependency graph, pass logic)
  NotificationHandlers/             # DeployRefresherNotificationHandlerTests
  Triggers/                         # One test class per trigger + a cross-cutting convention test
  Workspaces/                       # WorkspaceGroupDeploySaver tests
```

### Key design pattern: `UmbracoAutomateEntityServiceConnectorBase<TArtifact, TEntity>`

Every entity connector (`src/Umbraco.Deploy.Automate/Connectors/ServiceConnectors/UmbracoAutomate*ServiceConnector.cs`) derives from this base (`UmbracoAutomateEntityServiceConnectorBase.cs`), which implements the UDI/range/selector plumbing that Deploy's `ServiceConnectorBase<TArtifact, GuidUdi, TEntity>` requires, so each concrete connector only needs to supply:

- `UdiEntityType`, `ProcessPasses`, `ValidOpenSelectors`, `OpenUdiName`
- `GetEntityAsync` / `GetEntitiesAsync` (read from the Automate service layer)
- `GetArtifactAsync` (entity → artifact, including dependency declarations)
- `ProcessAsync` (artifact → entity, on import/restore)
- `GetTreeLevelAsync` (the base implements `ITreeServiceConnector`; override it to list this type's partial-restore tree level)
- Optionally `GetChildUdisAsync` / `GetDescendantUdisAsync` for `this-and-children` / `descendants-of-this` selector support (only Workspace Group overrides these — see below)

Deploy assigns each connector an integer **process pass**; artifacts are grouped by pass and processed together, but *within* a pass, ordering follows package order, not dependency order. The pass numbers actually used here (not sequential, gaps are intentional headroom for content/schema/other connectors that share the same pipeline):

| Pass | Connector | Why |
|---|---|---|
| 2 | Connection | No Automate-side dependencies — can go early. |
| 3 | Workspace | Depends on Connections (pass 2) and `UserGroup`s. |
| 4, 5 | Workspace Group | **Two passes**: pass 4 creates/updates every group with `ParentId = null`; pass 5 re-parents, because a child group can appear before its parent within the same pass (`UmbracoAutomateWorkspaceGroupServiceConnector.cs:24-31`). |
| 6 | Automation | Depends on Workspace, optional Group, and every Connection referenced by a step. |

## Commands

### Build

```bash
dotnet restore Umbraco.Deploy.Automate.slnx
dotnet build Umbraco.Deploy.Automate.slnx --configuration Release
```

### Build for CI / package verification (no local Umbraco.Automate checkout)

`Umbraco.Deploy.Automate.csproj:20-31` defaults `UseProjectReferences=true`, which project-references a **local sibling checkout** of `Umbraco.Automate` at `..\..\..\Umbraco.Automate\Umbraco.Automate\src\Umbraco.Automate.Core\Umbraco.Automate.Core.csproj` (relative to `src/Umbraco.Deploy.Automate/`) *if that path exists*. This is for local dev convenience — edit Automate.Core and see the change immediately without publishing a package. To build against the NuGet package instead (what CI does, and what you must do if you don't have `Umbraco.Automate` cloned alongside this repo):

```bash
dotnet build Umbraco.Deploy.Automate.slnx -p:UseProjectReferences=false
dotnet pack Umbraco.Deploy.Automate.slnx --configuration Release -p:UseProjectReferences=false
```

Forgetting this flag when the sibling checkout isn't present is harmless (the `Exists()` condition falls back to the package reference automatically), but if the sibling checkout *is* present and stale/on the wrong branch, you'll silently build against the wrong Automate.Core version — always pass `-p:UseProjectReferences=false` when you want a clean, reproducible build. On `v19/dev` this matters more: a sibling checkout on a v18 branch would build against Automate 18 instead of the `[19.0.0-rc1, 19.999.999)` package.

### Test

```bash
dotnet test Umbraco.Deploy.Automate.slnx --configuration Release
```

162 unit tests (the most of any Automate satellite) — `xunit` + `Shouldly` + `Moq`, no integration test project. CI runs this on Windows, Linux, and macOS in a matrix for `main`, `release/*` and `hotfix/*` builds (Linux only for PRs and feature branches; the shared Umbraco.Build template's Test stage).

### Pack

```bash
dotnet pack Umbraco.Deploy.Automate.slnx --configuration Release -p:UseProjectReferences=false
```

Packing regenerates `src/Umbraco.Deploy.Automate/appsettings-schema.Umbraco.Deploy.Automate.json` from `DeployAutomateSchema.cs` via the `GenerateAppsettingsSchema` MSBuild target (`Umbraco.Deploy.Automate.csproj:53-60`) and copies `wwwroot/**` into the package's static web assets via `ClientAssetsBuild` (`Umbraco.Deploy.Automate.csproj:63-99`) — both run automatically on `Build`, no separate step needed.

### Versioning

Version comes from Nerdbank.GitVersioning (`version.json`), not the csproj. Current baseline: `19.0.0-rc1` on `v19/dev` (`18.0.1` on `main`). `publicReleaseRefSpec` only treats `main`, `hotfix/*`, and `release/*` as public releases — builds from any other branch (`v19/dev`, feature branches, `dev`, PR builds) get a non-release semver with a git-height suffix. On `v19/dev` that packs as `19.0.0--rc1.preview.<height>.g<sha>` (format from Umbraco.GitVersioning.Extensions; read `PackageVersion`, not NBGV's `NuGetPackageVersion`), which sorts below every shipped `-rcN`. A `release/*` build packs the bare `version.json` version (`19.0.0-rc1`).

### Environment setup

- .NET SDK 11.0.100+ on `v19/dev` (`global.json`; `main` and `support/17.x` pin 10.0.100)
- No database, no appsettings to configure, no user secrets — this is a library package, not a runnable app.
- `nuget.config` on `v19/dev`: nuget.org only. CMS, Licenses, Deploy and Automate 19 RCs are all on nuget.org, so there is no MyGet source. `<packageSourceMapping>` maps `Umbraco`, `Umbraco.*` and `*` to nuget.org, and its `<clear />` stops NuGet merging mappings from a parent checkout's `nuget.config` (a worktree under `.claude/worktrees` sits inside another line's checkout). `main` and `support/17.x` keep their own `nuget.config` files, so check the file on the branch you're on.

## Style Guide

Nothing unusual beyond standard .NET conventions — no custom `.editorconfig`, no unusual naming. Two patterns worth knowing before you add a new trigger or connector:

- **Trigger alias convention is enforced by a test, not just convention**: every `[Trigger]` alias must start with `"umbracoDeploy."` and be unique — see `tests/Umbraco.Deploy.Automate.Tests.Unit/Triggers/TriggerAliasConventionTests.cs`, which reflects over the assembly for every `TriggerAttribute`. Adding a trigger with a wrong-prefixed or duplicate alias fails CI immediately, not at runtime.
- **`UmbracoConstants` alias for `Umbraco.Cms.Core.Constants`**: triggers/actions that need `RequiredSections` alias the CMS constants type (`using UmbracoConstants = Umbraco.Cms.Core.Constants;`, e.g. `TaskCompletedTrigger.cs:5`) to avoid colliding with Automate's own `Constants` type used for `Constants.DeploySelector.*` elsewhere in the same file tree.

## Test Bench

- Tests live in `tests/Umbraco.Deploy.Automate.Tests.Unit/`, mirroring the `src/` folder layout (`Actions/`, `Connectors/ServiceConnectors/`, `NotificationHandlers/`, `Triggers/`, `Workspaces/`).
- Run: `dotnet test Umbraco.Deploy.Automate.slnx`
- Framework: xunit + Shouldly (`.ShouldBe()`/`.ShouldNotBeNull()` assertions) + Moq. `InternalsVisibleTo` grants the test project access to internal types (`Umbraco.Deploy.Automate.csproj:37-39`).
- **Coverage gap to be aware of**: `Artifacts/` has no unit tests, and `Actions/` has only `DeployEnvironmentActionTests` (DeployContentAction and TriggerRestoreAction; AddToQueueAction has none). If you touch an action or an artifact, consider whether it needs test coverage; don't assume existing patterns generalize to that folder.
- When adding a connector `ProcessAsync` pass, test both the "entity doesn't exist yet" (create) and "entity exists" (update) branches. Coverage is uneven today: `UmbracoAutomateConnectionServiceConnectorTests.cs` and `UmbracoAutomateWorkspaceServiceConnectorTests.cs` exercise both, but `UmbracoAutomateAutomationServiceConnectorTests.cs` has no update-path test and `UmbracoAutomateWorkspaceGroupServiceConnectorTests.cs` covers neither — don't assume an existing file already covers the branch you're changing.
- When adding a trigger, `TriggerAliasConventionTests` will automatically pick it up via reflection — no need to add a new convention test, just make sure the alias is `umbracoDeploy.*` and unique.

## Error Handling

Deploy import/restore failures use **fail-fast validation with a message naming the missing package**, not silent skips or generic exceptions — this is deliberate so an operator debugging a broken deploy immediately knows what to install rather than chasing a dangling reference at runtime:

- Unknown connection type alias → `InvalidOperationException` naming the connection and alias (`UmbracoAutomateConnectionServiceConnector.cs:116-121`).
- Unknown trigger alias on an automation → `InvalidOperationException` naming the automation and alias (`UmbracoAutomateAutomationServiceConnector.cs:189-194`).
- Unknown step alias → same treatment, but must check **both** `ActionCollection` and `ControlFlowCollection` before concluding a package is missing (`UmbracoAutomateAutomationServiceConnector.cs:196-209`) — see Edge Cases below, this was a real bug.
- Missing workspace on automation import → `InvalidOperationException` telling the operator to deploy the workspace first (`UmbracoAutomateAutomationServiceConnector.cs:154-158`).

`DeployContentAction.ExecuteAsync` (`Actions/DeployContentAction.cs`) is the one place with granular validation-error categorization — every failure path returns `ActionResult.Failed(exception, StepRunErrorCategory.X)` with a specific category (`Validation`, `ConfigurationError`, `ServiceUnavailable`) rather than a bare failure, so automation flows can branch on *why* the deploy didn't start.

## Clean Code

- **`UmbracoAutomateArtifactDependency`** (`UmbracoAutomateArtifactDependency.cs`) is a one-line subclass of Deploy's `ArtifactDependency` that hardcodes `checksumValidation: false`. Automate entities don't have Deploy-style checksums, so every dependency declared anywhere in this codebase should go through this type rather than raw `ArtifactDependency` — using the base type directly would silently enable checksum validation that doesn't apply here.
- **Sensitive-data stripping is layered, and the layer order matters** (`UmbracoAutomateConnectionServiceConnector.FilterSettings`, `Connectors/ServiceConnectors/UmbracoAutomateConnectionServiceConnector.cs:183-217`): an always-on, non-configurable strip of environment-specific fields runs before everything (see Edge Cases), then the schema-driven `IgnoreSensitive` strip runs first (feeds a smaller dict into the rest), then the explicit `IgnoreSettings` blocklist, then the value-driven `IgnoreEncrypted` (`ENC:` prefix) filter. If you touch this method, preserve the order — the doc comment above it explains why (fewer entries to walk, blocklist still "wins" by being applied last).
- **`ISensitiveSettingsStripper` is an external dependency**, not defined in this repo — it comes from `Umbraco.Automate.Core.Connections`/`Automations`. This package calls `StripConnectionSettings`, `StripTrigger`, `StripSteps` but does not own or test that logic; if sensitive-field stripping seems wrong, check `Umbraco.Automate.Core` first, not this repo.

## Teamwork and Workflow

**Repository**: GitHub, `umbraco/Umbraco.Deploy.Automate` (origin). Built in Azure DevOps under the "Umbraco Deploy" project (pipeline definition 731).

### Branch model

- `main` = current CMS major line (**v18**, `version.json` → `18.0.1`).
- `v19/dev` = the **v19** line while CMS 19 is in pre-release (`version.json` → `19.0.0-rc1`). Long-lived like `main` and `support/17.x`: pushes to it build but never publish. Each pre-release (`19.0.0-rc1`, `-rc2`, ...) is cut from `v19/dev` as `release/YYYY.MM.N`, published from that branch to MyGet and NuGet.org, then merged back into `v19/dev`. When CMS 19 is GA the package drops the `-rc` label and releases as `19.0.0`. At v19 GA, `main` is expected to become the v19 line (not decided yet).
- `support/17.x` = previous CMS major line (**v17**), checked out as a persistent git worktree at `.claude/worktrees/support-17.x` in this local clone — use that worktree for v17 work rather than switching branches in the primary checkout.
- `dev` exists as a branch but is not part of the documented release flow described below; treat `main`, `v19/dev` and `support/17.x` as the live lines.
- Feature branches observed in this repo: `feature/*`, `fix/*`, `chore/*` (e.g. `fix/control-flow-step-deploy-validation`, `fix/control-flow-step-deploy-validation-17x` — the same fix ported to both lines).
- No `.github/pull_request_template.md`, `CONTRIBUTING.md`, or `.editorconfig` exist in this repo — there is no written PR/commit convention document. Infer style from `git log`: commits follow a loose Conventional Commits pattern (`fix(connectors): ...`, `feat(deploy): ...`, `docs: ...`, `chore(release): ...`, `build(deps): ...`, `ci: ...`) and merge commits are `Merge pull request #N from umbraco/<branch>`.

### Release process

Releases are cut as `release/YYYY.MM.N` branches, where `N` is a **counter shared across all CMS lines** in a given month (e.g. `release/2026.07.1` for a v18 release and `release/2026.07.2` for a v17 release in the same month — not independent per-line counters).

1. **Cut the release branch** — bump the `Directory.Packages.props` Automate package ranges to `[X.0.0, X.999.999)` for the target major `X` (this makes NuGet resolve to the stable floor instead of a prerelease). Leave the Deploy range on its own floor (`[18.2.0, 18.999.999)` on v18: `ITreeServiceConnector` does not exist in 18.0; `[19.0.0-rc1, 19.999.999)` on v19). Bump `version.json` to the target stable version. For a v19 pre-release the skill cuts from `v19/dev`, keeps the Automate range on a pre-release floor that exists on nuget.org (`[19.0.0-rc1, 19.999.999)`) and `version.json` on a not-yet-published `-rcN` version; at v19 GA both move to the stable `19.0.0` floors. This repo has a project-local skill for it: `.claude/skills/release-management/SKILL.md` — read it for the exact steps rather than re-deriving them; it's intentionally a "light" version of the core Automate release-management skill (single product here, no changelog, no manifest).
2. **CI builds, tests and publishes.** `azure-pipelines.yml` triggers on push to `release/*` (and `main`, `dev`, `v19/dev`, `hotfix/*`, `feature/*`) and extends the shared Umbraco.Build template (see CI/CD summary). On `release/*`/`hotfix/*` it also runs the SBOM stage, then **Prerelease** pushes the package to MyGet (`MyGet - Pre-releases` service connection) and **Release** pushes it to NuGet.org (`NuGet`, the Umbraco.Build template default: the pipeline passes no `nuget_service_connection`) after a manual approval in the run. A v19 `-rcN` takes the same path, so it lands on both feeds; NuGet.org lists it as a pre-release.
3. **Approve the Release stage** ("Wait for manual approval") in the release branch's run to push to NuGet.org.
4. **After both publish stages are green**, run the repo's other project-local skill, `.claude/skills/post-release-cleanup/SKILL.md`. It:
   - merges the release branch into its own line (`main`, `support/17.x` or `v19/dev`; a v19 pre-release never goes to `main`) with `--no-ff`;
   - tags the merge commit `release-<version>` (e.g. `release-18.0.0`, `release-19.0.0-rc1`);
   - creates a GitHub Release with `gh release create <tag> --target <branch> --generate-notes`, adding `--prerelease` for an `-rcN` and `--latest=false` for a non-default line such as `support/17.x` (GitHub never marks a pre-release as Latest);
   - bumps `version.json` on the target branch (patch bump after a stable release, next `-rcN` after a pre-release);
   - deletes the release branch, local and remote, and never deletes `v19/dev`.

   The pipeline sets no `github_release_connection`, so it creates no GitHub Release itself.

Do not attempt to shortcut this by publishing from a feature branch or skipping the publish confirmation step baked into the cleanup skill — it explicitly stops and asks for confirmation before tagging an unpublished release.

### CI/CD summary

- `azure-pipelines.yml` extends `devops/templates/azure-pipelines-template.yml` from the shared `umbraco/Umbraco.Build` repo: `BuildAndPack` stage (restore, build, pack, publish `nupkg` + `build_output` artifacts; on agents the sibling Automate repo isn't there, so `UseProjectReferences` falls back to the `Umbraco.Automate.Core` package) → `Test` stage (downloads `build_output`, runs `dotnet test`, publishes VSTest results).
- `GenerateSBOM` stage on `release/*`/`hotfix/*` only: `umbraco-sbom` with a license policy check (a violation fails the run), uploaded to Dependency-Track as `Umbraco.Deploy.Automate v<major>`. Tick **Generate SBOM and check licenses** at queue time to check another branch without uploading.
- `Prerelease` (MyGet) and `Release` (NuGet.org, after approval) stages on `release/*`/`hotfix/*` only. Every other branch skips both — `main` is a public release in `version.json` too, so `azure-pipelines.yml` gates them by branch rather than leaving the template's `auto`. The queue-time **Prerelease to MyGet** / **Release to NuGet.org** parameters override this (e.g. `nightly` pushes a `main` build to the MyGet nightly feed).
- `v19/dev` builds (push trigger and PR target) but never publishes: it isn't a `release/*` or `hotfix/*` branch.
- PR trigger targets `main`, `dev` and `v19/dev`; `support/17.x` isn't listed.

## Edge Cases

- **Control-flow step aliases vs. action aliases** (fixed 2026-07-02, commit `09ee624`): Pass 6 automation validation (`UmbracoAutomateAutomationServiceConnector.cs:196-209`) originally checked every step's `ActionAlias` against `ActionCollection` only. Automations using built-in control flow (For Each / While / Parallel / Switch) resolve their step alias through a **separate** `ControlFlowCollection`, so they were being rejected with a misleading "package not installed" error even though nothing was missing. The fix mirrors the runtime's dual lookup (`actionCollection.GetByAlias(...) is null && controlFlowCollection.GetByAlias(...) is null`). **If you add any new validation that walks automation steps, remember steps can resolve through either collection — checking `ActionCollection` alone will reject valid control-flow steps.**
- **Cancelable Deploy notifications are observation-only here.** `ArtifactExportingTrigger`, `ArtifactImportingTrigger`, and `ValidateArtifactImportTrigger` are backed by cancelable Deploy notifications, but the Automate trigger system has no mechanism to cancel from within an automation flow — these triggers fire for observation only. Don't design a flow (or a new trigger) assuming a step can veto the underlying Deploy operation.
- **No dedicated "deployment started" event.** `TaskNotification` is a base class only — Deploy publishes `TaskCompletedNotification` or `TaskFailedNotification` as terminal events, never a start event. `WorkContextPreparingTrigger` (Advanced group) is the closest available signal to "deployment starting."
- **`Automation.Status` / `Automation.PublishedVersion` are deliberately not overwritten on update** (`UmbracoAutomateAutomationServiceConnector.cs:211-228`) — redeploying a live automation must not knock it back to Draft. New automations are always created as `Draft` (`UmbracoAutomateAutomationServiceConnector.cs:241`) so an operator must explicitly publish on the target after first deploy.
- **Environment-specific connection fields are never transferred** (`UmbracoAutomateConnectionServiceConnector.RemoveEnvironmentSpecificSettings`). OAuth-based connection types (e.g. Slack's `OAuthCredentialsId`) store a Guid pointing at a row in the OpenIddict credentials table of the environment where the user authenticated; that table is never deployed. Such fields — detected from the connection type's settings schema by `EditorUiAlias == "Umb.Automate.OAuth"` or property name `OAuthCredentialsId` (case-insensitive; property-name-only fallback when the type isn't registered) — are always stripped on export and dropped from incoming artifacts on import (older artifacts may still carry them). Update keeps the target's existing value via the settings merge; create leaves the field unset, so OAuth connections must be authenticated per environment. This is not config-gated — the old `IgnoreSettings: ["oAuthCredentialsId"]` workaround is now redundant. Same principle as `Workspace.ServiceAccountKey` (not applied on deploy, see `AutomateWorkspaceArtifact`). Detection deliberately avoids referencing `Umbraco.Automate.OpenIddict`; constants live in `DeployAutomateConstants.EnvironmentSpecificConnectionSettings`.
- **`WorkspaceGroupDeploySaver` intentionally bypasses interactive validators** (workspace existence, parent existence, unique-name checks) — see `Workspaces/WorkspaceGroupDeploySaver.cs` and the comment in `DeployAutomateComposer.cs:35-36`. It goes straight through `IWorkspaceGroupRepository` and publishes `Saving`/`Saved` notifications itself, deliberately skipping whatever the interactive back-office save path would enforce. This exists specifically for Deploy's pass 4/5 create-then-reparent flow, where an intermediate state (group with no parent yet) would fail a normal validator. Don't reuse it outside the deploy pipeline without checking whether those validators matter for the new caller.
- **Partial-restore trees come from the connectors, not from registration.** Deploy builds each tree level with the connector of the *parent's* entity type (`CurrentEnvironment.GetTreeItems`), so the Automations tree is spread over four connectors: Automation lists workspaces at the root (with children), Workspace lists its root-level groups then automations, Workspace Group lists its child groups then automations, Automation has no children. The Workspaces tree is the Workspace connector's root (flat, no children); Connections is the Connection connector's root. Automations are paged through `IAutomationService.GetAutomationsPagedAsync` and listed after the (in-memory) groups via `UmbracoAutomateTreeItems.PageWithTailAsync`. A filter searches descendants; the Automation connector's root filter skips groups (it has no group service). The transfer registrations in `DeployAutomateComposer.ConfigureEntityTypes` carry no tree detail.
- **Runtime entities are not transferable.** `AutomationRun` and `StepRun` (execution history) intentionally have no connector — history is per-environment and should never be added to the transfer pipeline.

## Agentic Workflow

- **Adding a new trigger**: add a `Triggers/<Name>Trigger.cs` deriving from `NotificationTriggerBase<TInput, TOutput, TNotification>`, add its output POCO to `Triggers/Outputs/`, give it an alias prefixed `umbracoDeploy.` (the convention test enforces this automatically — no extra step needed), and update the trigger table in `README.md`. Check whether the underlying Deploy notification is cancelable before deciding whether "observation only" framing applies.
- **Adding a new action**: add `Actions/<Name>Action.cs` deriving from `ActionBase<TSettings, TOutput>`, plus `<Name>Settings.cs` and `<Name>Output.cs`. Follow `DeployContentAction.cs`'s pattern of validating every precondition explicitly and returning `ActionResult.Failed(ex, StepRunErrorCategory.X)` with the most specific category available, rather than throwing — actions in this codebase do not throw on expected validation failures.
- **Adding a new connector** (only needed if Deploy or Automate introduces a new transferable entity type): derive from `UmbracoAutomateEntityServiceConnectorBase<TArtifact, TEntity>`, pick a process pass that respects the existing dependency order (Connections=2, Workspaces=3, Groups=4/5, Automations=6 — insert with room, don't renumber existing passes), and always wrap cross-entity references in `UmbracoAutomateArtifactDependency`, never the raw Deploy `ArtifactDependency`.
- **Before touching `FilterSettings` or the sensitive-stripping path**: re-read the three-layer precedence comment in `UmbracoAutomateConnectionServiceConnector.cs:162-169` first — this is exactly the kind of logic where "simplifying" the order silently changes what gets leaked into a committed deploy artifact.
- **Quality gate before opening a PR**: `dotnet build -p:UseProjectReferences=false` and `dotnet test` must both pass against the NuGet-referenced Automate/Deploy packages, not just against a local sibling checkout — CI always builds against the packages (agents have no sibling Automate checkout, so the project reference falls back), so a change that only works against your local Automate checkout will fail CI silently until you test it that way yourself.
- **Common pitfall**: assuming a step's `ActionAlias` only ever resolves through `ActionCollection`. Any new code that walks `Automation.Steps` and needs to know "is this alias real" must check `ControlFlowCollection` too (see Edge Cases).
- **When in doubt about release/branch mechanics**, read `.claude/skills/release-management/SKILL.md` and `.claude/skills/post-release-cleanup/SKILL.md` rather than guessing — they encode the exact, tested sequence for this repo, including the shared-counter release numbering and the publish confirmation before tagging.

## Project-Specific Notes

- **This package's job is narrow and split three ways** (see README.md for full detail): (1) Deploy transport for Automate's four user-facing entities (Workspace, Workspace Group, Automation, Connection), (2) Deploy-notification-backed Automate triggers, (3) Automate actions that call Deploy. Don't conflate these — a bug report about "deploy doesn't work" could mean any of the three and they have almost no code in common beyond shared config (`DeployAutomateSettings`) and constants.
- **External SDK dependency: `Umbraco.Deploy.Infrastructure`** (and `Umbraco.Deploy.Core` transitively) is the actual Deploy extensibility surface this package plugs into — `ServiceConnectorBase`, `IDiskEntityService`, `DeployEntityTypesOptions` (`AddTransferEntityType` / `AddDiskEntityType`), `ITreeServiceConnector`, `TreeItem`, etc. all come from there. This package has almost no Deploy logic of its own; it's an adapter translating Automate's domain model into Deploy's extensibility contracts.
- **External SDK dependency: `Umbraco.Automate.Core`** provides the domain model this package reads/writes (`IAutomateEntity`, `Connection`, `Workspace`, `WorkspaceGroup`, `Automation`, `ActionCollection`, `TriggerCollection`, `ControlFlowCollection`, `ISensitiveSettingsStripper`) plus the `[Trigger]`/`[Action]` attributes and base classes (`NotificationTriggerBase<>`, `ActionBase<,>`) that make triggers/actions auto-discoverable. **`UseProjectReferences=true` by default** means local dev against an in-progress Automate.Core change is one clone away (`../../../Umbraco.Automate/...` relative to this repo — i.e. sibling checkout, not submodule) — see Commands above.
- **`DeployAutomateComposer` is auto-discovered** by Umbraco's composition system (standard `IComposer` convention) — no manual registration is required by consumers, and this repo has no `ComponentComposer` wiring beyond what's in `DeployAutomateComposer.cs`.
- **No bridge/adapter notification handlers are needed for triggers**, unlike some sibling Automate satellites — the composer's XML doc comment (`DeployAutomateComposer.cs:18-23`) explicitly calls this out: Deploy notifications already implement `INotification` and publish directly through the standard Umbraco CMS pipeline, so `NotificationTriggerBase<>` can subscribe natively. If you're porting a pattern from `Umbraco.Engage.Automate` or `Umbraco.Commerce.Automate` that involves a bridge handler, it likely doesn't apply here.
- **Known limitation: only 3 of the 3 "have" actions exist; 5 more are speculative.** README's "Future work" table lists Export Artifact, Import Artifact, Restore from Disk, Validate Schema, Queue Deploy Task as *not yet implemented* — don't assume they exist or start implementing them without confirming the request, since they're explicitly aspirational.
- **Config schema is generated, not hand-maintained.** `src/Umbraco.Deploy.Automate/appsettings-schema.Umbraco.Deploy.Automate.json` is regenerated on every build from `DeployAutomateSchema.cs` (a marker type) via the `Umbraco.JsonSchema.Extensions` package (`Umbraco.Deploy.Automate.csproj:46-60`, pinned via `PackageVersion Update` at the bottom of the same file — note it's set twice, once centrally in `Directory.Packages.props:10` and once locally overriding in the csproj itself at lines 108-116, which is intentional: the csproj pin exists so `PrivateAssets`/`IncludeAssets` can be attached to that specific reference). Never hand-edit the generated JSON file; change `DeployAutomateSettings.cs`/`DeployAutomateSchema.cs` instead.
- **History**: this package used to be `Umbraco.Automate.Deploy`, living inside the `Umbraco.Automate` monorepo. It was extracted into its own repo so the whole Deploy⇄Automate integration (both directions: Automate-entities-through-Deploy AND Deploy-events-into-Automate) ships as a single installable package rather than being split across two repos.
- **This repo has 162 unit tests — the most of any Automate satellite** — but coverage is uneven: `Connectors/ServiceConnectors/`, `Triggers/`, `Workspaces/` and `NotificationHandlers/` are covered, `Actions/` only partly, and `Artifacts/` not at all. Weigh this when assessing risk of a change in those folders.
- **No secrets, no auth code, no SQL in this package** — it's a domain-translation adapter over two other products' SDKs. The only "security-adjacent" logic is the sensitive-settings stripping described above, which is about *not leaking connection secrets into deploy artifacts written to disk/source control*, not authentication/authorization.
