# Pull request review: Aspire Fusion publishing

## Review scope and recommendation

This review compares the current branch with `origin/main` at
`a6f031161cced2d6b3bd6245148000064b26a38b`.

The pull request adds a substantial Aspire integration for producing Fusion source artifacts,
uploading immutable source versions to Nitro, deploying source services, composing a stage-specific
Fusion archive in memory, publishing that archive to Nitro, and then deploying the gateway. It also
adds command-line and endpoint-based schema acquisition for publishing.

**Recommendation: request changes before merging.** The overall design is thoughtful, especially
the immutable tag model, exact-version preflight, digest checks, bounded downloads, deployment-step
ordering, and uncertain-state handling. There are, however, three release-blocking behavior issues
and a number of lower-severity correctness, API, and documentation problems.

One of the blockers (the command-line export settings file) means that acquisition mode cannot
complete a publish at all. The full Aspire test suite is green, which is misleading: every blocker
below sits on a path that no test exercises.

## Findings

### P1: Command-line schema export always writes a throwaway settings file with a loopback URL

Location:
[CommandLineSchemaExporter.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/CommandLineSchemaExporter.cs#L50-L51)

The exporter points `--output` at `<fresh temp dir>/schema.graphqls`. `SchemaFileExporter` derives
the settings path from that
([SchemaFileExporter.cs](../src/HotChocolate/Core/src/Types/Execution/Internal/SchemaFileExporter.cs#L46-L135))
and only updates a settings file that already exists there. The export directory is always fresh:
`FusionPipelineExecutor.cs:741` builds it under the temporary directory and deletes it at `:756`,
and `SchemaComposition.cs` uses a per-run directory. The exporter therefore always falls through to
`CreateNewSettingsFile`, which writes the hardcoded template
`{ "name": ..., "transports": { "http": { "url": "http://localhost:5000/graphql" } } }`. The
project's own committed `schema-settings.json`, with real URLs and its `environments` block, is
never read.

Failure scenario: declare a source with `WithGraphQLSchemaExport(...)` and run
`aspire do fusion-upload`. `FusionPipelineExecutor` resolves those settings per stage and calls
`RejectLoopbackEndpoint` (`:1338`), which throws "Fusion deployment settings must not contain a
loopback production endpoint." This is deterministic for every command-line-exported source.
`WithCompositionEnvironment` also has nothing to resolve, because the generated settings carry no
`environments` block.

Note that the endpoint-acquisition path does read the project's `schema-settings.json` before
exporting. The command-line export path does not.

### P1: A claimed deployment slot leaks on every failure path but one

Location:
[FusionDeploymentWorkflow.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/FusionDeploymentWorkflow.cs#L274-L333)

`ClaimPublishAsync` succeeds at `:274`, and the only `ReleasePublishAsync` call in the entire
codebase is reached from `:299`, when validation returns a clean `ValidationFailed` event with
`Force: false`. There is no `try`/`finally` around the claim, and the `catch` at `:326` handles
`OperationCanceledException` and rethrows without releasing.

Failure scenario: CI publishes, the commit mutation at `:310` hits a transport error, the process
exits, and `startFusionConfigurationComposition` is still held on that request. The next pipeline
run against the same stage cannot claim until the server expires it. The same hole exists for
validate-command failure (`:284`), `WaitForValidationAsync` throwing (`:293`),
`WaitForTerminalPublishAsync` throwing (`:319`), and operation or approval timeout (`:326`).

### P1: A Nitro publish target without stages makes upload and publish succeed without doing work

Location:
[FusionPipeline.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipeline.cs#L38-L51)

`SelectTargets` filters out every `NitroPublishTargetResource` that has no `FusionStageResource`
before declaration validation. The pipeline steps are still registered, but all their actions see
zero targets or zero selected deployments and return successfully. Consequently, an AppHost that
calls `AddNitroPublishTarget` but accidentally omits `AddStage` can report successful results for
both `aspire do fusion-upload` and `aspire do fusion-publish` while uploading and publishing
nothing.

That is especially risky in non-interactive CI because the command itself looks healthy. Once a
publish target exists, the declaration should fail if it has no stages. Add a test that executes or
plans both public roots with a stage-less target and expects a clear configuration error.

### P2: The documented Nitro CLI credential fallback is not implemented for publishing

Locations:
[NitroExtensions.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/NitroExtensions.cs#L273-L285),
[FusionPipelineExecutor.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipelineExecutor.cs#L1299-L1318)

The public XML documentation and `FUSION_PUBLISHING.md` say that omitting `WithNitroApiKey` falls
back to `NITRO_API_KEY` or the Nitro CLI session. The publishing path actually checks only the
configured parameter, `Nitro:ApiKey`, and `NITRO_API_KEY`, then fails with "requires an API key."
It never invokes `NitroConnectionResolver`, reads a CLI session, or constructs an access-token
credential. `NitroFusionApi` also always creates an API-key credential from `FusionTarget.ApiKey`.

Either integrate the existing Nitro connection/session resolver into publication, or remove the
CLI-session promise from all public documentation. Until this is fixed, users must provide an API
key explicitly.

### P2: File-source name documentation contradicts the new validation behavior

Locations:
[NitroExtensions.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/NitroExtensions.cs#L48-L60),
[SchemaComposition.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/SchemaComposition.cs#L1248-L1287)

The `AddNitro` remarks say that a file-based source name is not checked against its settings file.
This pull request now routes file sources through `ReadEndpointConfiguration`, which requires the
configured or resource-derived name to exactly match `schema-settings.json`. The publishing
artifact path also validates the exact name.

The stricter behavior is reasonable and makes local composition agree with publishing, but the XML
documentation should state that all acquisition modes require an exact settings-name match.

### P2: Canonical-content hashing returns schema and settings bytes to the pool without clearing

Location:
[FusionArchiveContent.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/FusionArchiveContent.cs#L158-L199)

The canonical hash encoder rents a byte array, fills it with normalized schema, extension, and
settings text, and returns it with `clearArray: false`. Schema settings can include
environment-specific URLs or other sensitive configuration, so this buffer should be returned with
`clearArray: true`.

### P2: Steps that resolve parameters can run before `process-parameters`

Location:
[FusionPipeline.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipeline.cs#L147-L170)

`fusion-download` has no dependencies at all, and `fusion-upload` depends only on
`fusion-artifacts`. Both resolve `ParameterResource` values: `SelectStagesAsync` reads
`target.StageParameter.GetValueAsync` (`FusionPipeline.cs:67`), `ResolveConfigurationTagAsync` reads
`ConfigurationTagParameter.GetValueAsync` (`FusionPipelineExecutor.cs:1379`), and
`ResolveTargetAsync` reads `target.ApiKey.GetValueAsync`. The built-in `process-parameters` step is
only `RequiredBy` the deploy, build, and publish prerequisite steps, none of which these steps
depend on.

Before `ParameterProcessor.InitializeParametersAsync` runs, `ParameterResource.WaitForValueTcs` is
null and `GetValueAsync` falls through to the eager `_lazyValue`, which throws
`MissingParameterValueException` for any parameter without a configured value. An AppHost whose
`nitroApiKey`, `tag`, or `stage` parameters are supplied interactively fails at pipeline start
instead of being prompted. These steps should depend on `WellKnownPipelineSteps.ProcessParameters`.

### P2: The export timeout does not bound the whole operation

Location:
[CommandLineSchemaExporter.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/CommandLineSchemaExporter.cs#L219)

`WaitForExitAsync` uses `linkedSource.Token` (timeout plus caller token), but the following
`await Task.WhenAll(stdout, stderr)` uses `outputSource`, which is only cancelled inside
`TerminateProcessAsync`, never on the success path. The `finally` also skips termination once
`HasExited` is true.

Failure scenario: `dotnet run` exits 0, but a descendant that inherited the stdout/stderr pipe
write-end is still alive (an MSBuild node-reuse worker, or anything the app spawned). The reads
never see EOF and `ExportAsync` blocks indefinitely past `ExportTimeout`, stalling the
`BeforeResourceStartedEvent` handler or the publish pipeline step.

### P2: A failed export captures child output at cost and then discards it

Location:
[CommandLineSchemaExporter.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/CommandLineSchemaExporter.cs#L69-L75)

`RunProcessAsync` retains up to 32 KB of stdout, and `ExportAsync` throws "...exited with code {n}.
Child-process output was suppressed..." without ever using `processResult.StandardOutput`. When the
user's schema fails to build, the Aspire user sees only an exit code and must reproduce the exact
command by hand. The stdout of `dotnet run -- schema export` is CLI and build diagnostics, not
secrets, and stderr is already deliberately count-only.

### P2: The export environment allow-list omits variables the .NET CLI needs on Windows

Location:
[CommandLineSchemaExporter.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/CommandLineSchemaExporter.cs#L239-L289)

`APPDATA`, `ProgramFiles`, `ProgramFiles(x86)`, `ProgramData`, `PATHEXT`, `COMSPEC`, and `windir`
are all dropped. On Windows the implicit restore in `dotnet run` cannot find
`%APPDATA%\NuGet\NuGet.Config`, so a project restoring from an authenticated private feed fails.
Combined with the discarded child output above, the user sees only "exited with code 1".

### P2: The request id needed to recover from a stranded claim never reaches the operator

Location:
[FusionDeploymentWorkflow.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/FusionDeploymentWorkflow.cs#L329-L333)

`FusionIndeterminateStateException.RequestId` is set on every indeterminate throw, but it is never
interpolated into `Message`, and no production caller reads the property (the only reads in the
repository are in tests). An operator hit by the leaked publication slot sees "The Fusion
publication timed out before a terminal result could be verified." with no identifier, and cannot
release the slot. These two findings compound: the first strands state, the second removes the
means to fix it.

### P2: Deterministic remote rejections are reported as indeterminate state

Location:
[FusionDeploymentWorkflow.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/FusionDeploymentWorkflow.cs#L586-L592)

The `!result.Succeeded` branch of `EnsureRemoteCommandSucceededAsync` throws
`FusionIndeterminateStateException` even though Nitro answered successfully and explicitly said no.
When two deployers target the same stage, the second's claim is rejected with a definite "already
being composed" error, and the user is told the state could not be verified and may need manual
reconciliation, when nothing changed remotely and a plain retry is safe. Only the
`catch (Exception)` branch at `:577` is genuinely indeterminate.

### P2: A `FusionPipelineSession` is leaked on every `ResolveStepsAsync`

Locations:
[FusionPipeline.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipeline.cs#L124-L129),
[FusionPipelineSession.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipelineSession.cs#L21)

`CreateSteps` constructs a session that registers a `CancellationTokenRegistration` on the pipeline
token, but only the session belonging to the resolution whose `fusion-publish` step actually
executes is disposed. In publish mode, `ExecuteBeforeStartHooksAsync` runs
`pipeline.Clone().ExecuteStepSequentiallyAsync("before-start", ...)`, which resolves steps once, and
the real `ExecuteAsync` resolves again. The before-start session is never disposed and its
cancellation registration lives for the process lifetime. It also means the captured
`FusionPipelineTopology` is mutated by two independent resolutions, idempotent today only because
`ConfigureSteps` calls `ResourcesWithoutCompute.Clear()`.

### P3: Lower-severity findings

- [FusionPipeline.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipeline.cs#L147-L170):
  nothing in the step graph encodes that `fusion-upload` must precede `fusion-download`.
  `fusion-upload` declares `DependsOnSteps = [fusion-artifacts]` and no `RequiredBySteps`, and
  `fusion-download` declares neither. This is harmless in the documented split-CI flow, because
  `aspire do fusion-upload` schedules upload plus `fusion-artifacts` without scheduling download,
  and `aspire do fusion-publish` schedules the download chain without scheduling upload. The two
  commands run in separate jobs on separate machines, where a graph edge could not express the
  ordering anyway, and `PreflightAsync` fails loudly with `Fusion source 'products' version
'release-1' does not exist on target 'products'` if the upload job was skipped. It would only
  matter if a single invocation ever scheduled both roots, in which case they would start
  concurrently and preflight could fail spuriously against an in-flight upload. Worth an explicit
  precondition or a comment recording that the ordering is intentionally external.
- [FusionDeploymentWorkflow.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/FusionDeploymentWorkflow.cs#L459-L466):
  `WaitForReadyAsync` throws on `InProgress`, while `WaitForValidationAsync` and
  `WaitForTerminalPublishAsync` both continue on it. If Nitro replays `OperationInProgress` as the
  task's current state on subscribe, the publication aborts as indeterminate before anything is
  claimed.
- [NitroFusionApi.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/NitroFusionApi.cs#L461-L478):
  `ReadRemoteEvent` collapses an unrecognized `__typename` to `Unknown` and discards the raw name,
  so a newer Nitro server state produces an undiagnosable error. Relatedly,
  `Operations/WatchFusionDeployment.graphql` selects `state`, which `ReadRemoteEvent` never reads.
- [NitroGraphQL.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/NitroGraphQL.cs#L360-L387):
  a declared `Content-Length` over the limit returns `NitroGraphQLResult.Failed`, but a chunked
  response over the same limit throws `NitroResponseTooLargeException`, which is neither
  `IOException` nor `HttpRequestException` and so escapes both `SendWithRetryAsync` and
  `NitroFusionApi.SendAsync` uncaught. Its message is hardcoded to "schema validation response"
  although the path now serves every deployment mutation.
- [NitroFusionApi.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/Nitro/NitroFusionApi.cs#L488-L514):
  `ToCommandResult` derives `Succeeded` from counting errors that have a string `message`. If
  `commitFusionConfigurationPublish` returns an error type outside the `Error` interface, zero
  messages are collected and `PublishAsync` returns normally on a publication that never committed.
- [FusionPipelineExecutor.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipelineExecutor.cs#L427):
  `ComposeAsync` composes into a `MemoryStream` and calls `ToArray()`. The stream's internal buffer
  and prior growth buffers hold a full unzeroed copy of the composed configuration after
  `TransferComposition` takes ownership, which breaks the guarantee that the `Array.Clear` hygiene
  and the `Publish_Should_ClearSessionBuffers_*` tests assert.
- [FusionPipelineExecutor.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/FusionPipelineExecutor.cs#L796-L819):
  if `Directory.Move` fails after partially creating the destination, the catch guard
  `!Directory.Exists(destinationDirectory)` is false so the backup is not restored, yet the
  `finally` still sees `movedDestination == true` and deletes the backup. Previous deployment
  artifacts are permanently lost and the destination is left partial.
- [GraphQLResourceModel.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/GraphQLResourceModel.cs#L55),
  [GraphQLResourceBuilderExtensions.cs](../src/HotChocolate/Fusion/src/Fusion.Aspire/GraphQLResourceBuilderExtensions.cs#L112):
  `WithGraphQLSchemaExport` adds a `GraphQLSourceSchemaAnnotation` without
  `ResourceAnnotationMutationBehavior.Replace` (contrast `WithNitroApiId`), so
  `WithGraphQLSchemaFile().WithGraphQLSchemaExport(...)` on the same project yields "Sequence
  contains more than one matching element" with no resource name.

## Test quality and coverage

The Aspire suite is large and green, but coverage sits almost entirely on happy paths, and every
blocker above lives in an untested gap.

**Repository convention violations:**

- [FusionReleaseAcceptanceTests.cs](../src/HotChocolate/Fusion/test/Fusion.Aspire.Tests/FusionReleaseAcceptanceTests.cs#L27-L211):
  `Release_Should_UploadOnceAndPublishFromNitroAcrossRunners` has roughly 20 `Assert.*` calls in one
  185-line method against the repository's hard limit of 5, mixing an inline snapshot with a 36-line
  `Assert.Collection`. A regression anywhere in the chain surfaces as one opaque failure.
- Missing `// arrange` / `// act` / `// assert` markers across most new test files, including
  `FusionPipelineTests.cs` (inconsistent within the file itself: roughly 6 methods have them and 16
  do not), `NitroFusionApiTests.cs`, `CommandLineSchemaExporterTests.cs`,
  `GraphQLResourceModelTests.cs`, and `GraphQLResourceBuilderExtensionsTests.cs`.
- [CommandLineSchemaExporterTests.cs](../src/HotChocolate/Fusion/test/Fusion.Aspire.Tests/CommandLineSchemaExporterTests.cs#L176):
  `ValidateArtifactsAsync_Should_AcceptExactNameAndValidSchema` asserts nothing at all.

**Portability and flakiness:**

- [GraphQLResourceModelTests.cs](../src/HotChocolate/Fusion/test/Fusion.Aspire.Tests/GraphQLResourceModelTests.cs#L108-L114):
  the inline snapshot hardcodes `/` separators (`schemas/products.graphqls`), but
  `Path.GetRelativePath` returns backslashes on Windows, so this fails on a Windows agent.

**Uncovered risky paths:** the failure
and rollback half of the deployment state machine (claim failure, validate failure, commit failure,
operation timeout, release itself failing, `PublishingFailed` after commit);
`ReplaceDirectoryAtomically`'s rollback path; `VerifyReadinessAsync` rejecting a loopback endpoint
from a composed archive or clearing the session when readiness fails; `PublishAsync` failing on the
second of multiple targets after the first has committed; and, in the exporter, the timeout branch,
the incomplete-declaration guard, the non-zero-exit message, and the missing-artifact branch.

## Documentation placement

Three markdown files were added under `src/HotChocolate/Fusion/src/Fusion.Aspire/`, roughly 1,200
lines in total, and `git diff origin/main...HEAD -- website/` is **empty**. The entire publish and
deploy feature ships with no changes to `website/content/docs/fusion/`, so the only user-facing
description of `fusion-upload` and `fusion-publish` lives inside a NuGet package's source folder.

- `ASPIRE_PUBLISH_AND_DEPLOY.md` (637 lines) is a dated third-party research report. It opens with
  "Research date: 2026-07-30. Version reviewed: Aspire 13.4.6" and closes with a "Primary official
  sources" link list. It contains no Fusion content, only a summary of Microsoft's `aspire publish`,
  `deploy`, and `do` documentation, package prerelease status, Azure auth, and Docker/K8s/ACA/AKS
  walkthroughs. It has a hard expiry and no owner, and does not belong in `src/`.
- `FUSION_PUBLISH_DEPLOY_DESIGN.md` (325 lines) is a plan rather than documentation, written in the
  imperative to a future implementer ("Keep those APIs behind the small local pipeline adapter") and
  ending in a verification matrix of acceptance criteria. It documents target selection by
  "environment" in places where the implementation selects by **stage** via `WithStageParameter` and
  `Parameters__stage`, while its own AppHost sample already uses `AddStage`.
- `FUSION_PUBLISHING.md` (204 lines) is the only user-facing document, and it is accurate.
  `AddNitroPublishTarget`, `WithNitroCloudUrl`, `WithNitroApiKey`, `WithStageParameter`,
  `WithConfigurationTag`, `AddStage`, `WithApproval`, `WithForce`, and `WithCompositionEnvironment`
  all exist in `NitroExtensions.cs`; the step names match `FusionPipeline.cs:14-20`; and the
  128,000,000-byte per-source limit matches `FusionPipelineMemoryLimits.cs`.

The two documents also duplicate the AppHost sample, pipeline graph, CI YAML, and retry rules almost
verbatim, with only the design document's copy stale. Recommendation: promote `FUSION_PUBLISHING.md`
into `website/content/docs/fusion/` and delete or relocate the other two, subject to the credential
correction above.

## What the new integration does

The integration defines two related but distinct workflows:

1. **Local run composition.** Existing `AddNitro(stage)` support can seed a locally running gateway
   from a Nitro stage, combine that seed with locally referenced source schemas, and follow stage
   updates during the AppHost run.
2. **Release publishing.** The new `AddNitroPublishTarget` resource contributes named Aspire
   pipeline steps for creating source artifacts, reconciling immutable source versions, deploying
   workloads, composing a production FAR, and publishing it to a selected Nitro stage.

The release workflow deliberately uses Nitro as the handoff between jobs. The build job and deploy
job must evaluate the same AppHost revision and use the same immutable tag, but they do not exchange
a filesystem artifact.

### Pipeline shape

The two jobs below are ordered by CI, not by the step graph. `aspire do fusion-upload` schedules
only the upload branch and `aspire do fusion-publish` only the download branch, so no edge links
them.

```text
Build or upload job

fusion-artifacts
  -> acquire schema and settings from files or an explicit command-line export
  -> validate names, SDL, settings, environment bindings, and production URLs
  -> create portable source artifacts
  -> fusion-upload
  -> reconcile each immutable name@tag source version in Nitro

Deployment job

fusion-download
  -> verify every exact name@tag exists before provider compute changes
  -> deploy source compute
  -> download the exact versions again
  -> reject canonical digest changes
  -> fusion-compose using the selected environment and Nitro stage settings
  -> verify production endpoints are ready
  -> publish and commit the FAR to Nitro
  -> deploy gateway compute
  -> fusion-publish terminal
```

`aspire publish` remains an artifact-producing operation. It requires `fusion-artifacts`, but it
does not resolve the Nitro credential or mutate Nitro. `aspire deploy` includes the terminal
`fusion-publish` step in addition to the selected provider's deployment work. The lower-level
`aspire do fusion-upload` and `aspire do fusion-publish` commands are the intended split-CI surface.

## How to configure it

### 1. Declare publishable source schemas and a gateway

Each source must be referenced by the single gateway resource that has
`WithGraphQLSchemaComposition`. Publishing supports a checked-in schema file, the explicit Hot
Chocolate command-line exporter, or schema acquisition from a declared endpoint.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var products = builder
    .AddProject<Projects.Products>("products")
    .WithGraphQLSchemaFile(
        fileName: "schema.graphqls",
        sourceSchemaName: "products");

var reviews = builder
    .AddProject<Projects.Reviews>("reviews")
    .WithGraphQLSchemaExport(
        schemaName: "reviews",
        configuration: "Release",
        targetFramework: "net10.0",
        runtimeIdentifier: null,
        timeout: TimeSpan.FromMinutes(5));

var inventory = builder
    .AddProject<Projects.Inventory>("inventory")
    .WithHttpEndpoint(port: 5080, targetPort: 5080, name: "http")
    .WithGraphQLSchemaEndpoint();

var gateway = builder
    .AddProject<Projects.Gateway>("gateway")
    .WithReference(products)
    .WithReference(reviews)
    .WithReference(inventory)
    .WithGraphQLSchemaComposition(
        new GraphQLCompositionSettings
        {
            EnvironmentName = "Production"
        });
```

For a file source named `schema.graphqls`, place these files together in the source project:

```text
schema.graphqls
schema-settings.json
schema-extensions.graphqls   # optional
```

The effective source name must be unique, safe as a portable path segment, and exactly match the
`name` property in `schema-settings.json` with the current implementation.

The command-line export runs the source project with `dotnet run`, an exact configuration and
target framework, no launch profile, and a small environment allowlist. Projects whose schema
startup requires arbitrary environment variables should either make schema export independent of
those values or use checked-in schema artifacts. With the code as reviewed, this acquisition mode
cannot complete a publish because the generated settings file always carries a loopback URL; see the
findings above.

Endpoint acquisition reads `schema-settings.json` from the source project and downloads the schema
using the configured native or Apollo Federation protocol. The endpoint must already be reachable
from the artifact runner because the publishing pipeline does not start source resources. It must
also use a fixed target port because Aspire does not allocate dynamic ports in publish mode.

### 2. Add a provider deployment environment

The source services and gateway must be attached to an Aspire deployment integration that
contributes `DeployCompute` steps. Examples include Docker Compose, Kubernetes, AKS, Azure
Container Apps, or App Service, depending on the packages and deployment model used by the
AppHost. If the integration cannot prove a deploy-compute step for every source and for the
gateway, `fusion-readiness` fails closed.

This pull request does not copy the composed FAR into the gateway deployment. It publishes the FAR
to Nitro before gateway compute is deployed. The deployed gateway therefore needs its normal
runtime configuration to select the same Nitro API and stage.

### 3. Declare the Nitro publish target and all allowed stages

```csharp
var stage = builder.AddParameter("stage");
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder
    .AddNitroPublishTarget("nitro")
    .WithNitroCloudUrl("https://api.chillicream.com")
    .WithNitroApiId("products-fusion")
    .WithNitroApiKey(nitroApiKey)
    .WithStageParameter(stage)
    .WithConfigurationTag(tag);

nitro.AddStage("development")
    .WithCompositionEnvironment("Development")
    .WithApproval(waitForApproval: false);

nitro.AddStage("production")
    .WithCompositionEnvironment("Production")
    .WithApproval(waitForApproval: true)
    .WithForce(false);

builder.Build().Run();
```

The cloud URL and API ID can instead come from `Nitro:CloudUrl` and `Nitro:ApiId`, or from
`NITRO_CLOUD_URL` and `NITRO_API_ID`. With the code as reviewed, the credential must come from the
secret parameter, `Nitro:ApiKey`, or `NITRO_API_KEY`; the documented CLI-session fallback does not
currently work, and interactively supplied parameters can fail at pipeline start.

Declaring at least one stage is mandatory in practice: a target with no stages silently does
nothing.

The composition environment resolves in this order:

1. `WithCompositionEnvironment` on the selected stage.
2. `GraphQLCompositionSettings.EnvironmentName` on the gateway.
3. The Nitro stage name.

Composition settings explicitly declared by the AppHost win. Settings downloaded from the Nitro
stage fill values that the AppHost leaves unset.

Remote operations use a built-in 15-minute deadline. Approval waits use a built-in two-hour
deadline; these values are not part of the AppHost configuration surface.

### 4. Upload once, then publish the same immutable tag to stages

Build or upload job:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

Deployment job:

```shell
export Parameters__stage="production"
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-publish \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

Arguments can also be forwarded to the AppHost after `--`, for example:

```shell
aspire do fusion-publish -- \
  --Parameters:stage=production \
  --Parameters:tag="$RELEASE_TAG"
```

Prefer environment variables for secrets because command-line values are visible to other local
processes.

Use one tag, commonly the commit SHA, for the upload and every stage in a rollout. Reusing the tag
is safe only for an identical retry. If source content changes, use a new tag. Queue concurrent
writers with stable keys per Nitro API for uploads and per Nitro stage for publications, rather
than canceling an in-flight writer whose remote state may be indeterminate.

### Operational behavior to plan for

- Upload is idempotent when an existing `name@tag` has the same canonical schema, settings, and
  extensions. Different content under the same tag is rejected as an identity collision.
- Publish performs an exact-version preflight before source compute deployment and a second exact
  download before composition.
- Source deployments happen before composition and Nitro publication. Nitro publication happens
  before gateway deployment. A later failure can therefore leave a partial rollout that must be
  retried with the same tag.
- Multiple Nitro targets are processed sequentially, not transactionally. A failure on a later
  target does not roll back an earlier successful publication.
- `WithForce(true)` permits publication after a known validation failure. It should be reserved for
  an explicit operational policy.
- Downloaded source archives are capped at 128,000,000 compressed bytes each. The implementation
  clears owned source and FAR buffers on success, failure, and cancellation.

## Verification performed

| Check                                                               | Result                         |
| ------------------------------------------------------------------- | ------------------------------ |
| `git diff --check origin/main...HEAD`                               | Passed                         |
| `dotnet build src/HotChocolate/Fusion/HotChocolate.Fusion.slnx`     | Passed, no new warnings        |
| Nitro persisted-operation verification                              | Passed, 13 operations verified |
| Fusion Aspire tests, Debug, .NET 9/10/11                            | Passed, 1,167 executions       |
| Fusion Aspire tests, Release persisted-operation mode, .NET 9/10/11 | Passed, 1,122 executions       |
| Fusion packaging project diff against `origin/main`                 | No changes                     |

Step scheduling was checked against the declared step graph in this branch and against decompiled
`Aspire.Hosting` 13.4.6 `DistributedApplicationPipeline` semantics. `aspire do fusion-upload` and
`aspire do fusion-publish` schedule disjoint step sets, so the two documented commands do not
compete; see the note on upload/download ordering under P3.

### Findings resolved during review

The following were open at the branch head and are already addressed by working-tree changes. They
are recorded here so they are not re-reported.

- **Approval replaced the operation deadline and it was not restored after approval.** A quick
  approval let the post-approval publish run for almost the full approval window, and a late
  approval left almost no time for publication. `WaitForTerminalPublishAsync` now accepts
  `operationTimeout` and calls `timeout.CancelAfter(operationTimeout)` on the `Approved` event.
- **Run-mode command-line export never set `AllocatedHttpEndpointUrl`,** so
  `BuildLocalUrlOverrides` skipped the local URL override and an F5 run routed the subgraph to the
  settings URL instead of the DCP-allocated port. `GetSourceSchemaFromCommandLineAsync` now sets it
  (`SchemaComposition.cs:1228`).
- **Endpoint acquisition was rejected by the publish pipeline.**
  `SourceSchemaLocationType.SchemaEndpoint` is now handled in the artifact path
  (`FusionPipelineExecutor.cs:889`) via the new `SchemaEndpointSchemaFetcher`.

## Recommended follow-up coverage

Before merge, add repository-owned regression coverage for:

1. A command-line-exported source completing an upload against a target with a non-loopback
   production endpoint.
2. Release of a claimed publication slot on commit failure, validate failure, and timeout.
3. A configured Nitro publish target with zero declared stages.
4. Approval near the start and near the end of the approval window, followed by a fresh operation
   timeout, now that the deadline reset exists.
5. Publishing credential resolution for every documented source, including the Nitro CLI session
   if that behavior remains documented.
6. `ReplaceDirectoryAtomically` rollback when the move fails after partially creating the
   destination.
7. At least one real Aspire CLI `--list-steps` or end-to-end provider plan that exercises the
   contributed `DeployCompute` topology. The unit and acceptance tests cover the adapter heavily,
   but a real provider integration is the most likely place for future pipeline API drift.
