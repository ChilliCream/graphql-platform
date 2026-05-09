---
title: Manage client contracts
---

Use client contracts to release a Hot Chocolate v16 server without guessing which clients break. The schema is the type-level contract. Registered operations are the executable contract for each client version. Nitro stores both, validates them by stage, and can provide the persisted operations that Hot Chocolate executes in production.

This page covers one Hot Chocolate server API. Fusion gateway contracts use separate `nitro fusion` commands and are out of scope.

# Manage client contracts at a glance

A schema diff tells you what changed. Registered operations tell you which deployed clients depend on the schema shape. Use both signals in your release workflow:

```text
1. Change the Hot Chocolate schema.
2. Export SDL and review the schema diff.
3. Validate SDL against the target Nitro stage.
4. Extract client operations during each client build.
5. Validate, upload, and publish client versions.
6. Publish the server schema to the stage.
7. Enforce registered operation IDs in production.
```

The model has four parts:

1. **Schema contract.** The exported SDL describes the fields, arguments, nullability, enum values, directives, descriptions, and deprecations your server exposes.
2. **Executable client contract.** A client version registers the exact operations it can send. The operations are keyed by hash.
3. **Stage state.** Each stage, such as `dev`, `staging`, or `production`, has one active schema and many active client versions.
4. **Runtime enforcement.** Hot Chocolate can reject ad-hoc operations and execute only registered persisted operation IDs.

Use the registry as the shared source of truth for CI gates and releases. Use Hot Chocolate runtime settings to make production traffic follow the registered contract.

# Confirm prerequisites before you create a contract gate

You need these pieces before the workflow can block incompatible releases:

| Requirement                           | Why you need it                                       | Expected result                                                           |
| ------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------- |
| Hot Chocolate v16 server              | Exports the schema and enforces persisted operations. | The server builds and starts.                                             |
| `HotChocolate.AspNetCore.CommandLine` | Adds `schema export`.                                 | `dotnet run -- schema export --output schema.graphql` writes SDL.         |
| `RunWithGraphQLCommandsAsync(args)`   | Returns command failures to CI.                       | `Program.cs` returns an exit code.                                        |
| Nitro API and stage                   | Stores active schemas and client versions.            | You have an API ID and stages such as `dev` and `production`.             |
| Nitro CLI authentication              | Runs registry commands.                               | Use `nitro login` locally or `NITRO_API_KEY` in CI.                       |
| CI secret store                       | Protects API keys.                                    | `NITRO_API_KEY` is available only to release jobs that need it.           |
| `ChilliCream.Nitro` package           | Lets the server read persisted operations from Nitro. | `AddNitro()` is available in server configuration.                        |
| Client operation extraction           | Produces the operations file for each client build.   | You have a JSON file such as `persisted_queries.json`.                    |
| Client IDs                            | Separates independently deployed consumers.           | Examples: `web-checkout`, `ios-shop`, `android-shop`, `inventory-worker`. |
| Tag policy                            | Connects registry versions to artifacts.              | Use semver, mobile build numbers, Git SHAs, or release IDs.               |

At minimum, the contract workflow produces these artifacts and identifiers: `schema.graphql`, a Nitro API ID, a stage name, a Nitro API key, a client ID, a client version tag, and an operations JSON file.

# Export and review the schema contract

Start every server change by exporting the SDL from the configured application:

```bash
dotnet run -- schema export --output schema.graphql
```

Expected output:

```text
Exported Files:
- /repo/schema.graphql
- /repo/schema-settings.json
```

Configure command-line support in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Return the exit code from `RunWithGraphQLCommandsAsync`. CI can then fail when schema construction fails.

Store the exported SDL as a CI artifact. Review the diff in pull requests, especially when a change affects:

- Removed or renamed fields.
- Return type or nullability changes.
- Added required arguments.
- Added enum values, which can be dangerous for generated clients that use exhaustive matching.
- Added interface implementations, which can be dangerous for generated clients that handle possible types.
- Updated descriptions, deprecation reasons, and stability notes.

Descriptions and deprecation reasons are contract text. Treat them as part of the review, not decoration.

## Add a schema snapshot test

A local snapshot test catches accidental schema drift before a registry command runs:

```csharp
// Tests/SchemaTests.cs
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Tests;

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

        // act
        var schema = executor.Schema;

        // assert
        schema.MatchSnapshot();
    }
}
```

Expected result: the first run creates a snapshot. Later runs fail with a schema diff when the SDL changes. Review the diff, then update the snapshot only when the change is intentional.

# Register client operations as executable contracts

Each independently deployed client should publish the operations it can execute. Validate the operations before you upload or publish a version:

```bash
nitro client create --name "web-checkout" --api-id "<api-id>"

nitro client validate \
  --client-id "<client-id>" \
  --stage "dev" \
  --operations-file ./persisted_queries.json

nitro client upload \
  --client-id "<client-id>" \
  --tag "<git-sha-or-version>" \
  --operations-file ./persisted_queries.json

nitro client publish \
  --client-id "<client-id>" \
  --tag "<git-sha-or-version>" \
  --stage "dev"
```

Expected result: Nitro has a validated client version with an owner, a tag, an operations file, and an active stage.

## Extract operations during the client build

Relay can write a Relay-style persisted operations file. Hot Chocolate uses MD5 by default, and Relay commonly uses MD5, so the default hash provider matches this setup.

```js
// relay.config.js
module.exports = {
  schema: "schema/schema.docs.graphql",
  src: "app",
  language: "typescript",
  persistConfig: {
    file: "./persisted_queries.json",
    algorithm: "MD5",
  },
};
```

A minimal operations file maps operation hash to operation text:

```json
{
  "913abc361487c481cf6015841c0eca22": "query Viewer { me { username } }"
}
```

Other clients can use the same shape if their build produces stable operation hashes and GraphQL documents.

## Publish each deployable client separately

Do not combine unrelated applications under one client ID. A web app, iOS app, Android app, and background worker usually need separate clients because they deploy and retire versions differently.

A typical CI sequence is:

1. Build the client and extract operations.
2. Run `nitro client validate` against the target stage.
3. Upload the version with the build tag during the release build.
4. Publish the version to the stage where that artifact is deployed.
5. Keep old mobile versions published until their support window ends.

If you use Nitro operation monitoring for adoption evidence, send these headers with requests:

```http
GraphQL-Client-Id: <client-id>
GraphQL-Client-Version: <client-version-tag>
```

# Gate schema changes with schema and client checks

A safe server gate has two checks:

1. Validate the schema diff against the target stage.
2. Validate each submitted client operations file against the active schema for that stage.

Run schema validation in pull requests:

```bash
nitro schema validate \
  --api-id "<api-id>" \
  --stage "dev" \
  --schema-file ./schema.graphql
```

Expected result: the command exits with code `0` when Nitro accepts the candidate SDL for that stage. It exits non-zero for rejected schema changes or invalid SDL.

Use upload and publish in the release flow:

```bash
nitro schema upload \
  --api-id "<api-id>" \
  --tag "<git-sha-or-version>" \
  --schema-file ./schema.graphql

nitro schema publish \
  --api-id "<api-id>" \
  --tag "<git-sha-or-version>" \
  --stage "dev" \
  --wait-for-approval
```

Expected result: Nitro stores the tagged schema and marks it active for the stage after approval, when the stage requires approval.

Use this release order when a server and clients change together:

1. Export the server schema and run schema snapshot tests.
2. Validate the schema against the target stage.
3. Validate updated client operations against the stage.
4. Upload server and client artifacts with immutable tags.
5. Publish compatible client versions to the stage when those clients deploy.
6. Publish the schema version to the same stage as the server deployment.

Do not treat a removed field as safe only because a local search finds no uses. The registry state and runtime telemetry decide whether supported active clients still depend on it.

# Enforce trusted documents in production

Use trusted documents when production should execute only registered operations. Add `ChilliCream.Nitro`, connect the server to Nitro, enable the persisted operation pipeline, and map the persisted operation endpoint:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddNitro()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}

app.MapGraphQLPersistedOperations();

app.Run();
```

`AddNitro()` can read these environment variables:

```text
NITRO_API_KEY=<api-key>
NITRO_API_ID=<api-id>
NITRO_STAGE=production
```

The settings work together:

- `UsePersistedOperationPipeline()` resolves operations by ID.
- `OnlyAllowPersistedDocuments = true` rejects operations that are not registered.
- `AllowDocumentBody = false` puts the server in strict route mode. The transport does not read incoming `query` document bodies.
- `MapGraphQLPersistedOperations()` exposes `/graphql/persisted/{operationId}` and `/graphql/persisted/{operationId}/{operationName}` for GET and POST.
- `MapGraphQL()` remains available only where developer tooling and ad-hoc operations are allowed.

A strict GET request puts the operation ID and operation name in the route:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

A strict POST request sends variables, not `query` and not `id`:

```http
POST /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts
Content-Type: application/json

{
  "variables": { "first": 10 }
}
```

During migration, you can use the standard `/graphql` body shape with an `id` field when you intentionally allow document bodies:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "first": 10 }
}
```

If legacy clients still send `query`, set `AllowDocumentBody = true` only for the migration window. Strict route mode is the production target for a locked-down first-party API.

