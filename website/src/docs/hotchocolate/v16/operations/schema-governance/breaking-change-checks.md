---
title: Breaking-change checks
---

# Breaking-change checks

Before merging changes, always export or snapshot your Hot Chocolate schema and compare the candidate schema to the baseline your clients use. This process helps you catch accidental removals, risky nullability changes, new required inputs, and potential client operation failures before they reach production.

This guide focuses on schema governance for a single Hot Chocolate v16 server. If you use Fusion gateway composition, follow its separate workflow and use the [`nitro fusion`](/docs/nitro/cli/fusion) commands.

To export and validate your schema, use:

```bash
mkdir -p artifacts

dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "$NITRO_STAGE" \
  --schema-file artifacts/schema.graphqls
```

When you run `schema export`, you should see output like:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

The `nitro schema validate` command exits with `0` if the candidate schema is valid for the target stage. If there are breaking changes or published client operations would fail, it exits with a non-zero code and prints validation errors.

## Prerequisites

To use breaking-change checks, ensure you have:

- A Hot Chocolate v16 ASP.NET Core server project
- A reference to `HotChocolate.AspNetCore.CommandLine` in your server project
- A `Program.cs` that returns the command exit code
- The configuration needed to build the same schema as production
- A test project that can build an `IRequestExecutor` (for local snapshots)
- (Optional) Nitro CLI authentication via `nitro login`, `--api-key`, or `NITRO_API_KEY`
- (Optional) Nitro API ID, stage name, and published client operations (for registry checks)

Integrate the command-line package into your application host as follows:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Returning the `Task<int>` from `RunWithGraphQLCommandsAsync(args)` ensures that schema construction and command failures will fail your CI pipeline. For more setup details, see the [Command Line](/docs/hotchocolate/v16/server/command-line) documentation.

## Run a local schema check first

Begin every pull request by running a local contract check. This gives you the fastest feedback and does not require a registry account.

If your test suite already builds the executable schema, use a schema snapshot:

```csharp
using CookieCrumble.HotChocolate;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Tests;

public class SchemaTests
{
    [Fact]
    public async Task Schema_Should_MatchSnapshot_When_Built()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.MatchSnapshot();
    }
}
```

If the snapshot fails, treat it as a contract review. Do not accept a new snapshot until you classify the change and decide if clients need migration time.

When you need a standalone SDL file for reviewers, CI artifacts, code generation, or Nitro, use the CLI export:

```bash
mkdir -p artifacts

dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

git diff --exit-code -- artifacts/schema.graphqls
```

You should see:

- `artifacts/schema.graphqls` with the exported SDL
- `artifacts/schema-settings.json` with schema metadata for tools
- `git diff --exit-code` fails if the exported baseline changed

Use `schema print` if you need SDL on stdout. `schema export` always writes files.

## Read the diff as a contract change

Your GraphQL schema is the public contract between your server and its clients. A schema diff is not just a formatting change—it tells you if existing operations, generated client types, normalized caches, persisted operation manifests, and developer documentation still match the server.

Focus first on changes to names and shapes:

```diff
 type Query {
-  product(id: ID!): Product
+  productById(id: ID!): Product
 }
```

This is a breaking change because an existing operation that selects `product` will no longer validate:

```graphql
query ProductName($id: ID!) {
  product(id: $id) {
    name
  }
}
```

During review, prioritize these changes:

- Removed or renamed types, fields, arguments, input fields, enum values, directives, interfaces, or union members
- Field, argument, and input type changes
- Nullability changes
- New required arguments or required input fields
- Enum, interface, and union expansion
- Default value changes
- Deprecation and opt-in metadata changes
- Public type-system directives used by client code generators or tooling
- Description changes, since descriptions are user-facing contract documentation even if they rarely break execution

## Classify schema changes

Use consistent labels for code review, CI, and release approval:

