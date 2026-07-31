# Promoting Fusion releases with Aspire

This package supports a build-once, deploy-many Fusion release. One build job exports the source
schemas, creates immutable source-schema archives, uploads them to Nitro, and emits a portable
release manifest. Environment deployment jobs consume that exact manifest, download and verify the
uploaded archives, compose with the selected `schema-settings.json` environment, wait for deployed
services, and publish the resulting Fusion archive.

The promoted-manifest workflow is opt-in. Deployments without
`WithFusionReleaseManifest(...)` continue to use the legacy same-runner workflow.
The examples use Aspire CLI 13.4.6, whose AppHost selector is `--apphost`.

## AppHost declaration

Declare the release ID and manifest path as external parameters. The manifest path is required only
by deployment jobs and must resolve to an absolute path.

```csharp
var releaseId = builder.AddParameter("releaseId");
var releaseManifest = builder.AddParameter("fusionReleaseManifest");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder.AddNitroTarget("nitro")
    .WithCloudUrl("https://api.chillicream.com")
    .WithApiId("products-fusion")
    .WithApiKey(nitroApiKey);

nitro.AddFusionDeployment("staging")
    .ForEnvironment("Staging")
    .ToStage("staging")
    .WithCompositionEnvironment("staging")
    .WithConfigurationTag(releaseId)
    .WithFusionReleaseManifest(releaseManifest);

nitro.AddFusionDeployment("production")
    .ForEnvironment("Production")
    .ToStage("production")
    .WithCompositionEnvironment("production")
    .WithConfigurationTag(releaseId)
    .WithFusionReleaseManifest(releaseManifest)
    .WithApproval(waitForApproval: true);
```

`ForEnvironment` selects an Aspire deployment. `ToStage` selects the Nitro stage.
`WithCompositionEnvironment` selects the exact case-sensitive key under
`schema-settings.json.environments`. When it is omitted, an existing composition
`EnvironmentName` is used, then the Nitro stage name is the default.

The source archives are shared across these declarations. For example:

```json
{
  "name": "products",
  "transports": {
    "http": {
      "url": "{{PRODUCTS_URL}}/graphql"
    }
  },
  "environments": {
    "staging": {
      "PRODUCTS_URL": "https://products.staging.example.com"
    },
    "production": {
      "PRODUCTS_URL": "https://products.example.com"
    }
  }
}
```

The release contains this settings document once. Staging and production resolve different gateway
settings while retaining the same source name, version, and content digest.

Promoted releases use the release ID as the source version. The legacy
`WithDefaultSourceVersionFromGitCommit()` option applies only to deployments without a release
manifest; apply never invokes Git to rediscover a promoted version.

## Build and upload

The build job runs the named upload step:

```bash
export Parameters__releaseId="$RELEASE_ID"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-upload \
  --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Release \
  --output-path "$RUNNER_TEMP/fusion-release" \
  --non-interactive
```

`fusion-upload` depends on `fusion-artifacts`. This single command therefore:

1. discovers the composition source schemas;
2. reads checked-in schema files or executes explicitly configured command-line exporters once;
3. creates one immutable source archive per source;
4. computes both the archive SHA-256 and the normalized Fusion content SHA-256;
5. reconciles each source version into every distinct declared Nitro API target;
6. writes the final manifest only after all uploads are verified.

The final release is:

```text
<output-path>/
  fusion/
    releases/
      <release-id>/
        fusion-release.json
        fusion-release.draft.json
        sources/
          products/
            <release-id>.zip
          reviews/
            <release-id>.zip
```

Promote only `fusion/releases/<release-id>/fusion-release.json` as the deployable CI artifact. The
source ZIPs and draft manifest are local build evidence used by `fusion-upload`; manifest apply
never reads them. Instead, apply downloads every exact source version from Nitro and verifies its
normalized content digest. The final manifest contains portable relative archive paths so the full
release directory can still be retained for audit purposes, but those paths are not apply inputs.
It never contains credentials, project paths, working directories, stages, Aspire environments, or
timestamps.

`aspire publish` remains local-only. It runs `fusion-artifacts` and emits the archives plus draft
manifest, but it does not resolve a Nitro credential or perform an upload. Automation normally uses
`aspire do fusion-upload` directly because the artifact step is already its dependency.

## Deploy on a separate runner

Download the CI artifact into any read-only or otherwise stable directory. Pass the exact final
manifest path through the configured parameter and use a separate output directory for the apply:

