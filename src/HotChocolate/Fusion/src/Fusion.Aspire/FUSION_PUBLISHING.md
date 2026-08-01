# Publishing Fusion with Aspire

Fusion releases use Nitro itself as the handoff between the build job and deployment jobs. The
public commands are:

```shell
aspire do fusion-upload
aspire do fusion-publish
```

There is no release manifest parameter and no artifact upload or download between these commands.
Both invocations must evaluate the same AppHost composition and use the same `tag` value.

## AppHost declaration

Each api declares the stages it publishes to. An invocation names one of them:

```csharp
var stage = builder.AddParameter("stage");
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder.AddNitroPublishTarget("nitro")
    .WithNitroCloudUrl("https://api.chillicream.com")
    .WithNitroApiId("products-fusion")
    .WithNitroApiKey(nitroApiKey)
    .WithStageParameter(stage)
    .WithConfigurationTag(tag);

nitro.AddStage("development");
nitro.AddStage("production")
    .WithApproval(waitForApproval: true)
    .WithForce(false);
```

`AddStage` declares a stage of the api. `WithStageParameter` sets the parameter that names the
stage an invocation publishes to, and the publication fails when that parameter names a stage the
api does not declare. `WithConfigurationTag` supplies both the source version and the Fusion
configuration tag, and belongs to the api because one release carries the same tag to every stage.
`WithCompositionEnvironment` selects the environment block in each source's `schema-settings.json`
and defaults to the composition environment of the AppHost followed by the stage name.
`WithApproval` and `WithForce` configure the publication of a single stage. Remote operations have a built-in
15-minute deadline, and approval waits have a built-in two-hour deadline.

Each command needs only the parameters it uses, so `fusion-upload` needs a tag and `fusion-publish`
needs a tag and a stage. Values reach the AppHost through configuration, either as environment
variables or as arguments forwarded after `--`:

```shell
Parameters__tag=v1 aspire do fusion-upload
Parameters__stage=production Parameters__tag=v1 aspire do fusion-publish

aspire do fusion-publish -- --Parameters:stage=production --Parameters:tag=v1
```

Prefer the environment for secrets such as the Nitro API key, because a command line is visible in
the process list.

`WithNitroCloudUrl` and `WithNitroApiId` default to the `Nitro:CloudUrl` and `Nitro:ApiId`
configuration values, or to the `NITRO_CLOUD_URL` and `NITRO_API_ID` environment variables. When
`WithNitroApiKey` is not configured, the credential is resolved from `NITRO_API_KEY` or the Nitro
CLI session.

The effective source name is `SourceSchemaName` when explicitly declared, otherwise the Aspire
resource name. Effective names must be unique and portable path segments.

The artifact step supports all three schema declarations:

- `WithGraphQLSchemaFile` reads the checked-in schema, settings, and optional extensions next to
  the project.
- `WithGraphQLSchemaExport` registers an Aspire process command that runs the Hot Chocolate
  command-line exporter. When its optional schema name is omitted, Hot Chocolate's default schema
  is exported and the expected source name defaults to the Aspire resource name. The command uses
  the project's normal .NET SDK defaults, runs without a launch profile, and follows pipeline
  cancellation. It seeds the isolated export directory with the project's
  `schema-settings.json`, preserving its deployment URLs and environments.
- `WithGraphQLSchemaEndpoint` downloads the schema from the declared endpoint and reads
  `schema-settings.json` from the source project. The endpoint must already be reachable from the
  artifact runner because the publishing pipeline does not start source resources. Configure a
  fixed endpoint target port because Aspire does not allocate dynamic ports in publish mode.

## Upload

The build job checks out the source and uploads once per Nitro api:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

`fusion-upload` depends on `fusion-artifacts`. For every source in the current AppHost composition
it:

1. acquires and validates the schema and settings;
2. creates the portable source archive;
3. assigns the exact version `name@tag`;
4. rejects loopback endpoints for the composition environment of every declared stage; and
5. reconciles that immutable version on the Nitro API.

An immutable source version serves every stage of its api, so upload takes no stage. Each declared
api is uploaded once.

`aspire publish` remains an artifact-only root. It can create local Fusion source artifacts but it
does not resolve a Nitro credential or mutate Nitro. Automation normally invokes `fusion-upload`
directly because the artifact step is already its dependency.

## Publish

Deployment jobs check out the same AppHost revision and use the same tag, but they do not need the
source schema files or a build-job artifact:

```shell
export Parameters__stage=development
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-publish \
  --apphost ./src/AppHost/AppHost.csproj \
  --non-interactive
```

Publish resolves the stage that the stage parameter names, and fails when the api does not declare
it. It infers the complete, sorted source-name set from the current AppHost. Before source compute
is changed it downloads every exact `name@tag` from the Nitro API. A missing source fails
the deployment. This preflight records only identities and canonical content digests, then clears
the archive buffers before provider deployment begins.

After every source provider deploys, composition downloads the exact versions again, rejects any
canonical digest change from preflight, and resolves their settings with the current AppHost
composition options and selected composition environment. Readiness and publication
revalidate the session state and composed FAR digest. Publish never exports a schema, reads a
source checkout, invokes Git, or calls the source-upload API. The Fusion-specific download,
composition, readiness, and publication steps create no Fusion apply-state files and do not resolve
Aspire's output-path service. Provider-contributed deployment dependencies may still write target
artifacts or Aspire deployment state. Source archives are limited to 128,000,000 bytes each. Owned
buffers are cleared after success, failure, or cancellation.

## Ordering

The build-side graph is independent of deployment:

```text
fusion-artifacts -> fusion-upload
```

The deployment graph is:

```text
fusion-download -> source DeployCompute -> fusion-compose -> fusion-readiness

fusion-readiness
  -> fusion-publish-stage
  -> gateway DeployCompute
  -> fusion-publish
```

`fusion-download` is a fail-before-compute, metadata-only preflight. `fusion-compose` performs the
second exact download after source compute. `fusion-publish-stage` is internal. The public
`fusion-publish` step is terminal and completes only after gateway deployment. The broader
`aspire deploy` root requires the same terminal step.

The Aspire adapter recognizes direct `DeployCompute` steps and deployment-target contributed
`DeployCompute` steps. It fails closed when it cannot prove source and gateway compute ordering.

## CI shape

Use one immutable `RELEASE_TAG` for every job in a rollout:

```yaml
env:
  RELEASE_TAG: ${{ github.sha }}

jobs:
  fusion-upload:
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
    needs: fusion-upload
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
```

There is intentionally no artifact handoff. Use stable concurrency keys per Nitro API for upload
and per Nitro stage for publish. Queue writers instead of cancelling them.

## Failure and retry behavior

- An existing `name@tag` with identical canonical content is a successful reconcile.
- An existing `name@tag` with different content is an immutable-version collision and fails.
- A missing exact source during publish fails before source compute deployment.
- A changed AppHost source set, tag, target, downloaded archive, composition environment, or FAR
  fails in-memory session validation.
- A transient source endpoint is polled until the built-in operation deadline.
- Approval rejection, timeout, failed commit, or unverified terminal Nitro state fails publication.

The tag is the rollout identity. Use a new tag whenever the source content or intended rollout
changes, and reuse the same tag only for retries of that identical rollout.
