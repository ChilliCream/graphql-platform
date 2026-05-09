---
title: Fusion subgraphs
---

A Fusion subgraph is a standard Hot Chocolate GraphQL server that provides a source schema to a larger composite schema. You continue to own and operate the ASP.NET Core service. The Fusion gateway manages query planning across services, communicates with each subgraph using its configured transport, and returns a single result to the client.

Use this page if you own a Hot Chocolate v16 service, such as Products or Reviews, and want it to participate in a Fusion graph. You will set a source schema name, add entity lookup metadata, export the source schema files, and prepare the subgraph for composition.

## What you will build

A subgraph setup involves four main components:

1. A Hot Chocolate server with a stable source schema name.
2. Local GraphQL types and fields owned by the service.
3. Lookup fields that allow the gateway to resolve entities by key.
4. Exported `schema.graphqls` and `schema-settings.json` files for composition.

The gateway and composition pipeline are separate topics. This page guides you up to the point where your subgraph is ready for composition.

## Before you start

You will need:

| Requirement                                                           | Why it matters                                                           |
| --------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| An ASP.NET Core Hot Chocolate v16 service                             | A Fusion subgraph is hosted as a GraphQL server.                         |
| `HotChocolate.AspNetCore`                                             | Provides the GraphQL server integration.                                 |
| `HotChocolate.AspNetCore.CommandLine`                                 | Enables `schema export` for CI and local composition.                    |
| `HotChocolate.Types.Analyzers` when using generated type registration | Provides `AddTypes()` and source-schema setup generated from attributes. |
| A stable domain key, such as `Product.Id`                             | Entity lookups are key based.                                            |

In v16, Fusion attributes are found in `HotChocolate.Types.Composite`. For new v16 subgraphs, do not use older Fusion source-schema packages or namespaces.

## Understanding the subgraph boundary

A typical Fusion graph looks like this:

```text
Client operation
  -> Fusion gateway
     -> Products subgraph
     -> Reviews subgraph
     -> Shipping subgraph
```

The client sends operations to the gateway. The gateway plans the operation and calls the subgraphs that can provide the requested fields. Subgraphs expose their local fields and lookup entry points. They should not call each other for normal graph traversal, as the gateway coordinates data fetching across subgraphs.

Consider each type at the boundary:

| Boundary shape            | Meaning                                                                                                                                            |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| Owned entity              | The subgraph owns the domain data and exposes a public lookup, for example Products owns `Product`.                                                |
| Entity reference          | The subgraph returns a key-only object so the gateway can continue resolving fields elsewhere, for example Reviews returns `Product(id)`.          |
| Contributed entity fields | The subgraph adds fields to an entity owned elsewhere and provides an internal lookup for those fields.                                            |
| Shared field              | A non-key field appears in multiple subgraphs and every definition is intentionally marked shareable with matching semantics.                      |
| External field            | A field is declared for composition metadata but is owned by another subgraph. This is less common in Fusion than in Apollo Federation migrations. |

Entity reference objects are schema boundary values. They are not stand-ins for database rows and should not include fields the service does not own.

## Configure the Hot Chocolate server

Begin with a standard Hot Chocolate server and assign it a source schema name.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("Products")
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

- `AddGraphQL("Products")` sets the source schema name. This name appears in the exported settings and must remain stable across composition, publishing, and deployment.
- `AddTypes()` registers the types discovered by the Hot Chocolate source generator. For attribute-based subgraphs, the generated registration also applies the source-schema defaults needed for Fusion source schemas.
- `RunWithGraphQLCommandsAsync(args)` enables commands such as `schema export` and returns an exit code. Return that exit code from `Program.cs` so command failures cause shell scripts and CI jobs to fail. The synchronous alternative is `return app.RunWithGraphQLCommands(args);`.

If you configure the request executor manually instead of using generated `AddTypes()`, ensure your builder applies `AddSourceSchemaDefaults()` so the schema is registered as a source schema.

## Add a lookup for an owned entity

A lookup is a query field that resolves a single entity by key. The gateway uses lookups when another subgraph returns a reference to the same entity.

```csharp
using HotChocolate.Types.Composite;

namespace Products.Types;

public sealed class Product
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public decimal Price { get; init; }
}

[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductRepository products,
        CancellationToken cancellationToken)
        => await products.GetByIdAsync(id, cancellationToken);
}
```

Key rules for lookups:

- Return a nullable single entity, such as `Product?` or `Task<Product?>`.
- Do not return a list or array from a lookup.
- Let missing keys return `null`.
- Use DataLoader or another batching pattern for database access in production services.

After export, the source schema should contain a query field with `@lookup` metadata. The composition step uses this metadata to determine how the gateway can enter this subgraph for `Product`.

## Map key arguments when names differ

If a lookup argument does not match the entity field name, map it with `[Is]`.

```csharp
using HotChocolate.Types.Composite;

[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductAsync(
        [Is(nameof(Product.Id))] int productId,
        IProductRepository products,
        CancellationToken cancellationToken)
        => await products.GetByIdAsync(productId, cancellationToken);
}
```

Use `[EntityKey]` when you need to declare key fields explicitly, such as with composite keys or when the key shape cannot be inferred from the lookup. A key declaration and a lookup path are related but distinct: the key describes identity, while the lookup describes how the gateway resolves an entity by that identity.

## Contribute fields to an entity owned elsewhere

A common scenario is a subgraph that does not own the entity, but owns fields that belong on that entity in the composite graph. For example, Reviews might store reviews by `ProductId` while Products owns `Product.name` and `Product.price`.

First, keep the local model accurate:

```csharp
namespace Reviews.Types;

public sealed class Review
{
    public int Id { get; init; }

    public int ProductId { get; init; }

    public required string Body { get; init; }
}

public sealed record Product(int Id);
```

Next, expose a GraphQL field that returns a key-only product reference, rather than exposing `productId` as the client-facing relationship.

```csharp
using HotChocolate.Types;

namespace Reviews.Types;

[ObjectType<Review>]
public static partial class ReviewType
{
    [BindMember(nameof(Review.ProductId))]
    public static Product GetProduct([Parent] Review review)
        => new(review.ProductId);
}
```

Finally, add an internal lookup so the gateway can enter the Reviews subgraph when it needs fields that Reviews contributes to `Product`.

```csharp
using HotChocolate.Types.Composite;

namespace Reviews.Types;

[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product? GetProductById(int id)
        => new(id);
}
```

`[Internal]` hides the lookup from the public composite schema, but the gateway can still use it during planning. Keep stubs minimal: include keys and fields owned by this subgraph, and avoid duplicating fields from the owning subgraph unless they are key fields or are intentionally marked `[Shareable]` everywhere they appear.

## Shareable and external fields

Most fields should have a single owner. If two subgraphs expose the same non-key field, composition treats that as ambiguous unless the field is marked shareable in every contributing source schema.

```csharp
using HotChocolate.Types.Composite;

public sealed class Product
{
    public int Id { get; init; }

    [Shareable]
    public required string DisplayName { get; init; }
}
```

Only mark a field `[Shareable]` when each subgraph returns the same meaning for that field. If the values can differ by service, select one owner or model separate fields.

External fields are mainly useful when migrating Federation-style schemas or for advanced composition patterns. In new Fusion-first Hot Chocolate subgraphs, prefer entity references, lookups, `[Require]`, and field ownership rules before using external declarations. For Apollo Federation syntax and migration guidance, see [Apollo Federation Subgraph Support](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation) and [Coming from Apollo Federation](/docs/fusion/v16/migration/coming-from-apollo-federation).

## Export the source schema

Run the export command from your solution or repository root:

```bash
dotnet run --project ./Products -- schema export
```

A Fusion source schema export writes two files next to the project by default:

| File                   | Purpose                                                                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `schema.graphqls`      | The subgraph SDL, including composite directives such as `@lookup`, `@internal`, `@key`, and `@shareable` when used. |
| `schema-settings.json` | The source schema settings consumed by composition and publishing tools.                                             |

