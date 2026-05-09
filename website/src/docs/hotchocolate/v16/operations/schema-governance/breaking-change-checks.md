---
title: Breaking-change checks
---

# Breaking-change checks

Export or snapshot the Hot Chocolate schema before you merge, then check the candidate contract against the baseline your clients use. A good breaking-change workflow catches accidental removals, risky nullability changes, new required inputs, and client operation failures before they reach production.

This page covers one Hot Chocolate v16 server schema. Fusion gateway composition has a separate governance workflow and uses the [`nitro fusion`](/docs/nitro/cli/fusion) commands.

```bash
mkdir -p artifacts

dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

nitro schema validate \
  --api-id "$NITRO_API_ID" \
  --stage "$NITRO_STAGE" \
  --schema-file artifacts/schema.graphqls
```

Expected `schema export` output:

```text
Exported Files:
- /repo/artifacts/schema.graphqls
- /repo/artifacts/schema-settings.json
```

`nitro schema validate` exits with `0` when the candidate schema is valid for the target stage. It exits with a non-zero code and prints validation errors when schema changes or published client operations would break.

## Prerequisites

You need:

- A Hot Chocolate v16 ASP.NET Core server project.
- A reference to `HotChocolate.AspNetCore.CommandLine` in the server project.
- A `Program.cs` that returns the command exit code.
- The configuration needed to build the same schema that production serves.
- A test project that can build an `IRequestExecutor`, when you use local snapshots.
- Optional Nitro CLI authentication through `nitro login`, `--api-key`, or `NITRO_API_KEY`.
- Optional Nitro API ID, stage name, and published client operations, when you use registry checks.

Wire the command-line package into the real application host:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

Returning the `Task<int>` from `RunWithGraphQLCommandsAsync(args)` lets schema construction and command failures fail CI. See [Command Line](/docs/hotchocolate/v16/server/command-line) for setup details.

## Run the local schema check first

Start every pull request with a local contract check. This is the fastest feedback loop because it does not require a registry account.

Use a schema snapshot when your test suite already builds the executable schema:

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

When the snapshot fails, treat the mismatch as a contract review. Do not accept a new snapshot until you classify the change and decide whether clients need migration time.

Use CLI export when reviewers, CI artifacts, code generation, or Nitro need a standalone SDL file:

```bash
mkdir -p artifacts

dotnet run --project src/Catalog.Api -- \
  schema export --output artifacts/schema.graphqls

git diff --exit-code -- artifacts/schema.graphqls
```

Expected results:

- `artifacts/schema.graphqls` contains the exported SDL.
- `artifacts/schema-settings.json` contains schema metadata for tools.
- `git diff --exit-code` fails when the exported baseline changed.

Use `schema print` when a script needs SDL on stdout. `schema export` writes files.

## Read the diff as a contract change

The GraphQL schema is the public type-level contract between your server and its clients. A schema diff is not a text formatting event. It tells you whether existing operations, generated client types, normalized caches, persisted operation manifests, and developer documentation still match the server.

Look first for changes to names and shapes:

```diff
 type Query {
-  product(id: ID!): Product
+  productById(id: ID!): Product
 }
```

This is a breaking change because an existing operation that selects `product` no longer validates:

```graphql
query ProductName($id: ID!) {
  product(id: $id) {
    name
  }
}
```

During review, prioritize:

- Removed or renamed types, fields, arguments, input fields, enum values, directives, interfaces, and union members.
- Field, argument, and input type changes.
- Nullability changes.
- New required arguments or required input fields.
- Enum, interface, and union expansion.
- Default value changes.
- Deprecation and opt-in metadata changes.
- Public type-system directives that client code generators or tooling consume.
- Description changes, because descriptions are user-facing contract documentation even when they rarely break execution.

## Classify schema changes

Use the same labels across code review, CI, and release approval.

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

Nullability is part of the contract. It affects generated client types and runtime error propagation.

| Change                                 | Example                                                                                            | Risk                                                                                             | Review action                                                           |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------- |
| Non-null output to nullable output     | `name: String!` to `name: String`                                                                  | Generated clients may need to change from non-null to nullable types.                            | Treat as breaking unless all clients can accept nullable values.        |
| Nullable output to non-null output     | `name: String` to `name: String!`                                                                  | Existing operations still validate, but execution bubbles an error if the resolver returns null. | Treat as dangerous. Prove backing data and resolvers never return null. |
| Optional argument to required argument | `products(category: String)` to `products(category: String!)`                                      | Existing operations that omit the argument fail validation.                                      | Breaking. Add a new field or provide a default before requiring it.     |
| Required argument to optional argument | `products(category: String!)` to `products(category: String)`                                      | Clients gain flexibility, but server behavior for omitted values must be defined.                | Usually safe after resolver review.                                     |
| Add optional input field               | `input ProductFilter { term: String }` to `input ProductFilter { term: String inStock: Boolean }`  | Existing input objects remain valid.                                                             | Usually safe.                                                           |
| Add required input field               | `input ProductFilter { term: String }` to `input ProductFilter { term: String category: String! }` | Existing variables and literals fail validation.                                                 | Breaking.                                                               |

A nullable-to-non-null output change is not automatically safe. The schema now promises that null never appears. If an old row, partial backend failure, authorization filter, or resolver path can still produce null, GraphQL error propagation can null the parent selection and change the response shape.

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

