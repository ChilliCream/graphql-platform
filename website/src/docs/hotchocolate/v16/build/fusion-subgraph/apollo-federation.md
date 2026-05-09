---
title: Apollo Federation compatibility
---

Use Apollo Federation compatibility when you need a Hot Chocolate v16 service to work with Apollo Federation tooling, such as Apollo Router, GraphOS, or an Apollo Federation composition pipeline. The service remains a standard Hot Chocolate server, but it exposes Federation schema metadata and protocol fields that Apollo tools recognize.

If your service will be composed by Hot Chocolate Fusion, begin with [Fusion subgraphs](/docs/hotchocolate/v16/build/fusion-subgraph). Fusion native subgraphs use composite schema metadata like `[Lookup]`, `[EntityKey]`, `[Is]`, and `[Require]`. Apollo Federation metadata uses `[Key]`, `[ReferenceResolver]`, `_service`, and `_entities`. While these models are related at the architecture level, they do not share the same API surface.

| Use case                                                            | Start here                                                                                                |
| ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| A Hot Chocolate service must compose with Apollo Federation tooling | This page                                                                                                 |
| A Hot Chocolate service will be composed by Fusion or Nitro         | [Fusion subgraphs](/docs/hotchocolate/v16/build/fusion-subgraph)                                          |
| You are migrating a Federation graph to Fusion                      | [Coming from Apollo Federation](/docs/fusion/v16/migration/coming-from-apollo-federation)                 |
| You need an attribute-by-attribute reference                        | [Apollo Federation attributes](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation-attributes) |

## Install and register Federation support

First, install the `HotChocolate.ApolloFederation` package.

<PackageInstallation packageName="HotChocolate.ApolloFederation"/>

Next, register Apollo Federation on the GraphQL builder:

```csharp
using HotChocolate.ApolloFederation;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddApolloFederation()
    .AddQueryType<Query>()
    .AddType<Product>();
```

The `AddApolloFederation()` method enables Federation SDL metadata and adds the Federation protocol fields. In v16, the default is `FederationVersion.Federation26`. Specify a version explicitly if Apollo tooling or a directive requires it:

```csharp
builder
    .AddGraphQL()
    .AddApolloFederation(FederationVersion.Federation27);
```

Hot Chocolate v16 supports `Federation10` and Federation v2.0 through v2.7. Federation v2 output uses `schema @link(...)` imports for the directives present in the schema.

## Define an owned entity

An Apollo Federation entity is an object that can be identified across subgraphs. The owner subgraph typically exposes the domain data and defines at least one key.

```csharp
using HotChocolate;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    [ID]
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal Price { get; init; }
}
```

The generated Federation SDL includes the key:

```graphql
type Product @key(fields: "id") {
  id: ID!
  name: String!
  price: Decimal!
}
```

If the key consists of a single field, you can also mark the property directly:

```csharp
public sealed class Product
{
    [ID]
    [Key]
    public string Id { get; init; } = default!;
}
```

For descriptor-based configuration, use `Key()` on the type descriptor:

```csharp
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(t => t.Id).ID();
        descriptor.Key("id");
    }
}
```

Field sets use GraphQL field names, not C# member names. If you rename a field, use the final schema name in the field set.

## Resolve entity references

Apollo Router sends entity representations to the subgraph through `_entities`. Hot Chocolate maps each representation to a reference resolver. The resolver loads the local entity by key and may return `null` if the entity no longer exists.

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal Price { get; init; }

    [ReferenceResolver]
    public static Task<Product?> ResolveReferenceAsync(
        string id,
        ProductByIdDataLoader products,
        CancellationToken cancellationToken)
        => products.LoadAsync(id, cancellationToken);
}
```

Reference resolver rules:

| Rule                                               | Why it matters                                                   |
| -------------------------------------------------- | ---------------------------------------------------------------- |
| Match parameter names to GraphQL key field names   | The representation is keyed by GraphQL names.                    |
| Match CLR parameter types to representation values | Hot Chocolate binds the representation into resolver parameters. |
| Return `T?` or `Task<T?>` for missing entities     | Routers can request entities that no longer exist.               |
| Add one resolver for each resolvable key shape     | Each key can arrive as an `_entities` representation.            |
| Use DataLoader or another batching pattern         | Routers can send many representations in one request.            |

With descriptors, chain `ResolveReferenceWith()` from `Key()`:

```csharp
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Key("id")
            .ResolveReferenceWith(_ => ResolveProductByIdAsync(default!, default!, default));
    }

    private static Task<Product?> ResolveProductByIdAsync(
        string id,
        ProductByIdDataLoader products,
        CancellationToken cancellationToken)
        => products.LoadAsync(id, cancellationToken);
}
```

## Add multiple, compound, and nested keys

A type can expose more than one key. Add a resolver for every key that this subgraph can resolve.

```csharp
[Key("id")]
[Key("sku")]
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Sku { get; init; } = default!;

    [ReferenceResolver]
    public static Product? ResolveById(string id, ProductRepository products)
        => products.FindById(id);

    [ReferenceResolver]
    public static Product? ResolveBySku(string sku, ProductRepository products)
        => products.FindBySku(sku);
}
```

Compound keys use a field set with multiple fields:

```csharp
[Key("sku package")]
public sealed class Product
{
    public string Sku { get; init; } = default!;

