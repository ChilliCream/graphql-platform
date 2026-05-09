---
title: "Relay"
---

Use Relay-style schema helpers when the same entity appears through many query paths. A product can show up in a catalog list, a detail screen, a mutation result, and a search result. Clients need those appearances to represent one record in a normalized cache, and they need predictable ways to refetch records and traverse lists.

Hot Chocolate v16 provides server-side schema conventions for stable identity, refetchable entities, cursor connections, and mutation payloads. These conventions help Relay clients, Apollo-style normalized caches, Strawberry Shake, and custom clients. You do not need to use Relay JS to use these schema patterns.

This page is a map. It explains the pieces, shows short previews, and routes you to the focused implementation pages.

# Preview the schema you are working toward

A Relay-friendly catalog schema commonly exposes globally identified entities, a `Node` refetch field, and connection fields for large lists:

```graphql
interface Node {
  id: ID!
}

type Product implements Node {
  id: ID!
  name: String!
}

type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
  products(first: Int, after: String): ProductsConnection
}

type ProductsConnection {
  nodes: [Product!]
  edges: [ProductsEdge!]
  pageInfo: PageInfo!
}

type ProductsEdge {
  cursor: String!
  node: Product!
}
```

Use this shape as a target, not as a rule that every object must implement `Node`.

# Understand the four pieces

| Piece                        | Client problem                                                                   | Hot Chocolate entry point                                      | Learn the details                                              |
| ---------------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------- |
| Global ID                    | A raw key such as `1` is not unique across object types.                         | `[ID]`, `[ID<T>]`, `.ID(...)`                                  | [Global Identifiers](./global-identifiers)                     |
| Node refetch                 | A client has an ID and needs the object without knowing the original query path. | `.AddGlobalObjectIdentification()`, `[Node]`, `[NodeResolver]` | [Global Identifiers](./global-identifiers)                     |
| Connection                   | A client needs bounded, cursor-based traversal of a collection.                  | `[UsePaging]`, `.UsePaging()`, `Connection<T>`                 | [Connections](./connections)                                   |
| Mutation payload query field | A mutation response needs optional root query access for post-write reads.       | `.AddQueryFieldToMutationPayloads()`                           | [Mutation Payload Query Field](./mutation-payload-query-field) |

Complex IDs are a global ID scenario. If your public identity combines a tenant and local ID, SKU and batch, or another composite key, model that key as one server-side value and keep the serialized ID opaque. See [Complex IDs](./complex-ids).

The GraphQL `ID` scalar alone does not create the full Relay model. For Relay-style identity, expose stable, globally unique values. For refetch, add `Node` support and a resolver that can load the entity by its ID.

# Choose what to add

| Situation                                                                        | Use                            | Avoid                                                                              |
| -------------------------------------------------------------------------------- | ------------------------------ | ---------------------------------------------------------------------------------- |
| An entity has stable identity and appears through multiple query paths.          | Global ID                      | Public `Int` or `Guid` IDs that can collide across types.                          |
| An entity should be refetchable when the client only has its ID.                 | `Node` support                 | `Node` on transient values, aggregates with no stable lookup, or computed objects. |
| A field returns a large or user-scrollable collection.                           | Connection                     | Unbounded lists that force clients to load everything at once.                     |
| An entity uses a tenant plus local ID, SKU plus batch, or another composite key. | Complex ID value               | Multiple public key fields as the Relay identity.                                  |
| A mutation needs to let clients read related root data in the same operation.    | Mutation payload `query` field | A weak payload that relies on `query` for the main changed data.                   |

Consider Relay conventions for entity-heavy APIs and clients that normalize by `id` and `__typename`. Use plain fields for value objects, transient projections, small embedded collections, and data with no stable identity.

# Follow a typical implementation path

## Expose stable global IDs

Mark public ID fields with `[ID]`. Your resolver code can keep using the raw CLR value, while clients see an opaque `ID` value.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

public sealed class Product
{
    [ID]
    public int Id { get; init; }

    public required string Name { get; init; }
}
```

Use `[ID<T>]` for IDs that refer to another GraphQL type, such as foreign keys in arguments or input objects. In v16, `[ID<T>]` uses the configured GraphQL type name for `T`. Continue with [Global Identifiers](./global-identifiers).

## Add refetch only to entities that can be loaded by ID

Register global object identification once, then mark refetchable entities with `Node` support on the detailed page.

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification();
```

With the default options, Hot Chocolate registers the `Node` contract, adds `node(id:)`, adds `nodes(ids:)`, sets `AddNodesField` to `true`, and limits `nodes(ids:)` batches to 50 IDs through `MaxAllowedNodeBatchSize`.

A node type needs an ID field and a resolver that can load the entity:

```csharp
#nullable enable

using HotChocolate.Types.Relay;

[Node]
public sealed class Product
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public static Task<Product?> GetAsync(
        int id,
        IProductRepository products,
        CancellationToken cancellationToken)
        => products.GetByIdAsync(id, cancellationToken);
}
```

Put DataLoader and resolver details in [Global Identifiers](./global-identifiers). For batch request limits, see [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits).

## Return large lists as connections

