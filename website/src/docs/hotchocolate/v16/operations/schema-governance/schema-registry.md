---
title: Use a schema registry
---

# Schema Registry for Hot Chocolate v16

Use Nitro's schema registry to manage the schema contract for a single Hot Chocolate v16 server API. The registry stores versioned SDL, compares new schemas to the active schema for each stage, classifies changes, and can check active client operations before you publish a release.

This page focuses on a single Hot Chocolate server schema. Fusion gateways and source-schema publishing use [`nitro fusion`](/docs/nitro/cli/fusion) commands, which are not covered here.

## How Schema Governance Fits Your Release Pipeline

A safe release pipeline for your GraphQL API includes these feedback loops:

1. Change your Hot Chocolate schema in code.
2. Run a schema snapshot test to catch unintended local changes.
3. Export the configured server schema as SDL.
4. Validate the SDL against the Nitro stage you plan to target.
5. Upload the approved SDL under an immutable release tag.
6. Publish that tag to the stage when deployment is approved.
7. Deploy the matching server artifact.
8. Keep registered clients running against the active schema.

Your GraphQL schema defines the contract between client and server. Snapshot tests alert you to local contract changes. The schema registry tells you if your contract is safe for `dev`, `staging`, or `prod`, with each stage potentially having a different active schema and different active client versions.

The registry governs release safety. It does not define your C# schema types, replace schema design guidance, or require Nitro runtime services for schema export, validation, upload, or publish.

## Prerequisites

Before you start, make sure you have the following:

| Requirement                           | Why you need it                                             | Check                                               |
| ------------------------------------- | ----------------------------------------------------------- | --------------------------------------------------- |
| Hot Chocolate v16 server project      | The registry uses SDL exported from your configured API.    | The project builds and can start.                   |
| `HotChocolate.AspNetCore.CommandLine` | Adds `schema export` to your server process.                | The API project references the package.             |
| `RunWithGraphQLCommandsAsync(args)`   | Lets CI receive the command exit code.                      | `Program.cs` returns the command result.            |
| Nitro CLI                             | Runs registry commands.                                     | `nitro --help` works after installation.            |
| Nitro authentication                  | Allows local or CI registry access.                         | Use `nitro login` locally or `NITRO_API_KEY` in CI. |
| Nitro API id                          | Identifies the API in schema commands.                      | Run `nitro api list`.                               |
| Stages                                | Model environments such as `dev` and `prod`.                | Create or verify stages in Nitro.                   |
| API-scoped key for CI                 | Limits automation to one API, optionally one stage.         | Create with `nitro api-key create`.                 |
| Schema snapshot test                  | Catches accidental schema drift before registry validation. | Run the test in PR builds.                          |

Wire the command-line package into your API project:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Install and authenticate Nitro for local setup:

```bash
dotnet tool install --global ChilliCream.Nitro.CommandLine
nitro login
nitro api list
nitro api-key create --name "hc-ci-dev" --api-id "<api-id>" --stage-condition "dev"
```

Store the generated key as `NITRO_API_KEY` in your CI secret manager. Nitro shows the secret only once.

## Catch Accidental Schema Changes Locally

Add a schema snapshot test to review the SDL shape before CI uploads or publishes anything:

```csharp
// Tests/SchemaTests.cs
public class SchemaTests
{
    [Fact]
    public async Task Schema_Should_Match_Snapshot_When_ServerSchemaChanges()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act & assert
        executor.Schema.MatchSnapshot();
    }
}
```

On the first run, this test creates a snapshot. Later runs fail with a diff if the schema SDL changes.

This test is a fast local guardrail. It catches renamed fields, removed types, nullability changes, and description changes. However, it does not know which schema is active in Nitro or which registered clients are active on a stage.

## Export the Hot Chocolate Schema as SDL

Run `schema export` from the server project, or specify the project explicitly:

```bash
mkdir -p artifacts
dotnet run --project src/MyApi -- schema export --output ./artifacts/schema.graphqls
```

