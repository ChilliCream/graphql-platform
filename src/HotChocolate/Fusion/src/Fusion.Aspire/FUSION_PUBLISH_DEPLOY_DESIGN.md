# Fusion publish and deploy with Aspire

## Decision

The canonical release commands are:

```shell
aspire do fusion-upload --environment <name>
aspire do fusion-publish --environment <name>
```

`fusion-upload` exports and uploads immutable source schemas. `fusion-publish` downloads and
verifies the exact uploaded source versions, composes for the selected environment, deploys source
compute, checks readiness, publishes the Fusion configuration to Nitro, deploys the gateway, and
completes its terminal step.

Nitro is the handoff between the two commands. There is no release-manifest parameter, CI artifact
upload/download, source archive handoff, or dependency from `fusion-publish` to `fusion-upload`.
Both invocations evaluate the same AppHost revision and receive the same `tag` parameter.

`aspire publish` remains artifact-only for Fusion. It may create local source inputs but does not
resolve a Nitro credential or mutate Nitro. The broader `aspire deploy` root is supported and
requires the same public `fusion-publish` terminal.

## Aspire integration boundary

The implementation targets the repository's Aspire 13.4 integration. Aspire 13.4 made
`aspire publish` and `aspire deploy` generally available, while the programmatic custom-pipeline
APIs remain experimental and emit `ASPIREPIPELINES001`. Keep those APIs behind the small local
pipeline adapter. Sources: [deployment pipelines](https://aspire.dev/deployment/pipelines/),
[ASPIREPIPELINES001](https://aspire.dev/diagnostics/aspirepipelines001/),
[`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/),
[`aspire deploy`](https://aspire.dev/reference/cli/commands/aspire-deploy/), and
[`aspire do`](https://aspire.dev/reference/cli/commands/aspire-do/).

The adapter recognizes provider steps tagged `DeployCompute`, including steps contributed through
an Aspire `DeploymentTarget`. It does not treat provisioning as successful compute deployment. If
the adapter cannot identify deploy-compute steps for a source or gateway resource, publication
fails closed instead of claiming safe ordering.

## AppHost surface

```csharp
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder
    .AddNitroPublishTarget("nitro")
    .WithNitroCloudUrl("https://api.chillicream.com")
    .WithNitroApiId("products-fusion")
    .WithNitroApiKey(nitroApiKey);

nitro
    .AddStage("production")
    .WithCompositionEnvironment("production")
    .WithApproval(waitForApproval: true)
    .WithForce(false)
    .WithTimeouts(
        operation: TimeSpan.FromMinutes(15),
        approval: TimeSpan.FromHours(2));
```

`AddStage` declares a stage of the api. `WithStageParameter` names the parameter that selects the
stage of an invocation, and an unknown stage name is rejected. `WithCompositionEnvironment` selects
the source-settings environment. `WithConfigurationTag` supplies the immutable source version and
final configuration tag for every stage of the api.

An AppHost can declare several apis, and each api can declare several stages. One invocation
publishes to exactly one stage per api.

## Source declaration and acquisition

Fusion sources come from the resources referenced by the single AppHost composition resource.
Supported acquisition modes are:

1. checked-in `schema.graphqls` and `schema-settings.json` files; and
2. explicit command-line schema export.

Runtime HTTP introspection is not accepted during publication. File-based input is preferred for
deterministic, auditable CI.

The effective source name is the explicitly declared source-schema name when present, otherwise
the Aspire resource name. The implementation validates that effective names are unique, portable,
and exactly match the name in source settings. Duplicate names fail before any path can be
overwritten or any publication can become ambiguous.

Command-line export must validate the expected schema and settings files, not only exit code zero.
It runs without a launch profile, records the project/configuration/framework/runtime inputs, and
rejects missing or empty output.

## Identity

One rollout uses one immutable `tag`. Upload assigns that value to every source in the declared
set, producing exact identities such as:

```text
products@build-842-a1b2c3d4
reviews@build-842-a1b2c3d4
```

Publish uses the same value as the Fusion configuration tag. A Git commit is useful provenance but
is not the public rollout identity. A rebuild, endpoint change, composition change, or different
desired rollout requires a new tag. Retries of the identical rollout reuse the same tag.

Recommended configuration precedence is explicit AppHost values and parameter resources first,
then documented compatibility configuration. Secrets come from CI or a secret provider and are
never written to output or logs.

| Concern | Recommended input |
| --- | --- |
| Cloud URL | Explicit `.WithNitroCloudUrl(...)` HTTPS origin |
| API ID | Explicit `.WithNitroApiId(...)` |
| API key | Secret `ParameterResource` |
| Nitro stages | Explicit `.AddStage(...)` per stage |
| Selected stage | `.WithStageParameter(...)`, resolved per invocation |
| Rollout/source/configuration tag | `builder.AddParameter("tag")` |

## `fusion-upload`

The upload command writes immutable source versions, which every stage of an api shares, so it
needs no stage. It:

1. resolves the current AppHost composition and complete effective source-name set;
2. exports or reads every source and validates schema, settings, extensions, and endpoint binding;
3. materializes every archive as `name@tag`;
4. computes its raw archive digest and canonical Fusion content digest; and
5. reconciles the immutable version on the selected Nitro API.

An existing version with identical canonical content is success. An existing version with
different content is an immutable-version collision and fails. Partial uploads may remain orphaned
if a later source fails, but no Nitro stage is changed by upload.

The build job needs only the release tag:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

An immutable source version serves every stage of its api, so one upload covers all of them. Each
declared api is uploaded once.

## `fusion-publish`

Publish does not need a build-job artifact and does not read source schema files. It uses the
current AppHost only as the authority for:

- exact effective source names;
- target cloud URL and API ID;
- selected stage and composition environment; and
- current composition settings.

The command performs these release-critical phases:

1. select the current environment declaration and resolve `tag`;
2. infer and sort the complete effective source-name set;
3. preflight-download each exact `name@tag`, record its canonical digest, and clear the archive
   buffers before source compute starts;
4. deploy source compute;
5. download the exact versions again and require their canonical digests to match preflight;
6. compose a FAR from only those second-download archives using current AppHost composition settings
   and the selected composition environment;
7. revalidate source digests, composition environment, and FAR digest, then poll the composed
   production endpoints until ready;
8. publish the FAR and exact source references to the selected Nitro stage;
9. wait for approval when configured and verify the terminal Nitro result;
10. deploy gateway compute; and
11. complete the public terminal step.

A missing exact source fails during download before source compute changes. Publish never calls the
source reconciliation API, runs schema export, reads a source checkout, or invokes Git. The
Fusion-specific download, composition, readiness, and publication steps create no Fusion
apply-state directory and do not resolve Aspire's output-path service. Provider-contributed
deployment dependencies may still write target artifacts or Aspire deployment state.

## Pipeline graph and first release

Build-side work is independent:

```text
fusion-artifacts -> fusion-upload
```

Deployment uses this graph:

```text
fusion-download -> source DeployCompute -> fusion-compose -> fusion-readiness

fusion-readiness
  -> fusion-publish-stage
  -> gateway DeployCompute
  -> fusion-publish
```

Every source deploy-compute step depends on `fusion-download`, so the exact Nitro source set is a
fail-before-compute preflight. The preflight retains only identities and canonical digests, not
archive bytes. Composition runs only after every source deploy-compute step, re-downloads the exact
versions, and rejects any digest change from preflight. The internal `fusion-publish-stage` step
runs only after readiness. Gateway deployment depends on stage publication, which is required for a
first release because no gateway can start from Nitro before the first FAR exists. The public
`fusion-publish` terminal depends on gateway deployment and is required by the broader Deploy root.

The ordering is intentionally preflight, source deploy, exact re-download and composition,
readiness, internal Nitro publication, gateway deploy, terminal public publication. Upload is never
in the transitive dependency set of the public publish command.

## Invocation-memory integrity

Each `fusion-publish` invocation owns a private in-memory session shared only by its pipeline step
closures. Preflight retains only small source identities and canonical digests while provider steps
run. Exact archive bytes are downloaded again after source compute, compared with preflight, and
leased while composition, readiness, or publication reads them. Cancellation requests cleanup but
does not zero an actively leased buffer until its reader unwinds. The session retains no credentials
and writes no source archive, state file, apply directory, or composed FAR to disk.
Source archives are limited to 128,000,000 bytes each and 512,000,000 bytes in aggregate.

Compose, readiness, and publish validate:

- configured tag equals recorded tag;
- normalized cloud URL and API ID equal the selected target;
- recorded source names exactly equal the current sorted AppHost set;
- every source version equals the tag;
- every in-memory archive still has its recorded canonical content digest;
- the composition environment still matches the current declaration; and
- the FAR still has its recorded raw digest.

All owned source and FAR buffers are cleared after success, failure, or cancellation. A retry starts
a new isolated session and downloads the exact source versions from Nitro again.

## Readiness, approval, and retries

Readiness comes from the composed gateway settings, not an arbitrary Aspire liveness endpoint.
Loopback URLs are rejected for deployment. A response below HTTP 500 is considered reachable;
transport failures and server errors are retried until the configured operation timeout.

Publication is successful only after Nitro reports a verified terminal result. Approval rejection,
approval timeout, failed commit, polling timeout, or an unverified terminal state fails the step.

Retry rules:

- same `name@tag` and same canonical content: upload no-op success;
- same `name@tag` and different content: collision failure;
- missing `name@tag`: publish failure before compute;
- changed AppHost source set, target, tag, downloaded content, environment, or FAR: integrity
  failure; and
- transient readiness/Nitro polling failure: retry the same rollout with the same tag after the
  cause is resolved.

Serialize writers per Nitro API during upload and per Nitro stage during publication. Queue writers
instead of cancelling them. Repository-local concurrency is insufficient when another repository
or deployment system can write the same target.

## CI shape

All jobs check out the same revision and receive the same `RELEASE_TAG`:

```yaml
env:
  RELEASE_TAG: ${{ github.sha }}

jobs:
  upload:
    env:
      Parameters__tag: ${{ env.RELEASE_TAG }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - run: >-
          aspire do fusion-upload
          --apphost ./src/AppHost/AppHost.csproj
          --non-interactive

  deploy-development:
    needs: upload
    env:
      Parameters__stage: development
      Parameters__tag: ${{ env.RELEASE_TAG }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - run: >-
          aspire do fusion-publish
          --apphost ./src/AppHost/AppHost.csproj
          --non-interactive

  deploy-test:
    needs: deploy-development
    env:
      Parameters__stage: test
      Parameters__tag: ${{ env.RELEASE_TAG }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - run: >-
          aspire do fusion-publish
          --apphost ./src/AppHost/AppHost.csproj
          --non-interactive
```

There is deliberately no artifact upload/download step and no public `releaseId` or manifest
parameter.

## Verification matrix

The implementation is complete only when focused tests and a real materialized AppHost prove:

| Scenario | Required result |
| --- | --- |
| Environment selection | Only the matching declaration is used; ambiguous mappings fail. |
| Complete source set | Duplicate effective names and missing exact downloads fail. |
| Cross-runner publish | Publish succeeds with AppHost metadata and Nitro downloads but no schema files, Git metadata, or upload artifact. |
| Fusion-only disk behavior | Fusion download, composition, readiness, and publication succeed without resolving `IPipelineOutputService` or writing source archives, apply state, or a FAR. Provider dependencies own their target output and deployment state. |
| Invocation isolation | Interleaved environments and repeated deployments use separate sessions with no retained state. |
| Cleanup | Success, failure, and cancellation clear all owned source and FAR buffers. |
| Memory bounds | Oversized individual sources, aggregate sources, and FAR output fail with explicit diagnostics. |
| Environment composition | The same `name@tag` archives compose different Development/Test endpoints. |
| Integrity | Tag, target, source-set, archive, environment, and FAR drift fail. |
| Provider ordering | Source deploy waits for download; readiness waits for source compute; gateway waits for Nitro publication. |
| First release | Nitro stage publication precedes gateway deployment. |
| Command surface | Real `aspire do --list-steps` exposes `fusion-upload` and terminal `fusion-publish`. |
| Compatibility | Build and focused tests pass for the repository's supported target frameworks. |