Nitro client versions contain persisted operations. When you publish a client version to a stage, Nitro can validate future schema candidates against those operations. This matters because different clients have different lifecycles:

- A web app may have one active version, plus an old version during rollout.
- A mobile app may keep many active versions because users upgrade over time.
- An internal service may pin one released operation set per deployment.

A Relay-style operations file maps operation IDs to GraphQL documents:

```json
{
  "913abc361487c481cf6015841c0eca22": "query ProductName($id: ID!) { product(id: $id) { name } }"
}
```

Validate, upload, and publish client operations with Nitro:

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

After a client version is published to the stage, later schema validation can identify the client, version, operation, and GraphQL validation error that would break. See [Client Registry](/docs/nitro/apis/client-registry) and [`nitro client`](/docs/nitro/cli/client) for setup details.

## Configure registry strictness intentionally

Nitro API settings are governance policy. Review them with API owners instead of treating them as per-change suppressions.

Use a strict schema policy when schema-level breaking changes must always fail:

```bash
nitro api set-settings "$NITRO_API_ID" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes false
```

Use a client-aware policy when you allow schema-level breaking changes only if no published client operation breaks:

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

For planned replacements, add the new contract before you remove the old one.

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

Expected SDL:

```graphql
type Query {
  product(id: Int!): Product @deprecated(reason: "Use `productById` instead.")
  productById(id: Int!): Product
}
```

Deprecation applies to output fields, input fields, arguments, and enum values. You can use `[GraphQLDeprecated("reason")]`, `[Obsolete("reason")]`, descriptor `.Deprecated("reason")`, or SDL `@deprecated` depending on how you build the schema.

Use this timeline:

1. Add the replacement field, argument, input field, or enum value.
2. Deprecate the old member with an actionable reason that names the replacement.
3. Publish the additive schema.
4. Notify client owners and set a support window.
5. Validate and publish migrated client operations.
6. Unpublish retired client versions after traffic drains.
7. Remove the deprecated member in a later approved release.

You cannot deprecate a non-null argument or non-null input field that has no default value. Make it optional, add a default, or introduce a replacement field first. Use `@requiresOptIn` for unstable additions that require explicit consumer consent, not as a removal substitute.

## Handle intentional breaking changes with approvals

Sometimes a break is intentional: a product is retired, a client reached end of life, or a security issue requires a contract change. Make the exception visible and reversible.

Before publishing:

- Confirm the diff and classification.
- Identify affected clients and operations.
- Get approval from API and client owners.
- Publish migration notes and a support window.
- Prepare a rollback tag.
- Record the approval decision.

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

If a client version has reached end of life, unpublish it from the stage rather than forcing the schema around it:

```bash
nitro client unpublish \
  --client-id "$NITRO_CLIENT_ID" \
  --stage "production" \
  --tag "v1.8.0"
```

`--wait-for-approval` holds the deployment for review and times out if it is not approved. `--force` skips confirmation prompts and can publish even when breaking changes exist. Reserve `--force` for approved exceptional releases. Do not use `--force` in regular pull request validation. `--force` and `--wait-for-approval` are mutually exclusive.

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

Use this sequence for pull requests and releases:

1. Build the server and run schema snapshot tests.
2. Export SDL from the configured ASP.NET Core app.
3. Review or diff the exported baseline.
4. Run `nitro schema validate` for each target stage when you use Nitro.
5. Validate changed client operations when clients publish operation manifests.
6. Upload schema and client artifacts with immutable tags during release.
7. Publish to stages using the configured approval policy.
8. Keep rollback schema tags and old client versions until traffic drains.

A compact pipeline shape:

```yaml
- run: dotnet test src/Catalog.Tests --filter Schema_Should_MatchSnapshot_When_Built
- run: dotnet run --project src/Catalog.Api -- schema export --output artifacts/schema.graphqls
- run: git diff --exit-code -- artifacts/schema.graphqls
- run: nitro schema validate --api-id "$NITRO_API_ID" --stage "$NITRO_STAGE" --schema-file artifacts/schema.graphqls
- run: nitro client validate --client-id "$NITRO_CLIENT_ID" --stage "$NITRO_STAGE" --operations-file ./operations.json
```

A pull request should fail before merge when the schema contract or active client operations would break.

## Next steps

- Need export mechanics? Read [Export a schema](/docs/hotchocolate/v16/operations/schema-governance/export-schema).
- Need local test patterns? Read [Testing](/docs/hotchocolate/v16/guides/testing#test-schema-shape).
- Need schema design guidance? Read [Schema evolution](/docs/hotchocolate/v16/guides/schema-evolution) and [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Need nullability details? Read [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null).
- Need directive background? Read [Directives](/docs/hotchocolate/v16/building-a-schema/directives).
- Need Nitro commands? Read [`nitro schema`](/docs/nitro/cli/schema), [`nitro client`](/docs/nitro/cli/client), and [`nitro api`](/docs/nitro/cli/api).
- Need registry concepts? Read [Schema Registry](/docs/nitro/apis/schema-registry), [Client Registry](/docs/nitro/apis/client-registry), and [Deployment](/docs/nitro/apis/deployments).