Use paging on fields where clients browse, scroll, or request the next slice. Keep the underlying order deterministic so cursors stay stable.

```csharp
#nullable enable

using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(CatalogDbContext db)
        => db.Products.OrderBy(product => product.Id);
}
```

This produces a connection with `nodes`, `edges`, and `pageInfo`. Use `nodes` when the client only needs items. Use `edges` when the client needs per-item cursors or edge metadata. Continue with [Connections](./connections) or the deeper [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) guide.

## Keep composite identity opaque

Represent a composite key as one value in your server model, then serialize that value as one global ID.

```csharp
using HotChocolate.Types.Relay;

public readonly record struct ProductVariantId(int ProductId, string Sku);

public sealed class ProductVariant
{
    [ID<ProductVariant>]
    public ProductVariantId Id { get; init; }
}
```

Do not make clients assemble or parse this value. Add the serializer or generated ID value support in [Complex IDs](./complex-ids).

## Design mutation payloads for post-write reads

Return the changed object directly in the payload. Add the optional payload `query` field when clients benefit from selecting additional root query data in the same operation.

```csharp
public sealed record RenameProductPayload(
    Product? Product,
    IReadOnlyList<RenameProductError> Errors);
```

```csharp
builder
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads();
```

By default, Hot Chocolate adds `query` to payload types whose names end in `Payload`. The field gives clients a root query entry point inside the mutation response. It does not update a client cache by itself. Continue with [Mutation Payload Query Field](./mutation-payload-query-field) and [Mutations](../operations-mutations).

# Check the response shapes clients see

A global ID is opaque. Clients should store it and send it back as an `ID`, not decode it or depend on its format.

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "name": "Chai"
    }
  }
}
```

Use `node(id:)` when the client has an ID and needs to refetch the object:

```graphql
query GetNode($id: ID!) {
  node(id: $id) {
    id
    ... on Product {
      name
    }
  }
}
```

Use connection fields for paged lists:

```graphql
query BrowseProducts($after: String) {
  products(first: 20, after: $after) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Use a mutation payload to return the changed entity, typed errors, and optional follow-up reads:

```graphql
mutation RenameProduct($input: RenameProductInput!) {
  renameProduct(input: $input) {
    product {
      id
      name
    }
    errors {
      message
    }
    query {
      products(first: 5) {
        nodes {
          id
          name
        }
      }
    }
  }
}
```

The `query` selection is available only when you add the payload query field feature and the payload type matches its predicate.

# Troubleshoot common symptoms

| Symptom                                                                   | Likely cause                                                                                                                       | Where to fix it                                                                                                            |
| ------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Client cache reports inconsistent `__typename` values or mixes records.   | IDs are not globally unique across object types.                                                                                   | [Global Identifiers](./global-identifiers)                                                                                 |
| `[ID<Product>]` rejects an ID or serializes with an unexpected type name. | v16 uses the configured GraphQL type name, not always the CLR type name.                                                           | [Global Identifiers](./global-identifiers), [v15 to v16 migration](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) |
| `node(id:)` returns `null`.                                               | The type is not a node, the resolver cannot find the entity, or the ID type name does not match.                                   | [Global Identifiers](./global-identifiers)                                                                                 |
| Schema build fails after adding global object identification.             | No types implement `Node`, or `EnsureAllNodesCanBeResolved` finds a missing resolver.                                              | [Global Identifiers](./global-identifiers)                                                                                 |
| `nodes(ids:)` rejects a large batch.                                      | `MaxAllowedNodeBatchSize` defaults to 50 unless you configure it.                                                                  | [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Global Identifiers](./global-identifiers)      |
| Pagination skips or repeats records.                                      | The connection field does not use deterministic ordering, or cursor keys are unstable.                                             | [Connections](./connections)                                                                                               |
| A Relay client merges edges from different lists.                         | Client connection keys are reused, or schema connection names are ambiguous.                                                       | [Connections](./connections)                                                                                               |
| A mutation payload lacks `query`.                                         | The feature is not registered, the payload type does not match the predicate, the field name differs, or the field already exists. | [Mutation Payload Query Field](./mutation-payload-query-field)                                                             |

# Choose your next page

| If you need to...                                                                    | Go to                                                          |
| ------------------------------------------------------------------------------------ | -------------------------------------------------------------- |
| Serialize public IDs, accept IDs in arguments or inputs, and add `Node` refetch.     | [Global Identifiers](./global-identifiers)                     |
| Page through large lists with cursors, `nodes`, `edges`, and `pageInfo`.             | [Connections](./connections)                                   |
| Represent tenant-local IDs, SKU-based IDs, or other composite keys as one opaque ID. | [Complex IDs](./complex-ids)                                   |
| Add an optional root query field to mutation payloads.                               | [Mutation Payload Query Field](./mutation-payload-query-field) |

Adjacent schema guides help with the building blocks: [Arguments](../arguments), [Input Object Types](../input-object-types), [Interfaces](../interfaces), [Scalars](../scalars), [Lists and Non-Null](../lists-and-non-null), [Queries](../operations-queries), and [Mutations](../operations-mutations).
