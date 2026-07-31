# Fusion and .NET Aspire publishing and deployment

## Decision

The canonical split release commands are `aspire do fusion-upload` for the build job and `aspire do fusion-publish` for an environment deployment job. Upload creates immutable source versions and a portable release manifest. Publish consumes that exact manifest, deploys the source services, composes, verifies source readiness, publishes the Fusion configuration to Nitro, deploys the gateway, and completes its public terminal step. Manifest-mode publish never uploads a source.

`aspire publish` remains artifact-only for Fusion. It produces the portable source archives and draft manifest, but it does not call Nitro, resolve a Nitro credential, or mutate a Nitro stage. The broader `aspire deploy` root is also supported and requires `fusion-publish`, but CI uses the two named commands so the release artifact crosses runners explicitly.

The resulting invariant is:

```text
Fusion deployment declared for environment E
              +
aspire do fusion-publish --environment E
              =
Nitro stage reaches terminal success, or the Aspire deployment does not succeed
```

This is an explicit opt-in at AppHost design time, not an implicit inference. An AppHost without a matching Fusion deployment has no Nitro side effect.

## Aspire version and API maturity

At the time of research, the latest stable Aspire release is **13.4.6**, released June 20, 2026. See [Aspire 13.4.6](https://github.com/microsoft/aspire/releases/tag/v13.4.6). Aspire 13.4 made `aspire publish` and `aspire deploy` generally available. See [what's new in Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/).

The programmatic pipeline APIs used to register and order custom steps remain experimental and emit `ASPIREPIPELINES001`. `aspire do` is the command for running a selected step and its dependencies, but that does not make every API behind custom step construction GA. Keep the experimental integration behind a narrow adapter and explicitly accept the diagnostic in that package. Sources: [deployment pipelines](https://aspire.dev/deployment/pipelines/), [ASPIREPIPELINES001](https://aspire.dev/diagnostics/aspirepipelines001/), [`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/), [`aspire deploy`](https://aspire.dev/reference/cli/commands/aspire-deploy/), and [`aspire do`](https://aspire.dev/reference/cli/commands/aspire-do/).

This repository pins the Aspire hosting packages to **13.4.6** in [`src/Directory.Packages.props`](../../../../Directory.Packages.props). CLI examples use Aspire CLI 13.4.6 and its current `--apphost` option.

## Command contract

| Entry point | Fusion behavior | Remote effect |
| --- | --- | --- |
| `aspire publish` | Evaluate/build as Aspire requires, then emit Fusion SDL, settings templates, endpoint bindings, archive inputs, and provenance beneath the output path. | No Nitro API call, Nitro credential resolution, upload, slot request, or stage mutation. Aspire itself may still create/update local deployment configuration, verify certificates, build, or perform other non-Nitro work. |
| `aspire do fusion-upload` | Produce portable Fusion inputs, reconcile immutable source versions, and finalize `fusion-release.json`. | Source upload only. It is not equivalent to deployment completion. |
| `aspire do fusion-publish` | Read the exact promoted manifest, download and verify its source versions, deploy source compute, compose, check readiness, publish the Nitro stage, deploy gateway compute, and complete. | Stage publication only. It never uploads source versions in manifest mode. |
| `aspire deploy --environment Production` | Run the broader provider root, which requires the same `fusion-publish` graph for the matching declaration. | The same stage publication plus any additional AppHost deployment roots. |
| `aspire destroy` | Destroy provider resources according to the deployment target. | Never infer deletion of shared Nitro schema history or stage configurations. Nitro retention needs a separate explicit design. |

Neither publication command scans an arbitrary `aspire publish` directory for a promoted release. `fusion-upload` writes the final manifest, and `fusion-publish` reads only the absolute path supplied through the configured manifest parameter.

## Environment and stage selection

The Fusion deployment declaration must map an Aspire environment to exactly one intended Nitro stage. Do not infer stage from branch, tag, provider, resource name, or `ASPNETCORE_ENVIRONMENT`. Do not deploy staging and production from the same invocation merely because both are declared.

```csharp
var releaseManifest = builder.AddParameter("fusionReleaseManifest");

var nitro = builder.AddNitroTarget("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(builder.AddParameter("nitroApiKey", secret: true));

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithConfigurationTag(builder.AddParameter("releaseId"))
    .WithFusionReleaseManifest(releaseManifest)
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

Adding this resource opts Fusion publication into `aspire do fusion-publish --environment Production` and the broader `aspire deploy --environment Production`. A separate `.ForEnvironment("Staging").ToStage("staging")` declaration runs only when Staging is selected. Graph construction must fail when multiple Fusion deployments ambiguously claim the same environment/API/stage or when the selected environment has inconsistent mappings.

The sketch uses the implemented Aspire 13.4.6 public surface and keeps required values required.

## Existing `HotChocolate.Fusion.Aspire` state

The repository already has a source-schema and composition graph. Adapt it rather than creating a second Nitro-only graph.

* [`GraphQLResourceBuilderExtensions.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/GraphQLResourceBuilderExtensions.cs) exposes `WithGraphQLSchemaFile`, `WithGraphQLSchemaEndpoint`, and `WithGraphQLSchemaComposition`.
* [`SchemaComposition.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/SchemaComposition.cs) discovers referenced resources, creates internal `SourceSchemaInfo` records, and subscribes to `AfterResourcesCreatedEvent` through internal annotations and private discovery helpers.
* Native endpoint acquisition uses `/graphql/schema.graphql`. `/graphql` is only for an explicitly identified Apollo Federation `_service.sdl` flow.
* File mode can default its asserted name from the Aspire resource, while upload derives the authoritative name from `schema-settings.json`. The manifest must validate that the declared/manifest name exactly matches settings `name`.
* Current runtime discovery is suitable for `aspire run`, but normal DCP application orchestration is disabled in publish mode in the inspected Aspire source. Publish cannot assume resource endpoints are running.

The integration reuses these declarations and preserves the composition relationships between the
gateway resource and its referenced sources. The deploy pipeline augments that model with provider
step discovery, environment-resolved endpoint bindings, and readiness evidence.

## Can `aspire publish` use Hot Chocolate command-line schema export?

Yes, as an explicit short-lived child process. Aspire publish mode does not start normal AppHost/DCP resources, but the custom publish pipeline action can start the referenced GraphQL project itself to run `HotChocolate.AspNetCore.CommandLine`.

This export is not a pure or side-effect-free metadata operation. The child process executes the application's `Program`, configuration loading, service registration, `Build`, and endpoint mapping. Resolving the request executor performs full Hot Chocolate schema initialization, including type modules, schema hooks, warmup tasks, and any user code reached by those paths. Kestrel and ordinary `IHostedService` instances do not start because command mode invokes the CLI and skips `host.Run`/`host.RunAsync`. See [`WebApplicationExtensions.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore.CommandLine/WebApplicationExtensions.cs) and [`ExportCommand.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore.CommandLine/Command/ExportCommand.cs).

The correct registration symbol is `ExportSchemaOnStartup`, not `ExportSchemaFileOnStartup`. It registers a schema executor warmup task, so it can export again while the CLI export command initializes the executor. See [`HotChocolateAspNetCoreServiceCollectionExtensions.Warmup.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore/Extensions/HotChocolateAspNetCoreServiceCollectionExtensions.Warmup.cs). Projects used by the integration should either avoid the startup exporter in this mode or tolerate and isolate both writes.

The application entry point must return the command exit code:

```csharp
return await app.RunWithGraphQLCommandsAsync(args);
```

Use an argument-safe process invocation equivalent to:

```text
dotnet run --project /absolute/path/Products.Subgraph.csproj --configuration Release --no-launch-profile -- schema export --output /isolated/products/schema.graphqls --schema-name Products
```

`schema` must be the first application argument after `--`, otherwise the application follows its normal server path. Always select the schema name explicitly. `ExportCommand` can print `No schemas registered.` and return successfully without creating output, so exit code zero is insufficient. The integration must validate the expected schema and settings artifacts.

V1 should support checked-in/generated schema files and an explicit per-resource export command. Automatic `dotnet run` inference is opt-in because application startup and schema initialization can have arbitrary user side effects. Longer term, export from the exact published assembly or container that will be deployed, and bind schema provenance to its build/image digest.

### Child-process requirements

* Resolve a project with `ProjectResource.GetProjectMetadata().ProjectPath`. Reject resource types without a supported file/export declaration rather than guessing.
* Precreate an isolated output directory per source resource and invocation. Never share the application working directory or accept a stale file as fallback.
* Use `System.Diagnostics.Process` with `ProcessStartInfo.ArgumentList`, not shell concatenation. `WithProcessCommand` is a dashboard/resource command API, not the custom deployment pipeline runner.
* Use an environment allowlist. Do not pass Nitro credentials or unrelated AppHost secrets to the child.
* Record configuration, target framework, RID, working directory, launch-profile policy, project
  path and SHA-256, and exported schema SHA-256 in provenance. `--no-launch-profile` is the
  deterministic default. Exact deployed assembly or container-image binding remains future work.
* Enforce timeout and cancellation, and kill the entire process tree on termination. Capture bounded stdout/stderr and redact sensitive values.
* Validate freshness, non-empty SDL, parseable settings, exact `schema-settings.json` name, extensions, and the explicitly selected schema name.
* Treat a generated settings URL that resolves to localhost/loopback as a template risk. Reject it for final deployment and materialize the provider-resolved production URL only after readiness.
* Verify deterministic repeated exports for the same declared build inputs. Differences are a hard failure unless the differing provenance explains a new release input.

Register the action with `WithPipelineStepFactory`. Use `WithPipelineConfiguration`, `PipelineConfigurationContext.GetSteps`, and `RequiredBy` to connect it to the release graph. There is no universal provider-neutral after-compute/readiness tag. Every supported provider needs an explicit step selector/readiness adapter.

## Identity and configuration

The recommended common path uses one immutable release ID as the configuration tag and as the default upload version for bare source names. The native model still keeps each source version explicit so an advanced rollout can select `products@version-a` and `inventory@version-b`.

| Setting | Scope | Example | Policy |
| --- | --- | --- | --- |
| Cloud URL | Nitro target | `https://api.chillicream.com` | Explicit target/config value, then documented `NITRO_CLOUD_URL` compatibility fallback. HTTPS unless an explicit development exception exists. |
| API ID | Nitro target | `products-fusion` | Explicit identifier, not a display name. |
| API key | Credential | secret `ParameterResource` | Inject through CI/secret provider. Never store in artifacts or logs. |
| Aspire environment | Deployment selector | `Production` | Required explicit `.ForEnvironment(...)` mapping. |
| Stage | Nitro deployment | `production` | Required explicit mapping. Never inferred. |
| Release/configuration tag | Deployment invocation | `build-842-a1b2c3d4` | Unique for the rollout and stable across its retries. Inject once per CI release invocation. |
| Source version | Per source | `a1b2c3d4` or content hash | Immutable and derived from the exact deployed schema/build. Defaults to release tag only for bare-name compatibility. |
| Approval | Deployment | `true` | Explicit policy. Deployment succeeds only at terminal Nitro success. |
| Force | Deployment | `false` | Never infer and never default to true. |
| Timeouts | Deployment | operation and approval limits | Explicit and conservative. Timeout returns failed/indeterminate plus the recoverable request ID. |
| Stage ownership | Deployment | authoritative or additive | Explicit single-writer policy. |

A Git commit alone is not always a rollout identity. Redeploying the same commit with different infrastructure, a dirty worktree, rebuilt image, changed endpoint binding, or altered settings can produce different desired state. CI should inject a unique release ID per release invocation and reuse it for retries of that same rollout. Do not derive the current release from stale Aspire deployment cache state.

### Existing Nitro CLI input mapping

| Existing input | Aspire-native input | Compatibility meaning |
| --- | --- | --- |
| `NITRO_API_ID` | `Nitro:ApiId` / `Parameters__nitroApiId` | API identity. |
| `NITRO_STAGE` | deployment `.ToStage(...)` / `Parameters__nitroStage` | Explicit environment-to-stage mapping. |
| `NITRO_TAG` | `configurationTag`, plus default source version for bare names | Publish uses it as configuration tag/default selected version; upload uses it as source version. Aspire splits the concepts internally. |
| `NITRO_API_KEY` | secret `nitroApiKey` / `Parameters__nitroApiKey` | Never write to artifacts/logs. Aspire deployment cache can persist resolved secrets in plaintext as described below. |
| `NITRO_CLOUD_URL` | `Nitro:CloudUrl` / `Parameters__nitroCloudUrl` | Compatibility override for the API base URL. |

Prefer `ParameterResource` and Aspire configuration, including `Parameters__nitroApiKey` and `Parameters__releaseId`. Support `NITRO_*` only as a documented fallback with deterministic precedence. See [external parameters](https://aspire.dev/fundamentals/external-parameters/).

Current source metadata remains limited to the existing GitHub and Azure DevOps model. Richer
provider/actor metadata and binding schema provenance to the exact deployed build or container image
digest remain future work.

## Build-once artifacts and manifest apply

`aspire publish` produces source archives and a draft release manifest beneath
`<output-path>/fusion/releases/<release-id>`. It has no Nitro credential resolution, upload,
composition slot, commit, or stage mutation. `aspire do fusion-upload` runs that artifact step,
reconciles each immutable source version into every declared Nitro API target, verifies existing
versions by normalized content, and atomically writes the final `fusion-release.json`.

The final manifest records the release ID, exact composition tool identity and options, the complete
source-set digest, each source version and content digest, and every Nitro API target to which the
source set was uploaded. It contains no credential, stage, Aspire environment, timestamp, checkout
path, or runner-specific absolute path.

The deploy runner receives only that final manifest as the promotion artifact. Manifest apply reads
the exact absolute path supplied by `Parameters__fusionReleaseManifest`, verifies the release and
target bindings, downloads every exact source version from Nitro, verifies normalized content, and
composes the environment-specific FAR. It never reads the build runner's source ZIPs and never
uploads a source version.

## Mandatory deploy graph

The implemented promoted-manifest ordering is:

```text
fusion-release-prepare -> fusion-compose
                                |
source DeployCompute -----------+-> fusion-readiness
                                         |
                                         v
                              fusion-publish-stage
                                         |
                                         v
                              gateway DeployCompute
                                         |
                                         v
                               fusion-publish
                                         |
                                         v
                         WellKnownPipelineSteps.Deploy
```

`fusion-publish-stage` is the internal action that requests, validates, commits, and observes the
Nitro publication. The public `fusion-publish` step is a terminal completion step. The split is
required for a first release: the gateway must not start before the first FAR exists, but the named
public command must not finish until the gateway deployment terminal succeeds.

The source provider graph may run in parallel with manifest preparation and composition. Readiness
depends on both the composed FAR and every referenced source deployment terminal. It probes the
resolved production URLs and retries transient DNS, connection, request-timeout, and HTTP 5xx
failures with a bounded delay until the deployment's configured operation timeout. Responses below
HTTP 500 retain their existing accepted semantics. Deadline failures identify the source and
endpoint.

`WithPipelineConfiguration` selects `DeployCompute` on the declared resource when a provider uses
that shape. For Aspire 13.4 materialized targets, it follows `DeploymentTargetAnnotation` and
selects `DeployCompute` from the target resource. Infrastructure provisioning is not accepted as a
deployment terminal. Graph execution fails closed if any source or gateway terminal cannot be
proven.

`RequiredBySteps = [WellKnownPipelineSteps.Deploy]` attaches the public terminal to the broader
Deploy root. Explicit dependencies, not sibling membership under Deploy, enforce the order above.
No custom step depends on the Deploy root, which avoids a cycle.

## Current Nitro reconciliation flow

For each matching deployment, the apply graph executes even when the desired stage state is
unchanged:

1. Resolve the exact Aspire environment declaration, release ID, manifest path, stage, force and
   approval policy, and timeouts.
2. Verify the promoted manifest, target binding, source set, and composition-tool identity. Download
   each exact source from Nitro and verify its normalized digest.
3. Compose the environment-specific FAR while the provider may build and deploy the source
   services.
4. Wait for every source deployment terminal, then poll its resolved production endpoint until it
   responds below HTTP 500 or the operation timeout expires.
5. Acquire the Nitro slot, apply the configured validation and force policy, commit the already
   composed FAR, and observe approval or processing until terminal success.
6. Deploy the gateway only after the internal stage publication succeeds.
7. Complete the public `fusion-publish` terminal only after the gateway deployment succeeds,
   allowing the broader Aspire Deploy root to complete.

On timeout, cancellation, or lost response, return failed/indeterminate and report the recoverable request ID. Release a claimed slot only when lifecycle-safe. An approval request can outlive a CI process, so a new invocation must resume/reconcile it rather than blindly create another publication.

## Idempotency, serialization, and approvals

Every-deploy reconciliation makes idempotency mandatory:

* Same source name/version plus normalized-identical content is success/no-op. Same identity plus different content is a collision.
* Same release/configuration tag plus identical desired FAR and selected source intent is success/no-op. Same tag plus different intent is a collision.
* Persist `requestId`, phase, desired FAR digest, release ID, and source selection. Local cache alone is insufficient because CI runners are ephemeral and responses can be lost. Require server-backed lookup/idempotency or an equivalent durable release record before claiming robust retry behavior.
* Serialize publishers by `(cloudUrl, apiId, stage)`. A stale approval must never supersede a newer rollout. Enforce stage queue/order on the server or fail when it cannot be proven.
* Retry only bounded, known-transient transport/server failures with exponential backoff and jitter. Never retry authorization, invalid schema/settings, stage/API not found, force-policy rejection, or identity collisions.

Aspire stores deployment state under `~/.aspire/deployments`, including resolved parameter values and secrets in plaintext. Never share, publish, or commit this cache. Inject credentials and the release ID from CI, and use clear-cache behavior when state is stale or compromised. The cache cannot be the authority for Nitro idempotency. See [deployment state caching](https://aspire.dev/deployment/deployment-state-caching/).

## Stage ownership and partial state

Choose and document one stage ownership policy:

| Policy | Behavior | Constraint |
| --- | --- | --- |
| Authoritative declared set | The resulting FAR contains exactly the AppHost-declared source set for this deployment. Undeclared schemas are removed. | Recommended for a single release owner. Requires complete declarations. |
| Additive preserve-undeclared | Replace declared sources and preserve undeclared sources from the latest stage FAR. | Required for shared ownership, but prone to drift. Still requires one serialized writer/coordinator per stage. |

There is no transaction across the service deployment provider, artifact uploads, and Nitro stage publication. Uploads can remain orphaned. Compute can be updated while the old FAR remains active if Nitro validation, approval, or commit fails. Automatic rollback across the compute provider and Nitro is unsafe without a provider-specific, tested rollback protocol.

Source-service changes must remain backward-compatible with the old FAR during this window.
Operators need a documented recovery path using the persisted request ID, release manifest, source
digests, and previous FAR identity. The deployment result must report partial state precisely
instead of claiming rollback.

## Named `aspire do` operations

Use target-specific names and actual CLI options:

```bash
aspire publish --apphost ./MyApp.AppHost/MyApp.AppHost.csproj --output-path ./artifacts/aspire --environment Release --non-interactive
aspire do fusion-upload --apphost ./MyApp.AppHost/MyApp.AppHost.csproj --output-path ./artifacts/aspire --environment Release --non-interactive
aspire do fusion-publish --apphost ./MyApp.AppHost/MyApp.AppHost.csproj --output-path ./artifacts/aspire --environment Production --non-interactive
aspire do fusion-publish --apphost ./MyApp.AppHost/MyApp.AppHost.csproj --environment Production --list-steps
```

`aspire do` evaluates and builds the AppHost unless `--no-build` is used, and it reruns the selected step's dependencies. Deployment commands default to Production. Inspect scope with `--list-steps`. The safe reconcile command carries the same provider compute/readiness dependencies as Deploy, so it may redeploy or recheck resources.

`aspire publish` remains the artifact inspection path, and upload alone does not publish a release. `fusion-publish` requires the supported compute and readiness graph before it claims a slot.

## Implementation layout

The supported integration is packaged in `ChilliCream.Nitro.Aspire`. It reuses
`FusionSourceSchemaArchive` for immutable source packaging and the Nitro Fusion workflow for
download, normalized verification, composition, publication, approvals, and terminal reporting.
The local pipeline adapter owns environment selection, promoted-manifest validation, provider-step
ordering, readiness polling, and the named `fusion-upload` and `fusion-publish` commands.

### Implemented release-critical phases

1. Aspire 13.4.6 graph wiring covers exact environment selection, direct and materialized-target
   `DeployCompute` discovery, explicit dependencies, `RequiredBySteps`, named `do` commands, and
   cycle-safe source/publication/gateway ordering.
2. Build produces immutable source archives and a portable manifest. Upload verifies or creates
   exact Nitro source identities, and apply re-downloads and verifies them on a separate runner.
3. Production endpoint readiness is bounded by `OperationTimeout`; unsupported provider graphs fail
   closed.
4. Nitro publication preserves normalized duplicate policy, force, approvals, timeouts, terminal
   reporting, and retry reconciliation.
5. The internal publication step precedes gateway deployment, and the public terminal is a
   prerequisite of the Aspire Deploy root.

## Tests and acceptance criteria

| Layer | Required evidence |
| --- | --- |
| Environment selection | Production deploy runs only Production Fusion deployment; Staging runs only Staging; ambiguous mappings fail graph construction. |
| Publish | Produces templates/provenance and has no Nitro calls, credential resolution, upload, slot, or stage mutation. |
| Export lifecycle | Sentinels prove `Program`, configuration, service registration, endpoint mapping, schema hooks/modules, and executor warmups run. Sentinels also prove Kestrel and normal `IHostedService` instances do not start in command mode. |
| Export command | Uses argv-safe `ArgumentList`, explicit schema name, isolated/precreated output, bounded redacted output, timeout/cancellation/tree kill, and no Nitro secrets in the environment. |
| Export validation | Multi-schema selection is exact; zero exit with no registered schema/no output fails; localhost settings fail final materialization; stale output never falls back; repeated identical input exports deterministically. |
| Export provenance | Configuration, TFM, RID, working directory, launch-profile policy, project path and SHA-256, and exported schema SHA-256 are recorded. Exact deployed image binding is future work. |
| Pipeline topology | Source deployment and actual readiness precede internal Nitro publication; Nitro terminal success precedes gateway deployment; gateway completion precedes the public `fusion-publish` terminal and Aspire Deploy completion. No custom step depends on the Deploy root. |
| Provider readiness | Supported provider proves a real production endpoint, and unsupported Aspire-managed sources fail graph construction. External source requires explicit proof. |
| Materialization | The selected composition environment produces the final settings and FAR; settings name equals the declared source name. |
| Upload reconciliation | Exact normalized source identity is no-op; mismatch is collision; missing source uploads once under bounded retries. |
| Composition | Acquires slot after readiness, re-downloads latest FAR and sources, applies declared stage ownership, composes locally, and validates/commits the FAR. |
| Publication identity | Same release tag/identical intent is no-op; same tag/different intent fails; new release is serialized after existing stage work. |
| Resume/approval | Lost response or ephemeral runner resumes by request ID/server record. Approval timeout is failed/indeterminate, and Deploy cannot succeed before terminal Nitro success. Stale approval cannot supersede a later rollout. |
| Partial state | Nitro failure after compute update reports new compute plus old FAR, leaves recoverable state, and does not claim automatic rollback. |
| `aspire do` | Safe reconcile includes compute/readiness dependencies. Export/upload inspection cannot masquerade as completed deployment. |
| Compatibility | Focused suite passes on the repository's Aspire 13.4.6 pin, and real `aspire do --list-steps` output proves the materialized-target graph. |

Done means every matching `aspire deploy` executes Fusion reconciliation, can verified-no-op on identical desired state, and cannot complete until Nitro reaches terminal success. It also means the pipeline refuses to construct when exact compute/readiness ordering cannot be proven.

## Operational boundaries

CI remains responsible for serializing writers to the same Nitro stage. Source-service changes must
remain compatible with the previously active FAR during the interval between source deployment and
stage publication. The integration fails closed when it cannot prove a managed resource's provider
deployment terminal, and it does not infer deletion of shared Nitro history during Aspire destroy.

## Sources

* [What's new in Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/)
* [Aspire 13.4.6 release](https://github.com/microsoft/aspire/releases/tag/v13.4.6)
* [Deploy with Aspire](https://aspire.dev/deployment/deploy-with-aspire/)
* [Aspire deployment pipelines](https://aspire.dev/deployment/pipelines/)
* [`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/), [`aspire deploy`](https://aspire.dev/reference/cli/commands/aspire-deploy/), and [`aspire do`](https://aspire.dev/reference/cli/commands/aspire-do/)
* [Aspire external parameters](https://aspire.dev/fundamentals/external-parameters/)
* [ASPIREPIPELINES001](https://aspire.dev/diagnostics/aspirepipelines001/)
* [Aspire deployment state caching](https://aspire.dev/deployment/deployment-state-caching/)
* Local code: [`GraphQLResourceBuilderExtensions.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/GraphQLResourceBuilderExtensions.cs), [`SchemaComposition.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/SchemaComposition.cs), [`WebApplicationExtensions.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore.CommandLine/WebApplicationExtensions.cs), [`ExportCommand.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore.CommandLine/Command/ExportCommand.cs), [`HotChocolateAspNetCoreServiceCollectionExtensions.Warmup.cs`](../../../../HotChocolate/AspNetCore/src/AspNetCore/Extensions/HotChocolateAspNetCoreServiceCollectionExtensions.Warmup.cs), [`FusionConfiguration/IFusionConfigurationClient.cs`](FusionConfiguration/IFusionConfigurationClient.cs), [`FusionConfiguration/FusionConfigurationClient.cs`](FusionConfiguration/FusionConfigurationClient.cs), [`ChilliCream.Nitro.Client.csproj`](ChilliCream.Nitro.Client.csproj), and [`src/Directory.Packages.props`](../../../../Directory.Packages.props).