    public string Package { get; init; } = default!;

    [ReferenceResolver]
    public static Product? ResolveBySkuAndPackage(
        string sku,
        string package,
        ProductRepository products)
        => products.FindBySkuAndPackage(sku, package);
}
```

Nested keys use a nested selection set. Use `[Map]` when a resolver parameter reads a nested value from the representation:

```csharp
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("sku variation { id }")]
public sealed class Product
{
    public string Sku { get; init; } = default!;

    public ProductVariation Variation { get; init; } = default!;

    [ReferenceResolver]
    public static Product? ResolveByVariation(
        string sku,
        [Map("variation.id")] string variationId,
        ProductRepository products)
        => products.FindByVariation(sku, variationId);
}

public sealed class ProductVariation
{
    public string Id { get; init; } = default!;
}
```

A nested representation matches the shape of the field set:

```json
{
  "__typename": "Product",
  "sku": "apollo-federation-shirt",
  "variation": { "id": "red-xl" }
}
```

Use `resolvable: false` when this subgraph can mention an entity key but should not resolve that entity through `_entities`:

```csharp
[Key("id", resolvable: false)]
public sealed class ProductReference
{
    public string Id { get; init; } = default!;
}
```

## Reference an entity owned by another subgraph

A subgraph can return or extend an entity that another subgraph owns. Keep the local model focused on the fields this service knows. The type name and key must match the composed Federation entity.

```csharp
using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

[Key("id")]
[ExtendServiceType]
public sealed class Product
{
    [ID]
    public string Id { get; init; } = default!;

    public IReadOnlyList<Review> Reviews { get; init; } = [];

    [ReferenceResolver]
    public static Product ResolveReference(string id)
        => new() { Id = id };
}
```

The resolver returns a key-only object for the local contribution. Do not call another subgraph from this resolver to hydrate fields owned elsewhere. The router coordinates cross-subgraph fetching.

For Federation v2 schemas, a matching type name and key are often enough for entity merging. Use `[ExtendServiceType]` for v1-compatible extension metadata or for schemas that already rely on the extension shape. Mark a field `[External]` if it is owned by another subgraph and this subgraph references it from `@requires` or `@provides`.

## Use common Federation directives

This section covers the directives most often used in subgraph authoring. For the full v16 attribute reference, see [Apollo Federation attributes](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation-attributes).

### `@external`

Apply `[External]` to a field that this subgraph declares but another subgraph owns:

```csharp
[Key("id")]
public sealed class User
{
    public int Id { get; init; }

    [External]
    public string? FirstName { get; init; }

    [External]
    public string? LastName { get; init; }
}
```

In Federation v2, key fields do not require `@external` solely because they are keys.

### `@requires`

Use `[Requires]` when a local field depends on external fields to compute its value:

```csharp
[Key("id")]
public sealed class User
{
    public int Id { get; init; }

    [External]
    public string? FirstName { get; init; }

    [External]
    public string? LastName { get; init; }

    [Requires("firstName lastName")]
    public string? FullName =>
        FirstName is null && LastName is null
            ? null
            : $"{FirstName} {LastName}".Trim();
}
```

Generated SDL:

```graphql
type User @key(fields: "id") {
  id: Int!
  firstName: String @external
  lastName: String @external
  fullName: String @requires(fields: "firstName lastName")
}
```

### `@provides`

Apply `[Provides]` to a field that returns an entity when this subgraph can provide selected fields of that entity along this path:

```csharp
[Key("id")]
public sealed class Review
{
    public int Id { get; init; }

    [Provides("name")]
    public Product Product { get; init; } = default!;
}

[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [External]
    public string Name { get; init; } = default!;
}
```

Generated SDL:

```graphql
type Review @key(fields: "id") {
  id: Int!
  product: Product! @provides(fields: "name")
}

type Product @key(fields: "id") {
  id: Int!
  name: String! @external
}
```

### `@shareable`

Use `[Shareable]` when more than one subgraph intentionally resolves the same field or object with the same meaning:

```csharp
[Key("id")]
public sealed class Product
{
    public int Id { get; init; }