| Label           | Meaning                                                                                                                                  | Common examples                                                                                                                                                                                                                                                                                                                        | Review action                                                                                                  |
| --------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Safe            | Existing valid operations continue to validate and receive compatible shapes in most cases.                                              | Add an object type. Add a nullable output field. Add an optional argument. Add an optional input field. Add or improve a description. Add a deprecation reason.                                                                                                                                                                        | Local review is often enough. Validate with Nitro when your release process requires it.                       |
| Dangerous       | The schema remains valid for existing operations, but clients or tooling can still fail because behavior or exhaustive handling changes. | Add an enum value. Add an interface implementation. Add a union member. Change a default value. Change description semantics. Add or change a public directive used by tooling. Change a nullable output to non-null when data can still be null.                                                                                      | Require owner review. Consider treating dangerous changes as breaking in Nitro.                                |
| Breaking        | Existing operations, generated types, or tooling contracts can fail against the new schema.                                              | Remove or rename a type, field, argument, input field, or enum value. Remove a possible type. Change a field, argument, or input field type incompatibly. Add a required argument. Add a required input field. Change `String!` to `String` for typed clients. Remove deprecation metadata or directive metadata that clients rely on. | Deprecate first, migrate clients, or use an approved exception path.                                           |
| Client-breaking | A candidate schema change causes a published operation for the target stage to fail validation.                                          | Removing `Query.product` while a published web or mobile operation still selects it.                                                                                                                                                                                                                                                   | Do not publish until the client version is migrated or retired, unless an explicit approval permits the break. |

Examples:

| Change                      | Before                                 | After                                                     | Classification                                                     |
| --------------------------- | -------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------------------ |
| Add a nullable output field | `type Product { name: String! }`       | `type Product { name: String! description: String }`      | Safe in most cases.                                                |
| Add an enum value           | `enum Status { ACTIVE }`               | `enum Status { ACTIVE ARCHIVED }`                         | Dangerous, because clients may handle enums exhaustively.          |
| Remove a field              | `type Query { product: Product }`      | `type Query { catalog: Catalog }`                         | Breaking.                                                          |
| Add a required argument     | `type Query { products: [Product!]! }` | `type Query { products(category: String!): [Product!]! }` | Breaking. Existing calls omit `category`.                          |
| Loosen output nullability   | `type Product { name: String! }`       | `type Product { name: String }`                           | Breaking for typed clients that generated `string`, not `string?`. |

## Review nullability changes carefully

Nullability is part of your contract. It affects generated client types and how errors propagate at runtime.

| Change                                 | Example                                                                                            | Risk                                                                                             | Review action                                                           |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------- |
| Non-null output to nullable output     | `name: String!` to `name: String`                                                                  | Generated clients may need to change from non-null to nullable types.                            | Treat as breaking unless all clients can accept nullable values.        |
| Nullable output to non-null output     | `name: String` to `name: String!`                                                                  | Existing operations still validate, but execution bubbles an error if the resolver returns null. | Treat as dangerous. Prove backing data and resolvers never return null. |
| Optional argument to required argument | `products(category: String)` to `products(category: String!)`                                      | Existing operations that omit the argument fail validation.                                      | Breaking. Add a new field or provide a default before requiring it.     |
| Required argument to optional argument | `products(category: String!)` to `products(category: String)`                                      | Clients gain flexibility, but server behavior for omitted values must be defined.                | Usually safe after resolver review.                                     |
| Add optional input field               | `input ProductFilter { term: String }` to `input ProductFilter { term: String inStock: Boolean }`  | Existing input objects remain valid.                                                             | Usually safe.                                                           |
| Add required input field               | `input ProductFilter { term: String }` to `input ProductFilter { term: String category: String! }` | Existing variables and literals fail validation.                                                 | Breaking.                                                               |

A nullable-to-non-null output change is not automatically safe. When you make this change, the schema promises that null will never appear. If an old row, a backend failure, an authorization filter, or a resolver path can still produce null, GraphQL error propagation can null the parent selection and change the response shape.

## Validate the candidate schema against a Nitro stage

Use Nitro when your team keeps a schema registry or published client operations. `nitro schema validate` checks a local SDL file against the active schema for the target stage and the client versions published to that stage. Validation does not publish the candidate schema.

Run validation once for each deployment target that can have a different active schema or client set:

```bash
mkdir -p artifacts

dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "dev" \
  --schema-file artifacts/schema.graphqls
```

In GitHub Actions, keep the exported file as an artifact even when validation fails so reviewers can inspect the exact contract:

```yaml
- name: Export schema
  run: |
    mkdir -p artifacts
    dotnet run --project src/Catalog.Api -- \
      schema export --output artifacts/schema.graphqls

- name: Validate schema with Nitro
  env:
    NITRO_API_KEY: ${{ secrets.NITRO_API_KEY }}
    NITRO_API_ID: ${{ secrets.NITRO_API_ID }}
    NITRO_STAGE: dev
  run: |
    nitro schema validate \
      --api-id "$NITRO_API_ID" \
      --stage "$NITRO_STAGE" \
      --schema-file artifacts/schema.graphqls
```

