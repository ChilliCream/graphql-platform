# Publishing Fusion with Aspire

Fusion releases use Nitro itself as the handoff between the build job and deployment jobs. The
public commands are:

```shell
aspire do fusion-upload --environment <name>
aspire do fusion-publish --environment <name>
```

There is no release manifest parameter and no artifact upload or download between these commands.
Both invocations must evaluate the same AppHost composition and use the same `tag` value.

## AppHost declaration

```csharp
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder.AddNitroTarget("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(nitroApiKey);

nitro.AddFusionDeployment("development")
    .ForEnvironment("Development")
    .ToStage("development")
    .WithCompositionEnvironment("development")
    .WithConfigurationTag(tag);

nitro.AddFusionDeployment("test")
    .ForEnvironment("Test")
    .ToStage("test")
    .WithCompositionEnvironment("test")
    .WithConfigurationTag(tag);
```

`ForEnvironment` selects the Aspire invocation. `ToStage` selects the Nitro stage.
`WithCompositionEnvironment` selects the environment block in each source's
`schema-settings.json`. `WithConfigurationTag` supplies both the source version and Fusion
configuration tag.

The effective source name is `SourceSchemaName` when explicitly declared, otherwise the Aspire
resource name. Effective names must be unique and portable path segments.

## Upload

The build job checks out the source and runs one selected, real deployment environment:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./src/AppHost/AppHost.csproj \
  --environment Development \
  --non-interactive
```

`fusion-upload` depends on `fusion-artifacts`. For every source in the selected environment's
current AppHost composition it:

1. acquires and validates the schema and settings;
2. creates the portable source archive;
3. assigns the exact version `name@tag`;
4. rejects loopback endpoints for the selected composition environment; and
5. reconciles that immutable version on the selected Nitro API.

If several deployment environments use the same Nitro cloud URL, API ID, source set, and tag, one
upload serves all of them. Run `fusion-upload` once per distinct Nitro API target otherwise.

`aspire publish` remains an artifact-only root. It can create local Fusion source artifacts but it
does not resolve a Nitro credential or mutate Nitro. Automation normally invokes `fusion-upload`
directly because the artifact step is already its dependency.

## Publish

Deployment jobs check out the same AppHost revision and use the same tag, but they do not need the
source schema files or a build-job artifact:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-publish \
  --apphost ./src/AppHost/AppHost.csproj \
  --environment Development \
  --output-path "$RUNNER_TEMP/aspire-apply/Development" \
  --non-interactive
```

Publish infers the complete, sorted source-name set from the current AppHost. Before source compute
is changed it downloads every exact `name@tag` from the selected Nitro API. A missing source fails
the deployment. Download records an atomic apply state binding the tag, Nitro target, complete
source set, archive paths, and canonical content digests.

Composition revalidates every downloaded archive and resolves its settings with the current
AppHost composition options and selected composition environment. Readiness and publication
revalidate the apply state and composed FAR digest. Publish never exports a schema, reads a source
checkout, invokes Git, or calls the source-upload API.

## Ordering

The build-side graph is independent of deployment:

```text
fusion-artifacts -> fusion-upload
```

The deployment graph is:

```text
fusion-download -> source DeployCompute -> fusion-readiness
               \-> fusion-compose ------/

fusion-readiness
  -> fusion-publish-stage
  -> gateway DeployCompute
  -> fusion-publish
```

`fusion-download` is a fail-before-compute preflight. `fusion-publish-stage` is internal. The public
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
          --environment Development
          --non-interactive

  deploy-development:
    needs: fusion-upload
    env:
      Parameters__tag: ${{ env.RELEASE_TAG }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - run: >-
          aspire do fusion-publish
          --apphost ./src/AppHost/AppHost.csproj
          --environment Development
          --non-interactive
```

There is intentionally no artifact handoff. Use stable concurrency keys per Nitro API for upload
and per Nitro stage for publish. Queue writers instead of cancelling them.

## Failure and retry behavior

- An existing `name@tag` with identical canonical content is a successful reconcile.
- An existing `name@tag` with different content is an immutable-version collision and fails.
- A missing exact source during publish fails before source compute deployment.
- A changed AppHost source set, tag, target, downloaded archive, composition environment, or FAR
  fails apply-state validation.
- A transient source endpoint is polled until the configured operation timeout.
- Approval rejection, timeout, failed commit, or unverified terminal Nitro state fails publication.

The tag is the rollout identity. Use a new tag whenever the source content or intended rollout
changes, and reuse the same tag only for retries of that identical rollout.
