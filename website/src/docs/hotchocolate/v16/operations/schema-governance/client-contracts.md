---
title: Manage client contracts
---

Client contracts let you release a Hot Chocolate v16 server with confidence, knowing exactly which clients depend on your schema. The schema itself is your type-level contract, while registered operations define the executable contract for each client version. Nitro stores both contracts, validates them by stage, and supplies the persisted operations that Hot Chocolate runs in production.

This page focuses on the Hot Chocolate server API. Fusion gateway contracts require separate `nitro fusion` commands and are not covered here.

# Overview: Managing Client Contracts

When you change your schema, a schema diff shows what changed. Registered operations reveal which deployed clients rely on specific schema shapes. Use both signals in your release workflow:

```text
1. Change the Hot Chocolate schema.
2. Export SDL and review the schema diff.
3. Validate SDL against the target Nitro stage.
4. Extract client operations during each client build.
5. Validate, upload, and publish client versions.
6. Publish the server schema to the stage.
7. Enforce registered operation IDs in production.
```

This model has four key parts:

1. **Schema contract:** The exported SDL defines the fields, arguments, nullability, enum values, directives, descriptions, and deprecations your server exposes.
2. **Executable client contract:** Each client version registers the exact operations it can send, keyed by hash.
3. **Stage state:** Each stage (like `dev`, `staging`, or `production`) has one active schema and multiple active client versions.
4. **Runtime enforcement:** Hot Chocolate can reject ad-hoc operations and only execute registered persisted operation IDs.

Use the registry as your single source of truth for CI gates and releases. Configure Hot Chocolate runtime settings to ensure production traffic follows the registered contract.

# Prerequisites for Contract Gates

Before you can block incompatible releases with contract gates, make sure you have the following:

| Requirement                           | Purpose                                        | Expected Result                                                          |
| ------------------------------------- | ---------------------------------------------- | ------------------------------------------------------------------------ |
| Hot Chocolate v16 server              | Exports schema, enforces persisted operations  | Server builds and starts                                                 |
| `HotChocolate.AspNetCore.CommandLine` | Adds `schema export` command                   | `dotnet run -- schema export --output schema.graphql` writes SDL         |
| `RunWithGraphQLCommandsAsync(args)`   | Returns command failures to CI                 | `Program.cs` returns an exit code                                        |
| Nitro API and stage                   | Stores schemas and client versions             | You have an API ID and stages like `dev` and `production`                |
| Nitro CLI authentication              | Runs registry commands                         | Use `nitro login` locally or `NITRO_API_KEY` in CI                       |
| CI secret store                       | Protects API keys                              | `NITRO_API_KEY` is available only to release jobs that need it           |
| `ChilliCream.Nitro` package           | Reads persisted operations from Nitro          | `AddNitro()` is available in server configuration                        |
| Client operation extraction           | Produces operations file for each client build | You have a JSON file like `persisted_queries.json`                       |
| Client IDs                            | Distinguishes independently deployed consumers | Examples: `web-checkout`, `ios-shop`, `android-shop`, `inventory-worker` |
| Tag policy                            | Connects registry versions to artifacts        | Use semver, build numbers, Git SHAs, or release IDs                      |

At a minimum, your workflow should produce: `schema.graphql`, a Nitro API ID, a stage name, a Nitro API key, a client ID, a client version tag, and an operations JSON file.

# Export and Review the Schema Contract

Whenever you change the server, start by exporting the SDL from your configured application:

```bash
dotnet run -- schema export --output schema.graphql
```

You should see output like:

```text
Exported Files:
- /repo/schema.graphql
- /repo/schema-settings.json
```

To enable this, set up command-line support in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Always return the exit code from `RunWithGraphQLCommandsAsync` so CI can fail if schema construction fails.

Store the exported SDL as a CI artifact. In pull requests, review the diff carefully, especially for:

- Removed or renamed fields
- Changes to return types or nullability
- Added required arguments
- Added enum values (can break generated clients using exhaustive matching)
- Added interface implementations (can break generated clients handling possible types)
- Updated descriptions, deprecation reasons, or stability notes

Treat descriptions and deprecation reasons as part of the contract, not as optional decoration.

## Add a Schema Snapshot Test

A local snapshot test helps you catch accidental schema drift before you run any registry commands:

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

Expected result: The first run creates a snapshot. Later runs fail with a schema diff if the SDL changes. Review the diff and update the snapshot only when the change is intentional.

# Register Client Operations as Executable Contracts

Each independently deployed client should publish the operations it can execute. Always validate operations before uploading or publishing a version:

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

After these steps, Nitro will have a validated client version with an owner, tag, operations file, and active stage.

## Extract Operations During the Client Build

Relay can generate a persisted operations file. Hot Chocolate and Relay both use MD5 by default, so the default hash provider matches this setup.

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

Other clients can use this format if their build produces stable operation hashes and GraphQL documents.

## Publish Each Deployable Client Separately

Do not combine unrelated applications under one client ID. Web, iOS, Android, and background worker apps usually need separate client IDs because they deploy and retire versions independently.

A typical CI sequence:

1. Build the client and extract operations.
2. Run `nitro client validate` against the target stage.
3. Upload the version with the build tag during the release build.
4. Publish the version to the stage where that artifact is deployed.
5. Keep old mobile versions published until their support window ends.

If you use Nitro operation monitoring to track adoption, send these headers with requests:

```http
GraphQL-Client-Id: <client-id>
GraphQL-Client-Version: <client-version-tag>
```

# Gate Schema Changes with Schema and Client Checks

A robust server gate uses two checks:

1. Validate the schema diff against the target stage.
2. Validate each submitted client operations file against the active schema for that stage.