A successful validation exits with `0`. A failed validation exits non-zero and can include schema change violations, invalid SDL errors, persisted operation errors, or registry errors.

Use the service API `nitro schema` commands for this page. Fusion gateways use `nitro fusion` commands.

## Use client-aware checks when clients publish operations

Schema-only validation answers, "Could this kind of schema change break a client?" Client-aware validation answers, "Does this candidate schema break an operation that is active for this stage?"

When you publish client versions to a stage, Nitro stores their persisted operations. Nitro can then validate future schema candidates against those operations. This is important because different clients have different lifecycles:

- A web app may have one active version, plus an old version during rollout
- A mobile app may have many active versions as users upgrade over time
- An internal service may pin one released operation set per deployment

A Relay-style operations file maps operation IDs to GraphQL documents:

```json
{
  "913abc361487c481cf6015841c0eca22": "query ProductName($id: ID!) { product(id: $id) { name } }"
}
```

To validate, upload, and publish client operations with Nitro:

```bash
nitro client validate \
  --client-id "$NITRO_CLIENT_ID" \
  --stage "dev" \
  --operations-file ./operations.json

nitro client upload \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "$GITHUB_SHA" \
  --operations-file ./operations.json

nitro client publish \
  --client-id "$NITRO_CLIENT_ID" \
  --tag "$GITHUB_SHA" \
  --stage "dev"
```

Once a client version is published to the stage, later schema validation can identify the client, version, operation, and GraphQL validation error that would break. See [Client Registry](/docs/nitro/apis/client-registry) and [`nitro client`](/docs/nitro/cli/client) for setup details.

## Configure registry strictness intentionally

Nitro API settings define your governance policy. Review these settings with API owners, not as one-off suppressions.

Use a strict schema policy when you want all schema-level breaking changes to fail:

```bash
nitro api set-settings "$NITRO_API_ID" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes false
```

Use a client-aware policy if you allow schema-level breaking changes only when no published client operation breaks:

```bash
nitro api set-settings "$NITRO_API_ID" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes true
```

| Setting                                 | Effect                                                                                                                     |
| --------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `--treat-dangerous-as-breaking true`    | Escalates dangerous changes, such as enum or possible-type additions, so they require the same review as breaking changes. |
| `--allow-breaking-schema-changes false` | Rejects schema-level breaking changes.                                                                                     |
| `--allow-breaking-schema-changes true`  | Allows schema-level breaking changes only when no published client operation for the stage breaks.                         |

## Prefer deprecation over removal

When you plan to replace part of your schema, always add the new contract before removing the old one.

```csharp
namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    [GraphQLDeprecated("Use `productById` instead.")]
    public static Product? GetProduct(int id, CatalogService catalog)
        => catalog.GetById(id);

    public static Product? GetProductById(int id, CatalogService catalog)
        => catalog.GetById(id);
}
```

This produces SDL like:

```graphql
type Query {
  product(id: Int!): Product @deprecated(reason: "Use `productById` instead.")
  productById(id: Int!): Product
}
```

You can deprecate output fields, input fields, arguments, and enum values. Use `[GraphQLDeprecated("reason")]`, `[Obsolete("reason")]`, descriptor `.Deprecated("reason")`, or SDL `@deprecated` depending on your schema setup.

A safe deprecation timeline:

1. Add the replacement field, argument, input field, or enum value
2. Deprecate the old member with a clear reason that names the replacement
3. Publish the additive schema
4. Notify client owners and set a support window
5. Validate and publish migrated client operations
6. Unpublish retired client versions after traffic drains
7. Remove the deprecated member in a later approved release

You cannot deprecate a non-null argument or non-null input field that has no default value. Make it optional, add a default, or introduce a replacement field first. Use `@requiresOptIn` for unstable additions that require explicit consumer consent, not as a removal substitute.

## Handle intentional breaking changes with approvals

Sometimes a breaking change is necessary: a product is retired, a client reaches end of life, or a security issue requires a contract change. Make these exceptions visible and reversible.

Before publishing an intentional break:

- Confirm the diff and its classification
- Identify affected clients and operations
- Get approval from API and client owners
- Publish migration notes and a support window
- Prepare a rollback tag
- Record the approval decision

Upload the schema with an immutable release tag, then publish to a gated stage:

```bash
nitro schema upload \
  --api-id "$NITRO_API_ID" \
  --tag "$RELEASE_TAG" \
  --schema-file artifacts/schema.graphqls

nitro schema publish \
  --api-id "$NITRO_API_ID" \
  --tag "$RELEASE_TAG" \
  --stage "production" \
  --wait-for-approval
```

