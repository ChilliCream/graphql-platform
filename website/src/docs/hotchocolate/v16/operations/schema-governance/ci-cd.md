---
title: Automate schema governance in CI/CD
---

Automate schema governance for Hot Chocolate v16 so every pull request proves your server builds, the exported schema is intentional, schema changes are compatible with the target stage, and trusted-document clients still work. During release, publish the approved schema and client operation artifacts.

This page focuses on a single Hot Chocolate server API. Fusion composition and source-schema publishing require [`nitro fusion`](/docs/nitro/cli/fusion) workflows and are not covered here.

# Prerequisites

Before adding CI/CD steps, make sure you have:

| Requirement                           | Purpose                                                           | How to verify                                                       |
| ------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------- |
| Hot Chocolate v16 server project      | Export SDL from the same app that serves traffic.                 | `dotnet build` succeeds.                                            |
| `HotChocolate.AspNetCore.CommandLine` | Adds `schema export`, `schema list`, and `schema print` commands. | `dotnet run --project src/MyApi -- schema list` exits with `0`.     |
| `RunWithGraphQLCommandsAsync(args)`   | Returns command exit codes to CI.                                 | Schema command failures fail the job.                               |
| Nitro API and stages                  | Compare candidate artifacts with active stage state.              | Stages like `Dev`, `Staging`, and `Prod` exist.                     |
| Nitro client per application          | Validate and publish trusted documents per client.                | You have a `NITRO_CLIENT_ID` for each client.                       |
| Nitro API key                         | Authenticate CI without interactive login.                        | `NITRO_API_KEY` is stored as a CI secret.                           |
| Nitro CLI or official GitHub Actions  | Validate, upload, and publish artifacts in pipelines.             | `nitro --help` works, or workflows use `ChilliCream/nitro-*` steps. |
| Client operation extraction           | Provide a JSON operations file for client validation.             | Relay, Strawberry Shake, or another build step writes operations.   |

Integrate the command-line package in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Check that schema commands work:

```bash
dotnet run --project ./src/MyApi -- schema list
```

Expected output:

```text
_Default
```

Use CI variables and secrets consistently:

| Name                  | Store as           | Purpose                                              |
| --------------------- | ------------------ | ---------------------------------------------------- |
| `NITRO_API_ID`        | Variable or secret | API id for schema commands.                          |
| `NITRO_CLIENT_ID`     | Variable or secret | Client id for client commands. Use one per client.   |
| `NITRO_API_KEY`       | Secret             | API key or PAT for non-interactive Nitro calls.      |
| `NITRO_STAGE`         | Variable           | Target stage for validation or publish.              |
| `NITRO_CLOUD_URL`     | Variable or secret | Optional custom Nitro backend URL.                   |
| `NITRO_OUTPUT_FORMAT` | Variable           | Set to `json` for machine-readable Nitro CLI output. |

Prefer API-scoped keys. Use stage-scoped keys if separate workflows publish to different stages, such as a `Dev` key for CI and a `Prod` key for releases. Nitro only shows an API key secret once when you create it.

# Understand the pipeline shape

Validation and publishing are separate. Pull requests validate candidate changes. Releases upload immutable versions and publish them to stages.

| Phase              | Command or action                              | Publishes? | Failure blocks                                   | Artifact               |
| ------------------ | ---------------------------------------------- | ---------- | ------------------------------------------------ | ---------------------- |
| Build and test     | `dotnet build`, `dotnet test`                  | No         | Code, resolver, and snapshot regressions         | Test results           |
| Export schema      | `dotnet run -- schema export`                  | No         | Schema build and export failures                 | `schema.graphqls`      |
| Validate schema    | `nitro schema validate`                        | No         | Breaking or rejected schema changes              | Candidate SDL          |
| Extract operations | `yarn relay`, `dotnet build ./src/MyClient`    | No         | Client build failures                            | `operations.json`      |
| Validate client    | `nitro client validate`                        | No         | Operations incompatible with active stage schema | Candidate operations   |
| Upload release     | `nitro schema upload`, `nitro client upload`   | No         | Duplicate tags or invalid artifacts              | Version tags           |
| Publish stage      | `nitro schema publish`, `nitro client publish` | Yes        | Stage gates, rejected changes, approval timeout  | Active stage versions  |
| Deploy app         | Your deployment system                         | No         | Deployment health checks                         | Server/client binaries |