To validate the schema in pull requests:

```bash
nitro schema validate \
  --api-id "<api-id>" \
  --stage "dev" \
  --schema-file ./schema.graphql
```

The command exits with code `0` if Nitro accepts the candidate SDL for that stage. It exits non-zero for rejected schema changes or invalid SDL.

For releases, upload and publish the schema:

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

Nitro stores the tagged schema and marks it active for the stage after approval, if required.

When server and clients change together, follow this release order:

1. Export the server schema and run schema snapshot tests.
2. Validate the schema against the target stage.
3. Validate updated client operations against the stage.
4. Upload server and client artifacts with immutable tags.
5. Publish compatible client versions to the stage as they deploy.
6. Publish the schema version to the same stage as the server deployment.

Never assume a removed field is safe just because a local search finds no uses. The registry state and runtime telemetry determine whether any supported active client still depends on it.

# Enforce Trusted Documents in Production

To ensure production only executes registered operations, use trusted documents. Add `ChilliCream.Nitro`, connect your server to Nitro, enable the persisted operation pipeline, and map the persisted operation endpoint:

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

`AddNitro()` reads these environment variables:

```text
NITRO_API_KEY=<api-key>
NITRO_API_ID=<api-id>
NITRO_STAGE=production
```

These settings work together:

- `UsePersistedOperationPipeline()` resolves operations by ID.
- `OnlyAllowPersistedDocuments = true` rejects any operation not registered.
- `AllowDocumentBody = false` puts the server in strict route mode, ignoring incoming `query` document bodies.
- `MapGraphQLPersistedOperations()` exposes `/graphql/persisted/{operationId}` and `/graphql/persisted/{operationId}/{operationName}` for GET and POST.
- `MapGraphQL()` is available only where developer tooling and ad-hoc operations are allowed.

A strict GET request includes the operation ID and name in the route:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

A strict POST request sends only variables, not `query` or `id`:

```http
POST /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts
Content-Type: application/json

{
  "variables": { "first": 10 }
}
```

During migration, you can use the standard `/graphql` body shape with an `id` field if you intentionally allow document bodies:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "first": 10 }
}
```

If legacy clients still send `query`, set `AllowDocumentBody = true` only for the migration window. Strict route mode is the production target for a locked-down first-party API.

Use a request interceptor with `AllowNonPersistedOperation()` only for controlled development or administrative scenarios. Never grant that bypass to normal production traffic.

# Deprecate, Migrate, and Remove Fields with Owner Evidence

When you need to remove a field, always add the replacement first, then deprecate the old field with a clear, actionable reason:

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

A good deprecation reason names the replacement and the removal window:

| Reason                                                       | Quality                                  |
| ------------------------------------------------------------ | ---------------------------------------- |
| `Use productById instead. Removal planned after 2026-03-31.` | Good: gives an action and date           |
| `Deprecated.`                                                | Poor: no migration path                  |
| `Old field.`                                                 | Poor: no owner, replacement, or timeline |

Follow this removal playbook:

1. Add the replacement field, argument, enum value, or input field.
2. Deprecate the old schema element with an actionable reason.
3. Notify owners of registered operations that still use it.
4. Publish updated clients that stop using it.
5. Keep older mobile versions active until their support window ends.
6. Unpublish retired client versions.
7. Remove the schema element only when registry state and runtime monitoring show no supported client still uses it.

To unpublish retired versions from a stage:

```bash
nitro client unpublish \
  --client-id "<client-id>" \
  --stage "production" \
  --tag "<retired-version>"
```

Required input fields and required arguments need a default value before you can deprecate them. In v16, if an object field implements an interface field, deprecate the interface field as well.

A release timeline might look like this:

| Date   | Server action                                                   | Web action                                      | iOS action                          | Android action                      |
| ------ | --------------------------------------------------------------- | ----------------------------------------------- | ----------------------------------- | ----------------------------------- |
| Jan 10 | Add `productById`, deprecate `product`.                         | Validate and publish `web-checkout@2026.01.10`. | Build `ios-shop@1200`.              | Build `android-shop@940`.           |
| Feb 15 | Keep both fields.                                               | Unpublish old web tag after rollout.            | Keep supported app versions active. | Keep supported app versions active. |
| Mar 31 | Remove `product` only if no supported active operations use it. | No action.                                      | Unpublish retired builds.           | Unpublish retired builds.           |

# Version and Roll Out Clients by Stage

Stages let you model real deployment state. Each stage can have a different active schema and a different set of active client versions.

| Stage        | Active schema tag       | Active web tags                    | Active iOS tags                    | Active Android tags          |
| ------------ | ----------------------- | ---------------------------------- | ---------------------------------- | ---------------------------- |
| `dev`        | `server-a1b2c3`         | `web-a1b2c3`                       | `ios-1201`                         | `android-941`                |
| `staging`    | `server-2026.01.10-rc1` | `web-2026.01.10-rc1`               | `ios-1200`                         | `android-940`                |
| `production` | `server-2025.12.15`     | `web-2025.12.15`, `web-2026.01.10` | `ios-1180`, `ios-1190`, `ios-1200` | `android-930`, `android-940` |

A web client may have one active production version, or two during a rollout. Mobile clients may have several active production versions for weeks or months, since users update at different times.

Define a retirement policy before approving removals. For example:

> A client version can be unpublished when telemetry is below 0.1 percent of requests for 14 consecutive days and the published support window has ended.

Always publish schema and client versions to the same stage where their artifacts run. Tags should map to artifacts that teams can find later, such as a Git SHA, container image tag, semantic version, release ID, or mobile build number.

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