    [Shareable]
    public string Name { get; init; } = default!;
}
```

Federation v2 treats key fields as shareable. Use `[Shareable]` explicitly for non-key fields or object types that multiple subgraphs resolve intentionally.

## Inspect the Federation SDL

Query `_service { sdl }` against the subgraph to view the SDL that Apollo Federation tooling reads:

```graphql
query {
  _service {
    sdl
  }
}
```

A Federation v2 schema includes a `@link` to the Federation specification and imports the used directives:

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: ["@key", "@tag", "FieldSet"]
  ) {
  query: Query
}

type Query {
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type Product @key(fields: "id") {
  id: ID!
  name: String!
}

union _Entity = Product

scalar _Any
scalar FieldSet
```

The `_service { sdl }` field is a Federation protocol inspection surface. It is not the same as Fusion schema export. Fusion source schemas are exported for Fusion composition and contain composite schema metadata instead.

## Test reference resolvers with `_entities`

Use `_entities` in tests to verify that representations bind to the expected reference resolver. Normal API clients should query the public graph, not `_entities`.

```graphql
query ResolveProduct($representations: [_Any!]!) {
  _entities(representations: $representations) {
    ... on Product {
      id
      name
      price
    }
  }
}
```

Variables:

```json
{
  "representations": [{ "__typename": "Product", "id": "1" }]
}
```

Expected result shape:

```json
{
  "data": {
    "_entities": [
      {
        "id": "1",
        "name": "Chilli Sauce",
        "price": 12.95
      }
    ]
  }
}
```

Also run one `_entities` query for each compound or nested key shape that the subgraph resolves. This helps catch missing `[Map]` paths, mismatched GraphQL field names, and type conversion issues before composition.

## Compatibility notes and limitations

- Hot Chocolate provides the Federation-compatible subgraph schema and runtime protocol fields.
- Apollo tooling handles Federation composition, query planning, registry publishing, and router deployment.
- Fusion composition uses Fusion source schema metadata. `[Key]` and `[ReferenceResolver]` do not create Fusion `[Lookup]` metadata.
- If you are moving from Apollo Federation to Fusion, model entity lookups with Fusion attributes and validate the exported source schemas with Fusion composition.
- Router authorization directives such as `[Authenticated]`, `[RequiresScopes]`, and `[Policy]` emit Federation metadata. They do not replace Hot Chocolate authorization in the subgraph.
- Field set strings for `[Key]`, `[Requires]`, and `[Provides]` are GraphQL selection sets without outer braces.

## Troubleshooting

| Symptom                                       | Likely cause                                                                                         | Fix                                                                                                      |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `_service` is missing                         | Federation support was not registered                                                                | Add `.AddApolloFederation()` to the GraphQL builder.                                                     |
| `_entities` is missing                        | The schema has no resolvable entity types                                                            | Add a type with `[Key]` or `descriptor.Key(...)`, and register the type.                                 |
| A type is missing from `_Entity`              | The type has no key, is not registered, or only has non-resolvable keys                              | Register the type and add a resolvable key when this subgraph resolves it.                               |
| Reference resolver is not called              | Parameter names or CLR types do not match the key representation                                     | Match GraphQL field names and representation value types.                                                |
| Nested key value is `null`                    | The resolver parameter is not mapped to the nested representation path                               | Add `[Map("path.to.field")]` to the parameter.                                                           |
| Composition reports external field errors     | A field used by `[Requires]` or `[Provides]` is not declared as external where Federation expects it | Add `[External]` to fields owned by another subgraph.                                                    |
| Composition reports duplicate field ownership | Multiple subgraphs define the same non-key field                                                     | Pick one owner or mark the field `[Shareable]` in every subgraph that resolves it with the same meaning. |
| Fusion composition does not see a lookup      | The subgraph uses Apollo Federation attributes                                                       | Use Fusion `[Lookup]` and related composite schema attributes for Fusion native subgraphs.               |
| `_entities` queries are slow                  | Reference resolvers load one entity at a time                                                        | Use DataLoader or another batching pattern in reference resolvers.                                       |

## Next steps

- Use [Apollo Federation attributes](/docs/hotchocolate/v16/build/fusion-subgraph/apollo-federation-attributes) for directive details and descriptor equivalents.
- Build Fusion native subgraphs with [Fusion subgraphs](/docs/hotchocolate/v16/build/fusion-subgraph).
- Compare migration paths in [Coming from Apollo Federation](/docs/fusion/v16/migration/coming-from-apollo-federation).
- Model Fusion lookups with [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).
- Review field ownership with [Field Ownership and Sharing](/docs/fusion/v16/field-ownership-and-sharing).
- Use [DataLoader](/docs/hotchocolate/v16/build/dataloader) for batching reference resolver data access.