A Nitro stage represents an environment. Each stage has one active schema and multiple active client versions. Schema and client versions are separate artifacts, but using the same release tag (like `v1.4.0` or a Git SHA) makes rollback and audit predictable.

# Copy the GitHub Actions workflow shape

Start with two workflows or jobs: one for pull request validation, one for release promotion. The following examples use the Nitro CLI, but the commands work in other CI systems as well.

## Validate pull requests

```yaml
name: GraphQL governance

on:
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  validate-graphql:
    runs-on: ubuntu-latest
    # Secret-backed Nitro calls are not safe for untrusted fork PRs unless
    # your repository has a separate secret strategy.
    if: github.event.pull_request.head.repo.full_name == github.repository
    steps:
      - uses: actions/checkout@v6

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.x

      - uses: actions/setup-node@v6
        with:
          node-version: 24

      - name: Restore, build, and test
        run: |
          dotnet restore
          dotnet build --no-restore
          dotnet test --no-build

      - name: Export schema
        run: |
          mkdir -p artifacts
          dotnet run --project ./src/MyApi -- \
            schema export \
            --output ./artifacts/schema.graphqls

      - name: Build client operations
        run: |
          cd src/MyWebClient
          yarn install --immutable
          yarn relay
          cp persisted_queries.json ../../artifacts/operations.json

      - name: Validate schema against Nitro
        env:
          NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}
          NITRO_API_ID: ${{ vars.NITRO_API_ID }}
          NITRO_STAGE: Dev
        run: |
          nitro schema validate \
            --api-id "$NITRO_API_ID" \
            --stage "$NITRO_STAGE" \
            --schema-file ./artifacts/schema.graphqls \
            --output json

      - name: Validate client operations against Nitro
        env:
          NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}
          NITRO_CLIENT_ID: ${{ vars.NITRO_CLIENT_ID }}
          NITRO_STAGE: Dev
        run: |
          nitro client validate \
            --client-id "$NITRO_CLIENT_ID" \
            --stage "$NITRO_STAGE" \
            --operations-file ./artifacts/operations.json \
            --output json

      - name: Upload governance artifacts
        uses: actions/upload-artifact@v6
        with:
          name: graphql-governance
          path: |
            artifacts/schema.graphqls
            artifacts/schema-settings.json
            artifacts/operations.json
```

Expected result: compatible pull requests pass. Pull requests with unsafe schema changes or invalid client operations fail before merge.

To add pull request feedback from Nitro, use the official validation actions and grant `pull-requests: write`:

```yaml
permissions:
  contents: read
  pull-requests: write

steps:
  - name: Validate schema
    uses: ChilliCream/nitro-schema-validate@v16.0.0-rc.1.43
    with:
      api-id: ${{ vars.NITRO_API_ID }}
      stage: Dev
      schema-file: ./artifacts/schema.graphqls
      api-key: ${{ secrets.NITRO_API_KEY }}
      comment-mode: review

  - name: Validate client
    uses: ChilliCream/nitro-client-validate@v16.0.0-rc.1.43
    with:
      client-id: ${{ vars.NITRO_CLIENT_ID }}
      stage: Dev
      operations-file: ./artifacts/operations.json
      api-key: ${{ secrets.NITRO_API_KEY }}
      comment-mode: review
```

## Promote releases