If a client version has reached end of life, unpublish it from the stage instead of forcing the schema to support it:

```bash
nitro client unpublish \
  --client-id "$NITRO_CLIENT_ID" \
  --stage "production" \
  --tag "v1.8.0"
```

The `--wait-for-approval` flag holds deployment for review and times out if not approved. The `--force` flag skips confirmation prompts and can publish even when breaking changes exist. Use `--force` only for approved exceptional releases, never for regular pull request validation. You cannot use `--force` and `--wait-for-approval` together.

## Troubleshoot noisy or surprising diffs

| Symptom                                     | Likely cause                                                                                                                 | Fix                                                                                                |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| The diff only changes ordering.             | Registration or discovery order is nondeterministic.                                                                         | Stabilize schema registration and avoid reflection patterns that depend on file or assembly order. |
| The diff only changes descriptions.         | XML docs, attributes, or descriptor descriptions changed.                                                                    | Review as public documentation. Usually safe unless tooling depends on descriptions.               |
| `schema export` writes no SDL to stdout.    | `schema export` is file-oriented.                                                                                            | Use `schema print` when you need stdout.                                                           |
| The output path surprises you.              | Extensionless paths receive `.graphqls`; `.graphql` and `.graphqls` paths are preserved; a sibling settings file is written. | Use an explicit path such as `artifacts/schema.graphqls` and create the directory first.           |
| The wrong schema is checked.                | The app registers multiple schema names.                                                                                     | Run `dotnet run -- schema list`, then pass `--schema-name`.                                        |
| Nitro compares against the wrong baseline.  | The validation command targets the wrong stage.                                                                              | Pass the correct `--stage` and download the active stage schema for inspection.                    |
| Semantic non-null creates unexpected diffs. | Baseline and candidate use different export modes.                                                                           | Use the same `--semantic-non-null` mode for both files.                                            |
| Enum additions fail policy.                 | `treatDangerousAsBreaking` is enabled or clients handle enums exhaustively.                                                  | Update client handling or approve the policy exception.                                            |
| Validation references old operations.       | A stale client version is still published to the stage.                                                                      | Keep compatibility or unpublish the version after its end-of-life date.                            |
| Directive diffs are surprising.             | A directive is visible in SDL and may be tooling metadata.                                                                   | Decide whether the directive is part of the public contract before accepting the diff.             |

Useful inspection commands:

```bash
dotnet run --project src/Catalog.Api -- schema list

nitro schema download \
  --api-id "$NITRO_API_ID" \
  --stage "dev" \
  --output-file current.graphqls
```

## CI checklist

For pull requests and releases, follow this sequence:

1. Build the server and run schema snapshot tests
2. Export SDL from the configured ASP.NET Core app
3. Review or diff the exported baseline
4. Run `nitro schema validate` for each target stage if you use Nitro
5. Validate changed client operations when clients publish operation manifests
6. Upload schema and client artifacts with immutable tags during release
7. Publish to stages using the configured approval policy
8. Keep rollback schema tags and old client versions until traffic drains

A compact pipeline might look like:

```yaml
- run: dotnet test src/Catalog.Tests --filter Schema_Should_MatchSnapshot_When_Built
- run: dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
- run: git diff --exit-code -- artifacts/schema.graphqls
- run: nitro schema validate --api-id "$NITRO_API_ID" --stage "$NITRO_STAGE" --schema-file artifacts/schema.graphqls
- run: nitro client validate --client-id "$NITRO_CLIENT_ID" --stage "$NITRO_STAGE" --operations-file ./operations.json
```

A pull request should fail before merge if the schema contract or active client operations would break.

## Next steps

- Need export mechanics? Read [Export a schema](/docs/hotchocolate/v16/operations/schema-governance/export-schema).
- Need local test patterns? Read [Testing](/docs/hotchocolate/v16/guides/testing#test-schema-shape).
- Need schema design guidance? Read [Schema evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Need nullability details? Read [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null).
- Need directive background? Read [Directives](/docs/hotchocolate/v16/building-a-schema/directives).
- Need Nitro commands? Read [`nitro schema`](/docs/nitro/cli/schema), [`nitro client`](/docs/nitro/cli/client), and [`nitro api`](/docs/nitro/cli/api).
- Need registry concepts? Read [Schema Registry](/docs/nitro/apis/schema-registry), [Client Registry](/docs/nitro/apis/client-registry), and [Deployment](/docs/nitro/apis/deployments).