You should see output like:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

Use the exported `.graphqls` file as the artifact for Nitro validation and upload. Export from the same application configuration that represents the contract you deploy. If your server hosts multiple schemas, use `--schema-name <name>` for the intended contract:

```bash
dotnet run --project src/MyApi -- \
  schema export \
  --schema-name Catalog \
  --output ./artifacts/catalog.graphqls
```

Use `--semantic-non-null` only if your downstream clients intentionally consume semantic non-null annotations:

```bash
dotnet run --project src/MyApi -- \
  schema export \
  --output ./artifacts/schema.semantic.graphqls \
  --semantic-non-null
```

Treat the SDL as a build artifact for your release pipeline. Commit it only if your team reviews SDL in source control.

## Validate Schema Changes in Pull Requests

Always validate schema changes before deployment. Validate against the stage that matches your deployment target—usually `dev` for feature branches and `prod` for hotfixes.

```bash
export NITRO_API_ID="<api-id>"
export NITRO_STAGE="dev"
export NITRO_SCHEMA_FILE="./artifacts/schema.graphqls"

nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "$NITRO_STAGE" \
  --schema-file "$NITRO_SCHEMA_FILE" \
  --output json
```

The command exits with code `0` if Nitro accepts the schema. It exits non-zero if the SDL has GraphQL errors, contains rejected schema changes, or breaks active client operations published to the stage.

A minimal GitHub Actions step:

```yaml
- name: Export schema
  run: |
    mkdir -p artifacts
    dotnet run --project src/MyApi -- schema export --output ./artifacts/schema.graphqls

- name: Validate schema
  env:
    NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}
    NITRO_API_ID: ${{ vars.NITRO_API_ID }}
  run: |
    nitro schema validate \
      --api-id "$NITRO_API_ID" \
      --stage "dev" \
      --schema-file ./artifacts/schema.graphqls \
      --output json
```

Use `--output json` when CI needs machine-readable output. This also disables prompts, so provide every required option.

## Upload an Immutable Schema Version for a Release

After validation succeeds, upload the same SDL under an immutable tag. Uploading creates a version in the registry, but does not activate it on any stage.

```bash
SCHEMA_TAG="${GITHUB_SHA:-local-dev}"

nitro schema upload \
  --api-id "$NITRO_API_ID" \
  --tag "$SCHEMA_TAG" \
  --schema-file ./artifacts/schema.graphqls
```

Nitro reports the uploaded schema version and tag.

Use a Git commit SHA, release number, or container image tag. Do not reuse tags. If a release changes, create a new tag so registry history remains auditable.

## Publish the Schema Version to a Stage

Publishing makes one uploaded schema version active for a stage. Publish in the same order as your server deployment path.

```bash
nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "$SCHEMA_TAG" \
  --stage "dev"
```

Nitro marks the schema version as active for `dev`.

For a gated production stage, wait for approval from the deployment job:

```bash
nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "$SCHEMA_TAG" \
  --stage "prod" \
  --wait-for-approval
```

The command waits until deployment is approved, then completes when Nitro publishes the schema.

Keep schema tags and server artifact tags aligned. You can publish before, during, or after deployment, but the active schema should always describe the server version that receives traffic.

The `--force` option skips prompts and can publish despite breaking changes. Do not use it in normal CI. Document who can use it, when it is allowed, and which manual checks are required. `--force` and `--wait-for-approval` cannot be used together.

## Model Environments with Stages

A Nitro stage represents an environment. Each stage can have one active schema and multiple active client versions.

| Server environment | Nitro stage       | Typical use                                  |
| ------------------ | ----------------- | -------------------------------------------- |
| Development        | `dev`             | Feature validation and early integration.    |
| Staging or QA      | `staging` or `qa` | Release-candidate validation.                |
| Production         | `prod`            | User-facing contract and production clients. |

A small API might use `dev -> prod`. Larger teams might use `dev -> qa -> prod` or several QA stages before production. Always validate and publish against the same stage your server artifact targets.