```yaml
name: GraphQL release

on:
  push:
    tags:
      - "v*"

permissions:
  contents: read

jobs:
  publish-graphql-artifacts:
    runs-on: ubuntu-latest
    env:
      NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}
      NITRO_API_ID: ${{ vars.NITRO_API_ID }}
      NITRO_CLIENT_ID: ${{ vars.NITRO_CLIENT_ID }}
      RELEASE_TAG: ${{ github.ref_name }}
    steps:
      - uses: actions/checkout@v6

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.x

      - uses: actions/setup-node@v6
        with:
          node-version: 24

      - name: Export schema and client operations
        run: |
          mkdir -p artifacts
          dotnet run --project ./src/MyApi -- schema export --output ./artifacts/schema.graphqls
          cd src/MyWebClient
          yarn install --immutable
          yarn relay
          cp persisted_queries.json ../../artifacts/operations.json

      - name: Upload immutable versions
        run: |
          nitro schema upload \
            --api-id "$NITRO_API_ID" \
            --tag "$RELEASE_TAG" \
            --schema-file ./artifacts/schema.graphqls

          nitro client upload \
            --client-id "$NITRO_CLIENT_ID" \
            --tag "$RELEASE_TAG" \
            --operations-file ./artifacts/operations.json

      - name: Publish to Dev
        run: |
          nitro schema publish --api-id "$NITRO_API_ID" --tag "$RELEASE_TAG" --stage Dev
          nitro client publish --client-id "$NITRO_CLIENT_ID" --tag "$RELEASE_TAG" --stage Dev

      - name: Publish to Prod after approval
        run: |
          nitro schema publish \
            --api-id "$NITRO_API_ID" \
            --tag "$RELEASE_TAG" \
            --stage Prod \
            --wait-for-approval

          nitro client publish \
            --client-id "$NITRO_CLIENT_ID" \
            --tag "$RELEASE_TAG" \
            --stage Prod \
            --wait-for-approval
```

Expected result: Nitro contains schema and client versions tagged with the release tag. Publishing makes those versions active for the selected stage.

You can use the equivalent upload and publish actions, which use the same input names:

```yaml
- uses: ChilliCream/nitro-schema-upload@v16.0.0-rc.1.43
  with:
    api-id: ${{ vars.NITRO_API_ID }}
    tag: ${{ github.ref_name }}
    schema-file: ./artifacts/schema.graphqls
    api-key: ${{ secrets.NITRO_API_KEY }}

- uses: ChilliCream/nitro-client-upload@v16.0.0-rc.1.43
  with:
    client-id: ${{ vars.NITRO_CLIENT_ID }}
    tag: ${{ github.ref_name }}
    operations-file: ./artifacts/operations.json
    api-key: ${{ secrets.NITRO_API_KEY }}

- uses: ChilliCream/nitro-schema-publish@v16.0.0-rc.1.43
  with:
    api-id: ${{ vars.NITRO_API_ID }}
    tag: ${{ github.ref_name }}
    stage: Prod
    wait-for-approval: true
    api-key: ${{ secrets.NITRO_API_KEY }}

- uses: ChilliCream/nitro-client-publish@v16.0.0-rc.1.43
  with:
    client-id: ${{ vars.NITRO_CLIENT_ID }}
    tag: ${{ github.ref_name }}
    stage: Prod
    wait-for-approval: true
    api-key: ${{ secrets.NITRO_API_KEY }}
```

# Build, test, and snapshot the schema before registry calls

Run local tests before any Nitro validation that depends on the network. A schema snapshot test catches accidental SDL changes and provides reviewers with a diff in source control.

```csharp
// Tests/SchemaTests.cs
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace MyApi.Tests;

public class SchemaTests
{
    [Fact]
    public async Task Schema_Should_MatchSnapshot_When_SchemaChanges()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act and assert
        executor.Schema.MatchSnapshot();
    }
}
```

Run this test before exporting the schema:

```bash
dotnet test --no-build
```

Expected result: the first run creates a snapshot. Later runs fail with a diff if the SDL changes. Review the diff, decide if the change is intentional, and update the snapshot in the same pull request.

Use both snapshot tests and Nitro validation. The snapshot shows when your local schema shape changes. Nitro checks if the candidate schema is compatible with the active schema and clients on a stage.

# Export a deterministic schema artifact

Export the SDL from your configured server project:

```bash
mkdir -p artifacts
dotnet run --project ./src/MyApi -- schema export --output ./artifacts/schema.graphqls
```

Expected output:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

Use `./artifacts/schema.graphqls` as input for Nitro schema commands. The `schema-settings.json` file contains tool metadata. For single-API Nitro schema validation, only the SDL file is needed.

The `schema export` command writes files. If you omit `--output`, it writes `schema.graphqls` and `schema-settings.json` to the working directory. Use `schema print` to output SDL to stdout for debugging:

```bash
dotnet run --project ./src/MyApi -- schema print --schema-name _Default
```

If your server registers more than one request executor, list schemas and export by name:

```bash
dotnet run --project ./src/MyApi -- schema list
dotnet run --project ./src/MyApi -- \
  schema export \
  --schema-name Catalog \
  --output ./artifacts/catalog.graphqls
```

Use `--semantic-non-null` only if a downstream client still requires SDL with `@semanticNonNull` annotations:

```bash
dotnet run --project ./src/MyApi -- \
  schema export \
  --semantic-non-null \
  --output ./artifacts/schema.semantic.graphqls
```

To keep exports stable:

- Export from the same environment that defines your deployment contract.
- Pin the .NET SDK and Hot Chocolate package versions.
- Avoid schema registration that depends on time, random values, culture, local connection strings, or CI-only feature flags.
- Keep dynamic schema sources ordered deterministically.
- Log `schema list` in CI if you have multiple schemas.

# Validate schema changes in pull requests

Validate the exported SDL against the target stage:

```bash
nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "$NITRO_STAGE" \
  --schema-file ./artifacts/schema.graphqls \
  --output json
```

You must provide `--api-id`, `--stage`, `--schema-file`, and authenticate with `NITRO_API_KEY` or `--api-key`. The `--output json` flag disables prompts and produces machine-readable output.

Expected result: compatible changes exit with `0`. Unsafe changes exit non-zero and report the rejected changes. Run this once per target stage if a pull request can affect multiple release trains.

Schema validation answers: can this candidate schema become active for this stage without breaking the stage contract or active registered clients? This command does not publish the schema.

Do not use `nitro fusion` commands in this workflow. Fusion uses a separate composition model.

# Extract trusted documents from clients

Trusted documents are client artifacts. Build them from the same operations your client sends at runtime.

For Relay, you can write a JSON map of operation hash to operation text:

```js
// relay.config.js
module.exports = {
  src: "./src",
  schema: "./schema.graphqls",
  persistConfig: {
    file: "./persisted_queries.json",
    algorithm: "MD5",
  },
};
```

Build the client and copy the file to your shared artifact directory:

```bash
cd src/MyWebClient
yarn relay
cp persisted_queries.json ../../artifacts/operations.json
```

Expected output:

```json
{
  "913abc361487c481cf6015841c0eca22": "query Viewer { viewer }",
  "0e7cf2125e8eb711b470cc72c73ca77e": "query Product($id: ID!) { productById(id: $id) { name } }"
}
```

The hash algorithm and encoding must match the server. Relay uses MD5 by default, which Hot Chocolate supports out of the box. If you use SHA-1, SHA-256, Base64, or Hex, keep the client manifest, Nitro upload, and server document hash provider in sync.

Strawberry Shake can also produce persisted operation artifacts as part of the client build. Follow the Strawberry Shake documentation, then pass the generated operations file to `nitro client validate` and `nitro client upload`.