Use a request interceptor with `AllowNonPersistedOperation()` only for controlled development or administrative scenarios. Do not grant that bypass to normal production traffic.

# Deprecate, migrate, and remove fields with owner evidence

Add the replacement first, then deprecate the old field with an actionable reason:

```csharp
// Types/ProductQueries.cs
namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    [GraphQLDeprecated("Use `productById` instead. Removal planned after 2026-03-31.")]
    public static Product? GetProduct(int id, CatalogService catalog)
        => catalog.GetById(id);

    public static Product? GetProductById(int id, CatalogService catalog)
        => catalog.GetById(id);
}
```

Good deprecation reasons name the replacement and the removal window:

| Reason                                                       | Quality                                         |
| ------------------------------------------------------------ | ----------------------------------------------- |
| `Use productById instead. Removal planned after 2026-03-31.` | Good, gives an action and date.                 |
| `Deprecated.`                                                | Poor, gives no migration path.                  |
| `Old field.`                                                 | Poor, gives no owner, replacement, or timeline. |

Use this removal playbook:

1. Add the replacement field, argument, enum value, or input field.
2. Deprecate the old schema element with an actionable reason.
3. Notify owners of registered operations that still use it.
4. Publish updated clients that stop using it.
5. Keep older mobile versions active until their support window ends.
6. Unpublish retired client versions.
7. Remove the schema element only when active registry state and runtime monitoring show no supported client still uses it.

Unpublish retired versions from a stage:

```bash
nitro client unpublish \
  --client-id "<client-id>" \
  --stage "production" \
  --tag "<retired-version>"
```

Required input fields and required arguments need a default value before you can deprecate them. In v16, if an object field implements an interface field, deprecate the interface field as well.

A release timeline can look like this:

| Date   | Server action                                                   | Web action                                      | iOS action                          | Android action                      |
| ------ | --------------------------------------------------------------- | ----------------------------------------------- | ----------------------------------- | ----------------------------------- |
| Jan 10 | Add `productById`, deprecate `product`.                         | Validate and publish `web-checkout@2026.01.10`. | Build `ios-shop@1200`.              | Build `android-shop@940`.           |
| Feb 15 | Keep both fields.                                               | Unpublish old web tag after rollout.            | Keep supported app versions active. | Keep supported app versions active. |
| Mar 31 | Remove `product` only if no supported active operations use it. | No action.                                      | Unpublish retired builds.           | Unpublish retired builds.           |

# Version and roll out clients by stage

Stages let you model real deployment state. One stage can have a schema that is not yet active in another stage, and each stage can have a different set of active client versions.

| Stage        | Active schema tag       | Active web tags                    | Active iOS tags                    | Active Android tags          |
| ------------ | ----------------------- | ---------------------------------- | ---------------------------------- | ---------------------------- |
| `dev`        | `server-a1b2c3`         | `web-a1b2c3`                       | `ios-1201`                         | `android-941`                |
| `staging`    | `server-2026.01.10-rc1` | `web-2026.01.10-rc1`               | `ios-1200`                         | `android-940`                |
| `production` | `server-2025.12.15`     | `web-2025.12.15`, `web-2026.01.10` | `ios-1180`, `ios-1190`, `ios-1200` | `android-930`, `android-940` |

A web client may have one active production version, or two during a rollout. A mobile client may have many active production versions for weeks or months because users update at different times.

Define a retirement policy before approving removals. For example:

> A client version can be unpublished when telemetry is below 0.1 percent of requests for 14 consecutive days and the published support window has ended.

Publish schema and client versions to the same stage where their artifacts run. Tags should map to artifacts that teams can find later, such as a Git SHA, container image tag, semantic version, release ID, or mobile build number.

# Keep private, preview, and internal fields out of the stable contract

A field becomes part of the contract when clients can discover it and register operations against it. Decide visibility and stability before publishing the field.

| Problem                               | Better choice                                   | Why                                                      |
| ------------------------------------- | ----------------------------------------------- | -------------------------------------------------------- |
| No client should ever use the field.  | Do not expose it in GraphQL.                    | Authorization does not make an accidental contract safe. |
| Some users may access the field.      | Expose it with authorization.                   | Access control answers who may read or write data.       |
| The field is preview or experimental. | Use `@requiresOptIn`.                           | Default introspection hides it until consumers opt in.   |
| The field is stable.                  | Expose and document it as part of the contract. | Registered operations can depend on it.                  |

