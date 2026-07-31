# Fusion release pipeline example

This sample contains two Hot Chocolate source schemas, a Fusion gateway, an Aspire AppHost, and a
GitHub Actions workflow using the split Fusion release commands.

The AppHost and services reference the current `graphql-platform` checkout. Checked-in
`schema.graphqls` and `schema-settings.json` files make the upload input deterministic.

## Before a real deployment

Every remote value is intentionally fake. Replace:

- `https://nitro.example.invalid`;
- `replace-with-nitro-api-id`; and
- the Development and Test source URLs in both `schema-settings.json` files.

Provide `DEMO_NITRO_API_KEY` as a repository or organization secret for the upload job. Create the
GitHub environments `Development` and `Test`, then configure these environment secrets:

- `DEMO_NITRO_GATEWAY_API_KEY`;
- `DEMO_AZURE_CLIENT_ID`;
- `DEMO_AZURE_TENANT_ID`; and
- `DEMO_AZURE_SUBSCRIPTION_ID`.

Configure `DEMO_NITRO_STAGE`, `DEMO_AZURE_LOCATION`, and `DEMO_AZURE_RESOURCE_GROUP` as environment
variables. `DEMO_NITRO_STAGE` carries the Nitro stage of each GitHub environment and selects one of
the stages the AppHost declares. The upload job needs no stage at all. The sample uses Azure
Container Apps because it contributes the `DeployCompute` steps needed to prove source and gateway
ordering. Development and Test should normally use distinct resource groups.

The sources and gateway use external HTTP ingress. The deployment runner must reach the configured
source URLs for readiness polling. The committed `.invalid` URLs deliberately fail a real release.

The nested `.github/workflows/deploy.yml` is documentation while this example is in the monorepo.
GitHub discovers workflows only from the repository-root `.github/workflows` directory.

## Run locally

From the repository root:

```shell
dotnet restore examples/FusionReleasePipeline/FusionReleasePipeline.slnx
dotnet build examples/FusionReleasePipeline/FusionReleasePipeline.slnx --no-restore
dotnet run --project examples/FusionReleasePipeline/src/AppHost/AppHost.csproj
```

Run mode composes `examples/FusionReleasePipeline/src/Gateway/gateway.far` with the `local`
settings environment and starts:

- gateway at `http://localhost:5100/graphql`;
- products at `http://localhost:5101/graphql`; and
- reviews at `http://localhost:5102/graphql`.

Run mode composes for `local` and injects no `NITRO_*` variables, so the gateway uses its local FAR.
A publish composes for the stage that the `stage` parameter names and passes that same stage to the
gateway as `NITRO_STAGE`.

## Release flow

All jobs use one `RELEASE_TAG`, exposed to the AppHost as `Parameters__tag`. Only the deployment
jobs also set `Parameters__stage`, because an immutable source version serves every stage. The build
job uploads both sources as exact immutable versions:

```shell
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$DEMO_NITRO_API_KEY"

aspire do fusion-upload \
  --apphost examples/FusionReleasePipeline/src/AppHost/AppHost.csproj \
  --non-interactive
```

Development and Test are two stages of one Nitro api, so this single upload serves both. An AppHost
that declares several apis uploads each of them in the same invocation.

The deployment job checks out the same revision and publishes without a manifest or CI artifact:

```shell
export Parameters__stage="$DEMO_NITRO_STAGE"
export Parameters__tag="$RELEASE_TAG"
export Parameters__nitroApiKey="$DEMO_NITRO_API_KEY"
export Parameters__nitroGatewayApiKey="$DEMO_NITRO_GATEWAY_API_KEY"
export Azure__CredentialSource=AzureCli
export Azure__SubscriptionId="$DEMO_AZURE_SUBSCRIPTION_ID"
export Azure__ResourceGroup="$DEMO_AZURE_RESOURCE_GROUP"
export Azure__Location="$DEMO_AZURE_LOCATION"

aspire do fusion-publish \
  --apphost examples/FusionReleasePipeline/src/AppHost/AppHost.csproj \
  --non-interactive
```

`fusion-publish` infers `products` and `reviews` from the AppHost, downloads the exact source
versions `products@RELEASE_TAG` and `reviews@RELEASE_TAG` as a metadata-only preflight. After source
deployment it downloads them again, verifies the same canonical digests, and composes the
Development endpoints.

The Test job uses the same tag with the Test `DEMO_NITRO_STAGE`, which composes the Test endpoints
and publishes to the Test stage.

There is no artifact upload/download between jobs. The Fusion-specific publish steps never export
schemas, upload source versions, write Fusion apply-state files, or resolve Aspire's output-path
service. Exact archives and the FAR remain in bounded invocation memory and are cleared after
completion. Azure deployment dependencies can still write target artifacts and Aspire deployment
state according to the provider configuration. The safe order is exact preflight, source
deployment, exact re-download and composition, source readiness, internal Nitro stage publication,
gateway deployment, then terminal public `fusion-publish`.

The build job has a stable concurrency key for the Nitro API. Each deployment job has a stable key
for its GitHub environment, which is the one stage and Azure target that environment writes.
`cancel-in-progress: false` queues writers. Add external locking when another repository or
deployment system can write the same Nitro stage or compute target.