Use stage-scoped API keys to reduce risk. A `dev` deployment key should not be able to publish a schema to `prod`.

## Add Client Operations When Compatibility Depends on Usage

Schema-only validation answers: "Could this change break an existing valid operation?"

Client-aware validation answers: "Does this change break an active client version on this stage?"

For example, removing a deprecated field is structurally breaking. If your clients are registered, Nitro can check whether active client versions still select that field before you publish. This is important for first-party web apps, mobile apps with overlapping versions, and production trusted-document workflows.

Client versions contain persisted operations and are published to stages:

```bash
nitro client upload \
  --client-id "<client-id>" \
  --tag "$CLIENT_TAG" \
  --operations-file ./operations.json

nitro client publish \
  --client-id "<client-id>" \
  --tag "$CLIENT_TAG" \
  --stage "prod"
```

The client version becomes active on `prod` and participates in future schema checks for that stage.

You can use the schema registry without the client registry for public exploratory APIs. Add the client registry when operation usage determines whether a breaking schema change is safe. For server lock-down with trusted documents, see the [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) and [first-party API](/docs/hotchocolate/v16/guides/private-api) guides.

## Configure Schema-Change Policy and Review Gates

Decide how strict Nitro should be before relying on it in CI.

| Stage     | Suggested policy                                                                                                                      | Reason                                                             |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `dev`     | Treat dangerous changes as breaking, optionally allow breaking schema changes when no active client breaks.                           | Keeps early feedback strict while allowing coordinated migrations. |
| `staging` | Treat dangerous changes as breaking and require release approval.                                                                     | Mirrors production risk before users see the change.               |
| `prod`    | Treat dangerous changes as breaking, reject breaking schema changes unless your team has a documented client-aware exception process. | Protects the user-facing contract.                                 |

Configure API settings non-interactively with explicit boolean values:

```bash
nitro api set-settings "$NITRO_API_ID" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes false
```

Nitro will reject breaking schema changes, even if no active client operation currently breaks.

If your team intentionally allows removals when no active registered client uses the removed member, configure:

```bash
nitro api set-settings "$NITRO_API_ID" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes true
```

Now Nitro can allow breaking schema changes when active client operations still validate. Use this only with active client registry coverage and a documented deprecation process.

## Secure Registry Access in CI and Runtime

| Variable or option  | Used by                                           | Notes                                                   |
| ------------------- | ------------------------------------------------- | ------------------------------------------------------- |
| `NITRO_API_KEY`     | Nitro CLI and Nitro runtime integrations          | Store as a secret. Never commit it.                     |
| `--api-key`         | Nitro CLI                                         | Use only when an environment variable is not practical. |
| `NITRO_API_ID`      | Nitro CLI and `AddNitro()` scenarios              | Get it from `nitro api list` or the Nitro UI.           |
| `NITRO_STAGE`       | Validate and publish jobs, runtime integrations   | Match the deployment target.                            |
| `NITRO_SCHEMA_FILE` | `nitro schema validate` and `nitro schema upload` | Point to the exported SDL artifact.                     |
| `NITRO_TAG`         | Upload and publish commands                       | Use the release tag or commit SHA.                      |

Use API-scoped keys for schema registry automation. Add `--stage-condition "<stage>"` when a job should only validate or publish one stage. Use a personal access token only for broader user-level workspace automation.

Your Hot Chocolate server does not need `ChilliCream.Nitro` runtime configuration for `schema export`, `schema validate`, `schema upload`, or `schema publish`. Runtime configuration is only needed for Nitro-backed persisted operations, operation reporting, or telemetry.

## Troubleshooting: Rejected or Missing Schemas