Enable opt-in features and mark preview fields:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalRecommendations", "experimental");
```

```csharp
// Types/Product.cs
namespace Catalog.Types;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    [RequiresOptIn("experimentalRecommendations")]
    public IReadOnlyList<Product>? Recommendations { get; set; }
}
```

Consumers can discover opt-in fields with `includeOptIn` and can inspect `optInFeatureStability` through introspection. Use authorization for access control. Use opt-in features for stability signaling. Use documentation to set owner, lifecycle, and support expectations.

# Troubleshoot broken client contracts

| Symptom                                     | Likely cause                                                                                                                                  | Fix                                                                                                                           |
| ------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `nitro schema validate` fails.              | The candidate SDL has a breaking or rejected change against the target stage.                                                                 | Add a replacement, deprecate first, migrate clients, or use the approval workflow if your policy allows it.                   |
| `nitro client validate` fails.              | The operations file references a field, argument, type, or enum value that is missing or changed in the active stage schema.                  | Regenerate the client against the correct schema, update the operation, or publish the compatible schema to that stage first. |
| Persisted operation not found.              | The client version is not published to the stage, `NITRO_STAGE` is wrong, the hash differs, or the server is not connected with `AddNitro()`. | Verify environment variables, client publish state, operation hash, and server startup logs.                                  |
| Standard operation is rejected.             | `OnlyAllowPersistedDocuments = true` is active and the request did not reference a registered operation ID.                                   | Send a persisted operation request or allow non-persisted operations only in a controlled environment.                        |
| Query body is ignored.                      | Strict mode has `AllowDocumentBody = false`.                                                                                                  | Use `/graphql/persisted/{operationId}/{operationName}` or switch to migration mode deliberately.                              |
| Relay request fails in body mode.           | Relay examples may use `doc_id`, but Hot Chocolate body mode expects `id`.                                                                    | Send `id` in body mode. In strict route mode, send the operation ID in the URL and omit `id`.                                 |
| Hash mismatch.                              | The client extraction algorithm differs from the server hash provider.                                                                        | Use MD5 on both sides, or configure the Hot Chocolate document hash provider to match the client.                             |
| Operation name is required.                 | The endpoint was configured with `requireOperationName: true`.                                                                                | Include the operation name in `/graphql/persisted/{operationId}/{operationName}`.                                             |
| Developer tooling cannot introspect.        | `MapGraphQL()` is disabled or `AllowNonPersistedOperation()` is not granted in that environment.                                              | Enable tooling only in approved environments, or use a controlled bypass for developers.                                      |
| Opt-in field is missing from introspection. | Opt-in features are disabled or the introspection query omitted `includeOptIn`.                                                               | Enable `EnableOptInFeatures` and query with `includeOptIn`.                                                                   |
| Deprecated field still cannot be removed.   | An active registered client version or runtime usage still references it.                                                                     | Keep the field, migrate the owner, and unpublish retired versions after support ends.                                         |

# Choose trusted documents, APQ, or open operations

Pick the operation strategy per API surface:

| Strategy                               | Contract strength                                                  | Client requirements                                                | Server enforcement                                            | Best fit                                                                        |
| -------------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------ | ------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Trusted documents with client registry | Strong. Operations are pre-registered by client version and stage. | Clients extract operations at build time and send operation IDs.   | Hot Chocolate can execute only registered IDs.                | First-party web, mobile, and service clients.                                   |
| Automatic persisted operations (APQ)   | Moderate. Operations are stored at runtime.                        | Clients can retry with the full document when the hash is unknown. | Improves performance, but does not create a release contract. | Clients that cannot pre-register operations.                                    |
| Open operations                        | Low. Any valid operation can be sent.                              | Clients send GraphQL documents at runtime.                         | Use cost, depth, authorization, and introspection policies.   | Public exploratory APIs or partner APIs where pre-registration is not possible. |

This page focuses on Hot Chocolate server operations. It does not cover Fusion gateway contracts.

# Next steps

- [Schema Evolution](/docs/hotchocolate/v16/guides/schema-evolution): document, deprecate, and evolve fields.
- [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning): use `@deprecated` and `@requiresOptIn`.
- [Testing](/docs/hotchocolate/v16/guides/testing): add schema snapshot tests.
- [Command Line](/docs/hotchocolate/v16/server/command-line): export SDL in CI.
- [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents): understand persisted operation storage and hash providers.
- [First-Party API](/docs/hotchocolate/v16/guides/private-api): lock down a first-party Hot Chocolate server.
- [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations): compare APQ.
- [Nitro schema CLI](/docs/nitro/cli/schema): use schema registry commands.
- [Nitro client CLI](/docs/nitro/cli/client): use client registry commands.
- [Schema Registry](/docs/nitro/apis/schema-registry): learn registry concepts.
- [Client Registry](/docs/nitro/apis/client-registry): learn client and version concepts.
- [Operation Monitoring](/docs/nitro/open-telemetry/operation-monitoring): monitor client IDs and versions.
