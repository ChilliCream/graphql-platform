# Fusion and .NET Aspire publishing and deployment

## Decision

`aspire deploy` is the canonical Fusion release command. When an AppHost explicitly declares a Fusion deployment for the selected Aspire environment, every matching `aspire deploy` must export, reconcile source-schema uploads, compose, and publish the Fusion configuration before the well-known Deploy step can complete.

`aspire publish` remains artifact-only for Fusion. It produces portable schema inputs, templates, bindings, and provenance, but it does not call Nitro, resolve a Nitro credential, or mutate a Nitro stage. Named `aspire do` steps remain useful for inspection, repair, and controlled reconciliation, but they are supplementary and must not provide a weaker route around compute/readiness dependencies.

The resulting invariant is:

```text
Fusion deployment declared for environment E
              +
aspire deploy --environment E
              =
Nitro stage reaches terminal success, or the Aspire deployment does not succeed
```

This is an explicit opt-in at AppHost design time, not an implicit inference. An AppHost without a matching Fusion deployment has no Nitro side effect.

## Aspire version and API maturity

At the time of research, the latest stable Aspire release is **13.4.6**, released June 20, 2026. See [Aspire 13.4.6](https://github.com/microsoft/aspire/releases/tag/v13.4.6). Aspire 13.4 made `aspire publish` and `aspire deploy` generally available. See [what's new in Aspire 13.4](https://aspire.dev/whats-new/aspire-13-4/).

The programmatic pipeline APIs used to register and order custom steps remain experimental and emit `ASPIREPIPELINES001`. `aspire do` is the command for running a selected step and its dependencies, but that does not make every API behind custom step construction GA. Keep the experimental integration behind a narrow adapter and explicitly accept the diagnostic in that package. Sources: [deployment pipelines](https://aspire.dev/deployment/pipelines/), [ASPIREPIPELINES001](https://aspire.dev/diagnostics/aspirepipelines001/), [`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/), [`aspire deploy`](https://aspire.dev/reference/cli/commands/aspire-deploy/), and [`aspire do`](https://aspire.dev/reference/cli/commands/aspire-do/).

This repository currently pins `Aspire.Hosting.AppHost`, `Aspire.Hosting`, and `Aspire.Hosting.PostgreSQL` to **13.1.2** in [`src/Directory.Packages.props`](../../../../Directory.Packages.props). The required step-discovery and pipeline configuration concepts exist in that pin. Implement and test against 13.1.2 first, then separately test and upgrade to the current stable line.

## Command contract

| Entry point | Fusion behavior | Remote effect |
| --- | --- | --- |
| `aspire publish` | Evaluate/build as Aspire requires, then emit Fusion SDL, settings templates, endpoint bindings, archive inputs, and provenance beneath the output path. | No Nitro API call, Nitro credential resolution, upload, slot request, or stage mutation. Aspire itself may still create/update local deployment configuration, verify certificates, build, or perform other non-Nitro work. |
| `aspire deploy --environment Production` | Run the provider deployment and every Fusion deployment explicitly mapped to `Production`. It succeeds only after each mapped Nitro publication reaches terminal success. | Mandatory Nitro reconciliation for each matching declaration. |
| `aspire do fusion-upload` | Produce portable Fusion inputs and reconcile immutable source versions. | Upload only. It is not equivalent to deployment completion. |
| `aspire do fusion-publish` | Run the same compute discovery, readiness, upload, composition, and publication dependency graph as the matching deploy. | Safe supplementary reconciliation. It can redeploy or recheck prerequisites as dictated by its graph. |
| `aspire destroy` | Destroy provider resources according to the deployment target. | Never infer deletion of shared Nitro schema history or stage configurations. Nitro retention needs a separate explicit design. |

`aspire deploy` does not consume an arbitrary previously produced `aspire publish` directory as a promoted release. It evaluates the AppHost and executes pipeline dependencies for that invocation. Artifact promotion requires an explicit artifact-path/apply step or an external release tool.

## Environment and stage selection

The Fusion deployment declaration must map an Aspire environment to exactly one intended Nitro stage. Do not infer stage from branch, tag, provider, resource name, or `ASPNETCORE_ENVIRONMENT`. Do not deploy staging and production from the same invocation merely because both are declared.

```csharp
var nitro = builder.AddNitro("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(builder.AddParameter("nitroApiKey", secret: true));

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithConfigurationTag(builder.AddParameter("releaseId"))
    .WithDefaultSourceVersionFromGitCommit()
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

Adding this resource opts Fusion publication into every `aspire deploy --environment Production`. A separate `.ForEnvironment("Staging").ToStage("staging")` declaration runs only when Staging is selected. Graph construction must fail when multiple Fusion deployments ambiguously claim the same environment/API/stage or when the selected environment has inconsistent mappings.

The sketch is illustrative. Match the public API to Aspire 13.1.2 resource idioms and keep required values required.

## Existing `HotChocolate.Fusion.Aspire` state

The repository already has a source-schema and composition graph. Adapt it rather than creating a second Nitro-only graph.

* [`GraphQLResourceBuilderExtensions.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/GraphQLResourceBuilderExtensions.cs) exposes `WithGraphQLSchemaFile`, `WithGraphQLSchemaEndpoint`, and `WithGraphQLSchemaComposition`.
* [`SchemaComposition.cs`](../../../../HotChocolate/Fusion/src/Fusion.Aspire/SchemaComposition.cs) discovers referenced resources, creates internal `SourceSchemaInfo` records, and subscribes to `AfterResourcesCreatedEvent` through internal annotations and private discovery helpers.
* Native endpoint acquisition uses `/graphql/schema.graphql`. `/graphql` is only for an explicitly identified Apollo Federation `_service.sdl` flow.
* File mode can default its asserted name from the Aspire resource, while upload derives the authoritative name from `schema-settings.json`. The manifest must validate that the declared/manifest name exactly matches settings `name`.
* Current runtime discovery is suitable for `aspire run`, but normal DCP application orchestration is disabled in publish mode in the inspected Aspire source. Publish cannot assume resource endpoints are running.

Refactor these declarations into a reusable internal model while preserving composition relationships between the gateway/composition resource and its referenced sources. The deploy pipeline then augments those declarations with provider step discovery, deployment endpoint bindings, and readiness evidence.

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
* Record configuration, target framework, RID, working directory, launch-profile policy, project/assembly identity, and content/image digest in provenance. `--no-launch-profile` is the deterministic default.
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

Current source metadata must remain limited to the existing GitHub and Azure DevOps model. Richer provider/actor metadata is future work. Bind schema provenance to the exact build or container image digest deployed by the provider steps, not only to a mutable working directory.

## Two-phase artifacts

Some source-schema settings include deployment-resolved production URLs. A local publish cannot safely guess them. Artifact generation therefore has two phases.

### Publish phase

`aspire publish` emits immutable build inputs under the selected output path:

```text
<output-path>/fusion/production/
  nitro-deployment-template.json
  sources/
    products/
      schema.graphqls
      schema-settings.template.json
      extensions/
      provenance.json
    inventory/
      schema.graphqls
      schema-settings.template.json
      extensions/
      provenance.json
```

Its Fusion graph ends at artifacts:

```text
source file or explicit child-process export
                  |
          validate and record provenance
                  |
       SDL/settings template/bindings
                  |
          publish output complete
```

There is no Nitro credential resolution, upload, composition slot, commit, or stage mutation on this graph.

Acquisition supports an existing SDL file and an explicit native export command such as:

```text
dotnet run --project /absolute/path/Products.Subgraph.csproj --configuration Release --no-launch-profile -- schema export --output /isolated/products/schema.graphqls --schema-name Products
```

Endpoint acquisition is unavailable by default in publish mode. An externally managed endpoint can be used only when explicitly declared and authenticated. Apollo `/graphql` requires an explicit Apollo source kind.

### Deploy materialization phase

After provider deployment and actual readiness, resolve production endpoint bindings, materialize final `schema-settings.json`, and build the archive with the public `HotChocolate.Fusion.SourceSchema.Packaging.FusionSourceSchemaArchive`. Stage composition and commit must occur after service deployment and actual readiness. The final manifest records the release and provider provenance:

```json
{
  "formatVersion": 1,
  "cloudUrl": "https://api.chillicream.com",
  "apiId": "products-fusion",
  "environment": "Production",
  "stage": "production",
  "configurationTag": "build-842-a1b2c3d4",
  "stageOwnership": "authoritative",
  "sources": [
    {
      "name": "products",
      "sourceVersion": "a1b2c3d4",
      "archive": "materialized/products-a1b2c3d4.zip",
      "sha256": "...",
      "imageDigest": "sha256:..."
    }
  ]
}
```

The deployment manifest and its validation are new work. There is no existing Nitro CLI manifest/SDL validator to reuse. Validate all paths, hashes, settings names, bindings, and provenance. Write the final manifest atomically after all archives materialize.

## Mandatory deploy graph

The required ordering is:

```text
publish-time export/validation -----+
                                    |
provider source compute deploy      |    (may run in parallel)
              |                     |
actual production readiness --------+
              |
final settings/archive materialization
              |
reconcile source uploads
              |
request and claim Nitro slot
              |
re-download current stage FAR and selected archives
              |
authoritative local composition
              |
policy-aware remote validation and FAR commit
              |
terminal publication or approval completion
              |
WellKnownPipelineSteps.Deploy completes
```

Export can run in parallel with provider deployment. Readiness plus completed export feeds final settings/archive materialization, which precedes upload and stage composition. Upload may move earlier only when the final archive has no deployment-resolved input and its endpoint/provenance is already deployment-complete. If schema export itself requires deployment-only configuration, move that export after deployment/readiness or fail the resource as unsupported. Never claim a Nitro slot while waiting for compute deployment or readiness.

The Nitro reconcile step must depend on the lowest provider deployment and readiness steps and be required by the well-known Deploy step. It must never depend on Deploy itself, because that creates a cycle.

`RequiredBySteps = [WellKnownPipelineSteps.Deploy]` guarantees that reconciliation is included before Deploy completes. It does not order sibling prerequisites relative to one another. Register export with `WithPipelineStepFactory`. In `WithPipelineConfiguration`, discover concrete provider steps through `PipelineConfigurationContext.GetSteps(resource, WellKnownPipelineTags.DeployCompute)` where the provider supports it, then create explicit dependencies with `RequiredBy` from compute to readiness, export/readiness to materialization, upload to composition, and Nitro terminal status to root Deploy. These APIs are present in the repository-pinned Aspire 13.1.2 surface. There is no universal after-compute/readiness tag, so a provider adapter or explicit selector is required.

Provider-neutral readiness is not proven. A `DeployCompute` step completing may mean a resource was submitted, not that its production endpoint accepts traffic. Provider adapters must add a supported readiness/check step. Graph construction must fail if an Aspire-managed source lacks supported deploy-step discovery or production readiness. A source explicitly declared as external may participate only with an explicit readiness proof.

If Nitro publication is intended as an application-wide finalizer, it must also depend on all unrelated release prerequisites whose failure should prevent cutover. Merely being a sibling prerequisite of Deploy does not provide that ordering.

## Current Nitro reconciliation flow

For each matching deploy, the reconciliation step executes even if the desired state is unchanged:

1. Resolve the selected environment declaration, release ID, stage ownership, force/approval policy, and timeouts.
2. Run publish-time export/local validation in parallel with provider deployment where safe, then complete actual provider readiness. If export needs deployment-only configuration, run it after readiness or fail unsupported.
3. After both export and readiness, resolve final production bindings, materialize source archives, and verify settings name/provenance/image digest.
4. Reconcile each `name@sourceVersion`. Download an existing exact version and compare normalized schema, settings, and extensions. Identical is a verified no-op. Different content under the same identity is a hard collision. Upload only missing versions.
5. Acquire stage serialization for `(cloudUrl, apiId, stage)`. Reconcile any persisted/server-backed request state before requesting a new slot.
6. Request and claim the slot only after compute readiness. Re-download the latest stage FAR and every selected source archive after the slot is acquired so composition uses authoritative current inputs.
7. Compose locally. Apply the selected stage ownership policy, preserving or removing undeclared schemas as defined below, and produce the desired FAR.
8. Compare the desired FAR/intent with an existing publication for the same configuration tag. Identical is terminal success/no-op. Different intent for the same tag is a collision.
9. When approval is disabled, validate the FAR before commit and apply the explicit force policy. When approval is enabled, commit starts processing and validation/approval states are observed through the subscription.
10. Commit and subscribe until terminal success or failure. Approval-gated deployment succeeds only after terminal Nitro success.
11. Complete the Nitro reconciliation prerequisite, allowing `WellKnownPipelineSteps.Deploy` to complete.

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

Source-service changes must remain backward-compatible with the old FAR during this window. Operators need a documented recovery path using the persisted request ID, release manifest, image digests, and previous FAR identity. The deployment result must report partial state precisely instead of claiming rollback.

## Named `aspire do` operations

Use target-specific names and actual CLI options:

```bash
aspire publish --output-path ./artifacts/aspire --environment Production --non-interactive
aspire do fusion-upload --output-path ./artifacts/aspire --environment Production --non-interactive
aspire do fusion-publish --output-path ./artifacts/aspire --environment Production --non-interactive
aspire do fusion-publish --list-steps
```

`aspire do` evaluates and builds the AppHost unless `--no-build` is used, and it reruns the selected step's dependencies. Deployment commands default to Production. Inspect scope with `--list-steps`. The safe reconcile command carries the same provider compute/readiness dependencies as Deploy, so it may redeploy or recheck resources.

`aspire publish` remains the artifact inspection path, and upload alone does not publish a release. `fusion-publish` requires the supported compute and readiness graph before it claims a slot.

## Packaging and implementation plan

`ChilliCream.Nitro.Client` is explicitly non-packable in [`ChilliCream.Nitro.Client.csproj`](ChilliCream.Nitro.Client.csproj). Extract/move the supported client and workflow into a packable assembly or deliberately co-ship supported assemblies. A public `ChilliCream.Nitro.Aspire` package must not depend on an internal non-packable project.

| Area | Work |
| --- | --- |
| Existing Fusion Aspire | Adapt its source annotations and composition relationships into a reusable declaration model. |
| Artifact layer | Reuse `FusionSourceSchemaArchive`; add template, binding, provenance, manifest, normalization, and validation code. |
| Nitro workflow | Extract current local FAR composition and `IFusionConfigurationClient` orchestration behind a packable supported API. |
| Aspire pipeline | Add environment-filtered resources, provider adapters, readiness proof, exact step dependencies, Deploy completion prerequisite, and `do` commands. |
| State/recovery | Add server-backed request/tag lookup or durable release-state service, stage serialization, resume, collision checks, and terminal status reporting. |

### Release-critical phases

1. Prove pipeline wiring on Aspire 13.1.2: environment filtering, `GetSteps(...DeployCompute)`, exact dependencies, `RequiredBySteps`, `do --list-steps`, and cycle detection.
2. Refactor existing Fusion Aspire declarations and implement two-phase artifacts with provenance bound to deployed build/image digest.
3. Implement provider adapter one, including production endpoint readiness. Fail unsupported graphs closed.
4. Implement mandatory upload reconciliation and normalized duplicate policy.
5. Implement stage serialization, request resume/idempotency, authoritative re-download/local FAR composition, validation/commit, approvals, and terminal reporting.
6. Integrate reconciliation as a prerequisite of WellKnown Deploy for every environment-matched declaration.
7. Exercise partial-state and recovery behavior end to end, then upgrade/test against Aspire 13.4.6.

## Tests and acceptance criteria

| Layer | Required evidence |
| --- | --- |
| Environment selection | Production deploy runs only Production Fusion deployment; Staging runs only Staging; ambiguous mappings fail graph construction. |
| Publish | Produces templates/provenance and has no Nitro calls, credential resolution, upload, slot, or stage mutation. |
| Export lifecycle | Sentinels prove `Program`, configuration, service registration, endpoint mapping, schema hooks/modules, and executor warmups run. Sentinels also prove Kestrel and normal `IHostedService` instances do not start in command mode. |
| Export command | Uses argv-safe `ArgumentList`, explicit schema name, isolated/precreated output, bounded redacted output, timeout/cancellation/tree kill, and no Nitro secrets in the environment. |
| Export validation | Multi-schema selection is exact; zero exit with no registered schema/no output fails; localhost settings fail final materialization; stale output never falls back; repeated identical input exports deterministically. |
| Export provenance | Configuration, TFM, RID, working directory, launch-profile policy, project/assembly and exact build/image digest match the deployed resource. Unsupported resource types fail closed. |
| Pipeline topology | Export may parallel compute; export plus actual readiness precede final materialization/upload; upload/readiness precede slot; Nitro terminal success precedes Deploy completion. Sibling prerequisites cannot race and no step depends on Deploy. |
| Provider readiness | Supported provider proves a real production endpoint, and unsupported Aspire-managed sources fail graph construction. External source requires explicit proof. |
| Materialization | Production bindings and exact image/build digest produce the final archive; settings name equals declared name. |
| Upload reconciliation | Exact normalized source identity is no-op; mismatch is collision; missing source uploads once under bounded retries. |
| Composition | Acquires slot after readiness, re-downloads latest FAR and sources, applies declared stage ownership, composes locally, and validates/commits the FAR. |
| Publication identity | Same release tag/identical intent is no-op; same tag/different intent fails; new release is serialized after existing stage work. |
| Resume/approval | Lost response or ephemeral runner resumes by request ID/server record. Approval timeout is failed/indeterminate, and Deploy cannot succeed before terminal Nitro success. Stale approval cannot supersede a later rollout. |
| Partial state | Nitro failure after compute update reports new compute plus old FAR, leaves recoverable state, and does not claim automatic rollback. |
| `aspire do` | Safe reconcile includes compute/readiness dependencies. Export/upload inspection cannot masquerade as completed deployment. |
| Compatibility | Focused suite passes on repository pin 13.1.2 and on 13.4.6 before support is advertised. |

Done means every matching `aspire deploy` executes Fusion reconciliation, can verified-no-op on identical desired state, and cannot complete until Nitro reaches terminal success. It also means the pipeline refuses to construct when exact compute/readiness ordering cannot be proven.

## Remaining questions

1. Which provider is the first supported adapter, and what concrete step proves its production endpoint ready rather than merely submitted?
2. Which Nitro server lookup/idempotency API can recover request ID and phase from `(api, stage, release tag, desired digest)` on a new CI runner?
3. Can the server reject or serialize stale approvals so an older request can never become current after a newer rollout?
4. Which fields form normalized source archive equality, and can packaging APIs expose them without duplicate parsing?
5. Is authoritative stage ownership acceptable for the first integration, or is additive multi-owner composition required?
6. Which unrelated application prerequisites must Nitro cutover wait for when it acts as the global finalizer?
7. How should a same-commit rebuild derive source versions and release IDs when schema content, endpoint bindings, or image digests differ?
8. Which resources can safely opt into automatic child-process export, and which initialization hooks need explicit isolation guidance?

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