A settings file for Products might look like this:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "http://localhost:5001/graphql"
    }
  }
}
```

Check these fields before composition:

- `name` is unique in the composite schema and matches the source schema name from `AddGraphQL("Products")`.
- `transports.http.url` points to the GraphQL endpoint that the gateway can reach in the current environment.

Use environment-specific settings for local development, CI, staging, and production.

## Local development workflow

A productive local workflow includes:

1. Running each subgraph on its configured port.
2. Querying local fields directly against each subgraph endpoint.
3. Exporting each source schema with `dotnet run --project ./ServiceName -- schema export`.
4. Inspecting the SDL for expected directives such as `@lookup`, `@internal`, `@key`, and `@shareable`.
5. Checking each `schema-settings.json` name and URL.
6. Running Fusion composition with the exported source schemas.
7. Querying the composed graph through the gateway.

Composition is the next validation layer. The Nitro Fusion CLI can compose local source schemas, watch files, publish to Nitro, and produce a Fusion archive. See [Fusion CLI](/docs/fusion/v16/cli) and [Composition](/docs/fusion/v16/composition) for more details.

## Production checklist

Before handing a subgraph to a gateway or registry, verify the following:

- The source schema name is stable and unique.
- The GraphQL endpoint URL in `schema-settings.json` is reachable from the gateway runtime.
- Owned entities have public lookup fields where clients or other subgraphs need to enter the owner.
- Contributed entity fields have an internal or public lookup path.
- Lookups return nullable single entities.
- Non-key fields have one owner unless every definition is marked `[Shareable]` and has matching semantics.
- Key argument mappings use `[Is]` when argument names or shapes differ.
- Exported schema files are produced by the v16 project, not copied from an older setup.
- CI runs schema export and composition before publishing or deploying gateway artifacts.

## Troubleshooting

| Symptom                                                          | What to check                                                                                                                                           |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Exported SDL has no `@lookup` directives                         | Confirm the resolver uses `[Lookup]` from `HotChocolate.Types.Composite`, generated type registration is active, and stale packages are not referenced. |
| Analyzer warns that a lookup returns a non-null entity           | Change the return type to a nullable entity, for example `Product?` or `Task<Product?>`.                                                                |
| Analyzer errors that a lookup returns a list or array            | Return one entity per lookup field. Use batching inside the resolver rather than returning a collection.                                                |
| Composition reports a duplicated field                           | Select one owner, or mark the field `[Shareable]` in every source schema and align the field type and meaning.                                          |
| Composition cannot move to the subgraph that contributes a field | Add a compatible public or internal lookup for that entity in the contributing subgraph.                                                                |
| A lookup argument does not map to the entity key                 | Add `[Is]` to map the argument to the entity field, or declare the key explicitly with `[EntityKey]` when needed.                                       |
| The gateway calls the wrong endpoint locally                     | Update `transports.http.url` in `schema-settings.json` for the local port.                                                                              |
| A project still has old composition settings                     | Export fresh `schema.graphqls` and `schema-settings.json` from the v16 project and update the Nitro composition inputs.                                 |

## API quick reference

| API                                 | Use it for                                                                                        |
| ----------------------------------- | ------------------------------------------------------------------------------------------------- |
| `AddGraphQL("Name")`                | Names the source schema.                                                                          |
| `AddTypes()`                        | Registers generated Hot Chocolate types and source-schema setup.                                  |
| `AddSourceSchemaDefaults()`         | Applies source-schema defaults when manually configuring the executor.                            |
| `RunWithGraphQLCommandsAsync(args)` | Enables command-line schema export and returns an exit code.                                      |
| `[Lookup]`                          | Marks a query field as an entity lookup path.                                                     |
| `[Internal]`                        | Hides helper fields from the public composite schema while keeping them available to the gateway. |
| `[Is]`                              | Maps a lookup argument to an entity field.                                                        |
| `[EntityKey]`                       | Declares entity key fields explicitly when inference is not enough.                               |
| `[Shareable]`                       | Allows intentional shared ownership of a non-key field.                                           |

The Fusion attributes in this table are found in `HotChocolate.Types.Composite`.

## Next steps

- Explore entity resolution in more depth with [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).
- Learn about ownership rules with [Field Ownership and Sharing](/docs/fusion/v16/field-ownership-and-sharing).
- Map required data with [Data Requirements and Mapping](/docs/fusion/v16/data-requirements-and-mapping).
- Add another source schema with [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph).
- Compose local schemas with [Fusion CLI](/docs/fusion/v16/cli) and [Composition](/docs/fusion/v16/composition).
- Deploy the composed graph with [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd).
- Review Hot Chocolate server basics in [Command Line](/docs/hotchocolate/v16/build/server-configuration/command-line), [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types), [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types), [Resolvers](/docs/hotchocolate/v16/build/resolvers), and [DataLoader](/docs/hotchocolate/v16/build/dataloader).
- For Federation-compatible subgraph syntax, see [Apollo Federation Subgraph Support](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation).