```bash
export Parameters__releaseId="$RELEASE_ID"
export Parameters__fusionReleaseManifest="$RUNNER_TEMP/promoted-release/fusion-release.json"
export Parameters__nitroApiKey="$NITRO_API_KEY"

aspire do fusion-publish \
  --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Staging \
  --output-path "$RUNNER_TEMP/aspire-apply/Staging" \
  --non-interactive
```

Production uses the identical downloaded manifest:

```bash
aspire do fusion-publish \
  --apphost ./MyApp.AppHost/MyApp.AppHost.csproj \
  --environment Production \
  --output-path "$RUNNER_TEMP/aspire-apply/Production" \
  --non-interactive
```

The executor reads exactly `Parameters__fusionReleaseManifest`; it does not scan or reuse
`--output-path` to discover a release. It validates each manifest archive path as a portable
release-relative path but does not open or copy that local archive during apply. Downloaded sources,
apply state, and the composed FAR are written beneath the fresh apply output:

```text
<apply-output>/
  fusion/
    apply/
      <deployment-name>/
        fusion-apply.json
        fusion-configuration.far
        sources/
```

The downloaded release manifest is never regenerated or overwritten. A deployment runner still
needs to evaluate the same pinned AppHost model so Aspire can discover the Fusion declarations and
provider deployment steps. Supply either a buildable AppHost checkout or the same promoted AppHost
binary and locked dependencies with the appropriate Aspire `--no-build` workflow. A compute
provider may still require its own workload source, container context, or promoted image. The
Fusion apply graph itself does not need the source-schema files, the checkout used for schema
export, Git metadata, a schema exporter, or the build runner's local source ZIPs.

## GitHub Actions promotion and writer serialization

This example uploads only the final manifest, downloads it into a different runner path, and
serializes each Nitro stage writer:

```yaml
jobs:
  fusion-build:
    runs-on: ubuntu-latest
    env:
      RELEASE_ID: ${{ github.sha }}
      Parameters__releaseId: ${{ github.sha }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - name: Build and upload immutable Fusion sources
        run: >-
          aspire do fusion-upload
          --apphost ./MyApp.AppHost/MyApp.AppHost.csproj
          --environment Release
          --output-path "$RUNNER_TEMP/fusion-release"
          --non-interactive
      - uses: actions/upload-artifact@v4
        with:
          name: fusion-release-manifest
          path: ${{ runner.temp }}/fusion-release/fusion/releases/${{ env.RELEASE_ID }}/fusion-release.json
          if-no-files-found: error

  fusion-deploy:
    needs: fusion-build
    runs-on: ubuntu-latest
    strategy:
      matrix:
        include:
          - environment: Staging
            writer-key: api.chillicream.com|products-fusion|staging
          - environment: Production
            writer-key: api.chillicream.com|products-fusion|production
    concurrency:
      group: fusion-${{ matrix.writer-key }}
      cancel-in-progress: false
    env:
      Parameters__releaseId: ${{ github.sha }}
      Parameters__nitroApiKey: ${{ secrets.NITRO_API_KEY }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/download-artifact@v4
        with:
          name: fusion-release-manifest
          path: ${{ runner.temp }}/promoted-release
      - name: Publish promoted Fusion release
        env:
          Parameters__fusionReleaseManifest: ${{ runner.temp }}/promoted-release/fusion-release.json
        run: >-
          aspire do fusion-publish
          --apphost ./MyApp.AppHost/MyApp.AppHost.csproj
          --environment "${{ matrix.environment }}"
          --output-path "$RUNNER_TEMP/aspire-apply/${{ matrix.environment }}"
          --non-interactive
```

Each `writer-key` must be a stable encoding of the canonical `(Nitro cloud origin, API ID, stage)`
tuple. `cancel-in-progress: false` queues writers instead of cancelling an in-flight approval. This
package rejects duplicate declarations for the same canonical target and stage within one selected
AppHost environment. It does not provide a distributed lock across workflow runs, repositories, or
other deployment systems. Keep the CI concurrency control, and require server-side Nitro
serialization or an external coordinator when writers can originate outside that one concurrency
domain.

## Command contract

| Command | Promoted-manifest behavior | Nitro effect |
| --- | --- | --- |
| `aspire publish` | Exports once and writes source archives plus a draft manifest. | None. |
| `aspire do fusion-upload` | Runs artifacts, reconciles immutable versions into all declared targets, and finalizes `fusion-release.json`. | Source upload only. |
| `aspire do fusion-publish` | Reads the exact manifest parameter, downloads and verifies sources, deploys the sources, composes for the selected environment, waits for readiness, publishes the stage, deploys the gateway, and completes. | Stage publication only, never source upload. |
| `aspire deploy --environment <name>` | Runs provider deployment and requires the same manifest apply graph to complete. | The same ordered source deployment, stage publication, and gateway deployment. |