| Symptom                                                       | Likely cause                                                                                                       | Fix                                                                                                                          |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| `schema export` reports that no schema is registered.         | GraphQL services were not configured, the command ran in the wrong project, or the wrong schema name was selected. | Run from the server project, ensure `AddGraphQL()` or `AddGraphQLServer()` runs, and pass `--schema-name` for named schemas. |
| Schema file does not exist in CI.                             | The working directory or output path is wrong.                                                                     | Print `pwd`, list `artifacts`, create the directory, and pass an explicit `--schema-file`.                                   |
| Nitro returns unauthorized.                                   | `NITRO_API_KEY` is missing, invalid, expired, or lacks API or stage access.                                        | Recreate an API-scoped key, store it as a CI secret, and check `--stage-condition`.                                          |
| API or stage is not found.                                    | `NITRO_API_ID` is wrong, the stage name differs, or the key is restricted to another stage.                        | Run `nitro api list`, verify names such as `prod` vs `production`, and check key scope.                                      |
| Schema is rejected for breaking or dangerous changes.         | The candidate contract removes or changes members that clients may depend on.                                      | Inspect Nitro diagnostics, deprecate first, make an additive change, or coordinate a migration.                              |
| Schema is rejected because active clients break.              | A published client version still sends an operation that no longer validates.                                      | Publish updated clients first, keep the old field, or wait until affected client versions are unpublished.                   |
| GraphQL syntax or schema error.                               | The SDL is stale, hand-edited, or exported from a failing build.                                                   | Rebuild, re-export from the current Hot Chocolate server, and validate the artifact you uploaded.                            |
| Duplicate tag.                                                | The release tag was already uploaded.                                                                              | Use a new immutable tag instead of overwriting history.                                                                      |
| Upload succeeded but Nitro still shows the old active schema. | Upload created a version but did not publish it.                                                                   | Run `nitro schema publish` for the target stage.                                                                             |
| Diff does not match local expectations.                       | You compared against a different active stage schema or a stale artifact.                                          | Download the active schema and compare it with your exported file.                                                           |

To manually diff the active stage schema, download it:

```bash
nitro schema download \
  --api-id "$NITRO_API_ID" \
  --stage "$NITRO_STAGE" \
  --output-file ./current.graphqls
```

The `current.graphqls` file will contain the schema currently active on the stage.

## Related Topics

- Fusion gateway governance uses [`nitro fusion`](/docs/nitro/cli/fusion) commands.
- Operation reporting and telemetry are covered in observability pages.
- Trusted document server lock-down is covered in [persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) and [first-party API](/docs/hotchocolate/v16/guides/private-api).
- Schema design, deprecation, and public API lifecycle patterns are covered in [schema evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [versioning](/docs/hotchocolate/v16/building-a-schema/versioning).

## Verify Your Workflow

Before relying on the registry gate, check that:

- `dotnet run --project src/MyApi -- schema export --output ./artifacts/schema.graphqls` exits with code `0`.
- The exported SDL is the file passed to `nitro schema validate` and `nitro schema upload`.
- Validation runs against the stage that matches the deployment target.
- Upload uses an immutable tag such as the commit SHA or image tag.
- Publish uses the same tag as the deployed server artifact.
- Production stages use approval gates or another documented review process.
- CI secrets use API-scoped, stage-scoped keys where possible.
- Client registry checks are enabled when first-party client usage determines compatibility.

## Next Steps

- Design non-breaking changes with [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Configure SDL export with [Export a schema](/docs/hotchocolate/v16/operations/schema-governance/export-schema) and [Command Line](/docs/hotchocolate/v16/server/command-line).
- Manage Nitro schema versions with the [Nitro schema CLI](/docs/nitro/cli/schema).
- Add client-aware checks with [Client Registry](/docs/nitro/apis/client-registry), [Nitro client CLI](/docs/nitro/cli/client), and [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents).
- Model environments with [Nitro stages](/docs/nitro/apis/stages).
- Secure automation with [Nitro API keys](/docs/nitro/cli/api-key) and [Global Options](/docs/nitro/cli/global-options).
- Lock down first-party APIs with the [First-Party API guide](/docs/hotchocolate/v16/guides/private-api).