If your server loads operations from Nitro at runtime, configure it with `ChilliCream.Nitro`, `AddNitro()`, and `UsePersistedOperationPipeline()`. See [trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and [First-Party API](/docs/hotchocolate/v16/guides/private-api) for strict server lock-down.

# Validate client operations in pull requests

Validate each changed client against the stage schema:

```bash
nitro client validate \
  --client-id "$NITRO_CLIENT_ID" \
  --stage "$NITRO_STAGE" \
  --operations-file ./artifacts/operations.json \
  --output json
```

Expected result: valid operations exit with `0`. Operations that no longer match the active stage schema exit non-zero.

Client validation answers the opposite question from schema validation: do these candidate operations work against the schema already active on the stage? It does not replace schema validation. Run both when a pull request changes the server schema and client operations.

For multiple clients, use separate client ids and operation files:

```bash
nitro client validate --client-id "$WEB_CLIENT_ID" --stage Dev --operations-file artifacts/web-operations.json
nitro client validate --client-id "$MOBILE_CLIENT_ID" --stage Dev --operations-file artifacts/mobile-operations.json
```

Do not reuse the API id for client commands. Client commands require `--client-id`.

# Upload immutable release artifacts

After validation succeeds and before publishing to a stage, upload your release artifacts:

```bash
RELEASE_TAG="${GITHUB_REF_NAME:-local-dev}"

nitro schema upload \
  --api-id "$NITRO_API_ID" \
  --tag "$RELEASE_TAG" \
  --schema-file ./artifacts/schema.graphqls

nitro client upload \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "$RELEASE_TAG" \
  --operations-file ./artifacts/operations.json
```

Expected result: Nitro contains unpublished schema and client versions with the release tag.

Uploading does not make a version active for traffic. Treat duplicate tag failures as a release decision point. If reruns must be idempotent, check for an existing version before uploading. If your process treats tags as immutable, fail loudly and create a new tag.

Use the same tag for schema and client artifacts produced by the same build. Keep CI artifact names, container image tags, schema tags, and client tags aligned so rollback can find the matching known-good set.

# Publish to stages and gate deployments

Publishing activates an uploaded version for a stage:

```bash
nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "$RELEASE_TAG" \
  --stage Prod \
  --wait-for-approval

nitro client publish \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "$RELEASE_TAG" \
  --stage Prod \
  --wait-for-approval
```

Expected result: Nitro creates deployment entries, waits for approval if the stage requires it, and marks the release tag as active after approval.

Use stage-specific jobs and secrets when possible:

| Stage     | Publish pattern                    | Notes                                               |
| --------- | ---------------------------------- | --------------------------------------------------- |
| `Dev`     | Auto-publish after merge           | Use a dev-scoped key and fast feedback.             |
| `Staging` | Publish with approval              | Validate release candidate state before production. |
| `Prod`    | Publish with approval, then deploy | Gate the deployment job on successful publish jobs. |

`--force` skips prompts and can publish breaking versions. Reserve it for documented emergency procedures. `--force` and `--wait-for-approval` cannot be used together.

If your server is locked to trusted documents, publish client versions before or alongside the app deployment so new operation IDs are available when traffic arrives. For schema removals, use an additive release first, deploy updated clients, keep old client versions published until traffic drains, then remove deprecated fields in a later release.

# Version environments and rolling deployments

Plan your stage state before a rollout. Each stage has one active schema but can have several active client versions.

| Environment | Active schema tag | Active client tags                        | Deployment state                          |
| ----------- | ----------------- | ----------------------------------------- | ----------------------------------------- |
| `dev`       | `v1.5.0`          | `web-v1.5.0`                              | New build receives test traffic.          |
| `staging`   | `v1.4.2`          | `web-v1.4.2`, `web-v1.5.0-rc`             | Release candidate is being verified.      |
| `prod`      | `v1.4.2`          | `web-v1.4.1`, `web-v1.4.2`, `mobile-v8.2` | Rolling or mobile traffic still overlaps. |

During a web rollout, old and new bundles can both send operations. For mobile, old versions may stay active for weeks or months. Keep old client versions published until telemetry or deployment data shows traffic has drained.

Never publish a schema that removes fields still used by any active client version. Use Nitro stages to represent environment differences, not runtime schema shape changes based on environment-specific registrations.

# Roll back safely

Use these commands to recover from a bad deployment:

```bash
nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "<previous>" \
  --stage Prod

nitro client publish \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "<previous>" \
  --stage Prod

nitro client unpublish \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "<bad>" \
  --stage Prod
```

| Symptom                                         | Recovery                                                                                      |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------- |
| App deploy fails after schema or client publish | Redeploy the old app or republish the previous schema and client tags that match the old app. |
| New client version breaks                       | Unpublish the bad client tag from the stage while leaving previous client versions active.    |
| Rollback crosses a breaking schema boundary     | Restore the schema tag before routing old app traffic.                                        |
| Active mobile clients still use old fields      | Keep the old schema shape or keep compatible replacement fields until those clients are gone. |

Do not delete old versions needed by active clients. Keep release notes or deployment metadata that maps app versions to schema tags and client tags.

# Troubleshoot CI failures

| Symptom                                              | Likely cause                                                                                                                                                                                       | Fix                                                                                                                           |
| ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `schema` command is not recognized                   | Missing `HotChocolate.AspNetCore.CommandLine`, or `Program.cs` does not use `RunWithGraphQLCommandsAsync(args)`.                                                                                   | Add the package and return the command wrapper exit code.                                                                     |
| CI hangs after running the app                       | `Program.cs` calls `app.Run()` and never returns for schema commands.                                                                                                                              | Replace it with `return await app.RunWithGraphQLCommandsAsync(args);`.                                                        |
| `No schemas registered.`                             | GraphQL services were not registered in the CI environment, or the schema name is wrong.                                                                                                           | Run `schema list`, check environment-specific registration, and pass `--schema-name` when needed.                             |
| Export produces an unexpected path                   | The output path was omitted or interpreted differently than expected.                                                                                                                              | Create `artifacts` and pass `--output ./artifacts/schema.graphqls`.                                                           |
| Snapshot diff appears with no intentional SDL change | Environment-specific registration, unstable ordering, random values, culture/time differences, dynamic schema source, or dependency version drift.                                                 | Pin versions, fix environment variables, sort deterministic sources, and remove runtime-only values from schema registration. |
| Nitro auth fails                                     | Missing `NITRO_API_KEY`, wrong secret scope, deleted key, or key restricted to another API or stage.                                                                                               | Recreate or rotate the key and verify CI secret exposure.                                                                     |
| Validation targets the wrong environment             | `NITRO_STAGE`, `NITRO_API_ID`, or `NITRO_CLIENT_ID` points to another resource.                                                                                                                    | Store ids per environment and print non-secret target names in CI logs.                                                       |
| Upload fails because the tag exists                  | The release was already uploaded or rerun with the same immutable tag.                                                                                                                             | Treat the rerun as idempotent only if your process checks for existing versions, otherwise create a new tag.                  |
| Publish rejects flags                                | `--force` and `--wait-for-approval` were used together.                                                                                                                                            | Choose one. Prefer `--wait-for-approval` for gated stages.                                                                    |
| PR comments do not appear                            | Missing `pull-requests: write`, action `comment-mode` is `none`, or the PR comes from a fork without token permissions.                                                                            | Add permission for trusted PRs and use `comment-mode: review` or `comment`.                                                   |
| Client validation fails but schema validation passes | Operations changed against an unchanged active schema.                                                                                                                                             | Update the operations, publish a compatible schema first, or validate against the intended stage.                             |
| Schema validation fails but client validation passes | Candidate schema is incompatible with existing active clients.                                                                                                                                     | Use additive schema changes, deprecate first, or unpublish retired client versions after traffic drains.                      |
| Trusted-document request fails in production         | Operation file was not published, hash algorithm or format differs, `AddNitro()` is missing, `UsePersistedOperationPipeline()` is missing, or app traffic arrived before client publish completed. | Align hash settings, publish client versions before deployment, and verify server trusted-document configuration.             |

# What this page does not cover

- Fusion composition and source-schema publishing. Use [`nitro fusion`](/docs/nitro/cli/fusion).
- Detailed schema design and deprecation strategy. See [schema evolution](/docs/hotchocolate/v16/guides/schema-evolution), [schema registry](/docs/hotchocolate/v16/operations/schema-governance/schema-registry), and [breaking-change checks](/docs/hotchocolate/v16/operations/schema-governance/breaking-change-checks).
- Schema export and local schema test details beyond the CI path. See [export a schema](/docs/hotchocolate/v16/operations/schema-governance/export-schema) and [test schema changes](/docs/hotchocolate/v16/operations/schema-governance/test-schema-changes).
- Detailed server lock-down. See [trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) and [First-Party API](/docs/hotchocolate/v16/guides/private-api).
- Telemetry and operation reporting. See [operation reporting](/docs/nitro/apis/operation-reporting).
- Nitro CLI reference details. See [global options](/docs/nitro/cli/global-options), [schema commands](/docs/nitro/cli/schema), [client commands](/docs/nitro/cli/client), [stages](/docs/nitro/apis/stages), and [deployments](/docs/nitro/apis/deployments).

# Next steps

1. Add a schema snapshot test to your server test project.
2. Export `schema.graphqls` in pull request CI.
3. Validate schema and client operations against the first stage that receives the change.
4. Upload schema and client artifacts with the same release tag.
5. Publish those tags to each stage before deploying traffic that depends on them.