## Pipeline graphs

Build and upload:

```text
fusion-artifacts -> fusion-upload
       |
       +-----------------> Aspire Publish
```

Promoted-manifest apply:

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
                                  Aspire Deploy
```

`fusion-publish` has no transitive dependency on `fusion-artifacts` or `fusion-upload` in manifest
mode. Consequently neither the named publish step nor `aspire deploy` can invoke source export,
archive materialization from a checkout, Git version discovery, or source upload.
The named publish step includes the provider build, push, and deploy graph for every referenced
source and for the gateway. The order is source deployment, readiness, internal Nitro stage
publication, gateway deployment, then the terminal public `fusion-publish` step. `aspire deploy`
is the broader AppHost root and requires that same terminal step.

Legacy apply, used only when no manifest parameter is configured:

```text
fusion-artifacts -> fusion-upload
fusion-artifacts -> fusion-readiness <- source DeployCompute
fusion-upload + fusion-readiness -> fusion-publish-stage
fusion-publish-stage -> gateway DeployCompute
gateway DeployCompute -> fusion-publish -> Aspire Deploy
```

Do not mix manifest and legacy deployments for the same Aspire environment.

## Manifest integrity and target binding

`fusion-release.json` is written atomically after upload verification. It binds:

- the manifest format version, release ID, and exact Fusion composition tool version;
- composition options and their digest;
- the complete source set digest;
- each source name, immutable version, relative archive path, archive SHA-256, and normalized content
  SHA-256;
- every Nitro cloud URL and API ID to which the complete source set was uploaded.

Apply rejects unsupported formats, invalid or duplicate entries, path traversal, composition or
source-set digest changes, an unexpected release ID, an incompatible composition tool version, and
a target that is not recorded in the manifest. It downloads each exact `name@version` from Nitro
and verifies normalized content before writing its private apply cache. The cache is verified again
before composition. Build and apply should use the same promoted AppHost binary and locked package
set; the manifest's exact composition-tool identity makes a mismatched apply fail before download
or composition.

The raw archive digest protects the build artifact. The normalized content digest protects the
Nitro identity even if ZIP container bytes differ while schema, settings, and extensions remain
identical.

The manifest itself is immutable. Retrying the same release ID against an output directory that
already contains its final manifest intentionally reuses that final manifest before inspecting the
current checkout. This makes a lost-response retry stable even if the working tree changed. A new
checkout, source set, composition configuration, tool version, or intended release requires a new
release ID. A different manifest cannot overwrite an existing `fusion-release.json` at the same
release path, and an existing final manifest cannot be extended to a different target set.

## Publication behavior

Composition runs before readiness so environment variables in shared source settings are resolved
first. Readiness reads the composed FAR gateway settings and probes the final
`sourceSchemas.*.transports.http.url` values after every referenced source's provider deployment
terminal. Transient DNS, connection, request-timeout, and HTTP 5xx failures are retried with a
bounded delay until the deployment's operation timeout expires. Any response below HTTP 500 is
accepted, preserving the endpoint's ability to use authentication or method-specific status codes
as liveness evidence. A deadline failure reports both the source and endpoint.

The adapter prefers a `DeployCompute` step associated directly with the resource, then resolves
Aspire 13.4's materialized deployment target and selects its `DeployCompute` step. It applies the
same rule to the gateway. Publication fails closed when any managed source or gateway has no
provable deployment terminal. Infrastructure provisioning alone is not treated as a completed
compute deployment. The gateway deployment depends on successful internal stage publication, and
the public `fusion-publish` terminal depends on the gateway deployment.

Publication then uses the already composed FAR and the exact source references from the manifest.
It preserves the configured approval, force, operation-timeout, and approval-timeout behavior.
The stage reaches a verified terminal Nitro result or the Aspire step fails.

Aspire environment selection remains exact and case-sensitive. `fusion-publish` and `aspire deploy`
perform no Nitro stage publication when the selected environment has no matching Fusion
deployment. The dedicated build invocation is intentional exception: when promoted-manifest
declarations exist, `aspire do fusion-upload --environment Release` can use an otherwise unmatched
build environment to create and upload the shared source set to every distinct declared Nitro API
target. Running `aspire deploy --environment Release` does not attach that build-only upload step to
Deploy.
