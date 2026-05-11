---
title: Mutation Payload Query Field
---

A mutation payload query field allows a mutation response to include both the changed data and related root query data in a single operation. For example, after updating a product, the response can return the updated product and also refresh the current viewer summary or the first page of a product list.

The process works as follows:

1. The root mutation field performs the write operation.
2. Fields selected beneath the mutation payload are read selections that reflect the state after the write.
3. The payload's `query` field exposes the root `Query` type, enabling the client to request additional reads without requiring a second network request.

```graphql
mutation UpdateProduct($input: UpdateProductInput!) {
  updateProduct(input: $input) {
    product {
      id
      name
      price
    }
    query {
      viewer {
        cartSummary {
          totalItems
        }
      }
    }
  }
}
```

This feature is a Hot Chocolate schema helper designed for Relay-style workflows. It is compatible with Relay clients, Apollo-style normalized caches, Strawberry Shake, or custom clients.

# How the Field Works

`AddQueryFieldToMutationPayloads()` is an opt-in schema feature that adds a field named `query` (by default) to matching mutation payload object types.

```graphql
type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
  query: Query!
}
```

The injected field is non-null and points to the schema's query root. If your root query type uses a different GraphQL name, the field will use that name (for example, `query: QueryType!`).

The payload query field:

- Allows a client to select root query fields from within a mutation payload.
- Applies to object return types used by mutation fields and accepted by the payload predicate.
- Uses the same resolvers, middleware, paging, filtering, sorting, projections, and DataLoaders as the same fields selected from the root `Query`.
- Does not cause nested selections to perform writes.
- Does not update a client cache on its own.
- Does not replace direct payload fields for the primary changed data.
- Does not replace typed domain errors.
- Does not bypass authorization.
- Does not require `.AddGlobalObjectIdentification()` unless the operation uses Node features such as `node(id:)`.
- Does not overwrite an existing payload field with the configured field name.

# Check the Payload Shape Before Enabling

Begin with a payload that communicates the mutation result. Mutation conventions typically generate this shape:

```graphql
type Mutation {
  updateProduct(input: UpdateProductInput!): UpdateProductPayload!
}

type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
}
```

You can also return a custom payload object. The helper adds `query` when all of the following are true:

| Requirement                                                    | Why it matters                                                        |
| -------------------------------------------------------------- | --------------------------------------------------------------------- |
| The schema has a query root type.                              | The injected field needs a root query type to expose.                 |
| The schema has a mutation root type.                           | The helper scans mutation fields to find payload return types.        |
| A mutation field returns an object payload type.               | Scalars, enums, and list return shapes are not payload objects.       |
| The payload type matches the predicate.                        | The default predicate matches GraphQL type names ending in `Payload`. |
| The payload does not already define the configured field name. | Hot Chocolate leaves existing payload fields unchanged.               |

# Enabling Query Fields on Mutation Payloads

Register the helper in the `.AddGraphQL()` chain:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddMutationConventions(applyToAllMutations: true)
    .AddQueryFieldToMutationPayloads();
```

Before enabling the helper, a conventional payload might look like this:

```graphql
type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
}
```

After the helper, matching payloads gain a root query entry point:

```graphql
type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
  query: Query!
}
```

Exact payload fields depend on your resolver return type, mutation conventions, and custom payload configuration. The `query` field is added only when Hot Chocolate can see both operation roots and a matching object payload type.

# Request changed data and follow-up data

The next example uses implementation-first operation types, mutation conventions, typed domain errors, and the payload query field.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProductService>();

builder
    .AddGraphQL()
    .AddMutationConventions(applyToAllMutations: true)
    .AddQueryFieldToMutationPayloads();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

```csharp
// Types/ProductOperations.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    public static Viewer GetViewer()
        => new(new CartSummary(3));

    [UsePaging]
    public static IQueryable<Product> GetProducts(ProductService products)
        => products.GetProducts().OrderBy(product => product.Id);
}

[MutationType]
public static partial class ProductMutations
{
    [Error<ProductNotFoundException>]
    public static async Task<Product?> UpdateProductAsync(
        int productId,
        string name,
        decimal price,
        ProductService products,
        CancellationToken cancellationToken)
    {
        return await products.UpdateAsync(
            productId,
            name,
            price,
            cancellationToken);
    }
}

public sealed record Product(int Id, string Name, decimal Price);

public sealed record Viewer(CartSummary CartSummary);

public sealed record CartSummary(int TotalItems);

public sealed class ProductService
{
    private readonly List<Product> _products = new()
    {
        new Product(1, "Trail Backpack", 120.00m),
        new Product(2, "City Bike", 499.00m)
    };

    public IQueryable<Product> GetProducts()
        => _products.AsQueryable();

    public Task<Product> UpdateAsync(
        int productId,
        string name,
        decimal price,
        CancellationToken cancellationToken)
    {
        var index = _products.FindIndex(product => product.Id == productId);

        if (index < 0)
        {
            throw new ProductNotFoundException(productId);
        }

        var product = _products[index] with
        {
            Name = name,
            Price = price
        };

        _products[index] = product;

        return Task.FromResult(product);
    }
}

public sealed class ProductNotFoundException : Exception
{
    public ProductNotFoundException(int productId)
        : base($"Product {productId} was not found.")
    {
    }
}
```

SDL excerpt:

```graphql
type Mutation {
  updateProduct(input: UpdateProductInput!): UpdateProductPayload!
}

input UpdateProductInput {
  productId: Int!
  name: String!
  price: Decimal!
}

type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
  query: Query!
}

union UpdateProductError = ProductNotFoundError

type ProductNotFoundError implements Error {
  message: String!
}

interface Error {
  message: String!
}
```

Client operation:

```graphql
mutation UpdateProduct($input: UpdateProductInput!) {
  updateProduct(input: $input) {
    product {
      id
      name
      price
    }
    errors {
      ... on ProductNotFoundError {
        message
      }
    }
    query {
      viewer {
        cartSummary {
          totalItems
        }
      }
      products(first: 3) {
        nodes {
          id
          name
          price
        }
      }
    }
  }
}
```

Variables:

```json
{
  "input": {
    "productId": 1,
    "name": "Trail Backpack Pro",
    "price": 149.99
  }
}
```

Successful response shape:

```json
{
  "data": {
    "updateProduct": {
      "product": {
        "id": 1,
        "name": "Trail Backpack Pro",
        "price": 149.99
      },
      "errors": [],
      "query": {
        "viewer": {
          "cartSummary": {
            "totalItems": 3
          }
        },
        "products": {
          "nodes": [
            {
              "id": 1,
              "name": "Trail Backpack Pro",
              "price": 149.99
            },
            {
              "id": 2,
              "name": "City Bike",
              "price": 499.0
            }
          ]
        }
      }
    }
  }
}
```

`product` is the primary mutation result. `errors` communicates expected business failures. `query.viewer` and `query.products` are follow-up reads that already belong to the root query contract.

# Choose direct payload fields, `query`, or Node

| Need                                                   | Prefer                 | Rationale                                                                                 |
| ------------------------------------------------------ | ---------------------- | ----------------------------------------------------------------------------------------- |
| Return the created or updated object.                  | Direct payload field   | The changed object is the mutation result and should be explicit.                         |
| Return a deleted object ID or affected aggregate.      | Direct payload field   | The payload should tell the client what changed.                                          |
| Return expected business failures.                     | Payload `errors` field | Domain errors belong to the mutation contract.                                            |
| Refresh viewer, session, cart, list, or summary state. | `query`                | The data already belongs to root `Query`, and the client can select the same field shape. |
| Refetch one object by opaque global ID.                | `query` plus Node      | The `node(id:)` field is a root query field and requires global object identification.    |
| Fill in unclear mutation semantics.                    | Redesign the payload   | A `query` field should not hide a weak mutation contract.                                 |

Start with direct payload fields for the changed data. Add `query` when clients benefit from selecting related root query data in the same mutation response. Keep payloads understandable for clients that do not use Relay.

# Use Node refetch when you configure Node

`AddQueryFieldToMutationPayloads()` and `AddGlobalObjectIdentification()` are independent features:

- `AddQueryFieldToMutationPayloads()` adds root query access to matching mutation payloads.
- `AddGlobalObjectIdentification()` adds the Node contract and the `node(id:)` and `nodes(ids:)` root fields.

Use them together when a mutation response needs a Node refetch. The operation can select `node(id:)` under `query`, but the schema must also configure global object identification and Node resolvers.

```graphql
mutation UpdateProductAndRefetchNode($input: UpdateProductInput!, $id: ID!) {
  updateProduct(input: $input) {
    query {
      node(id: $id) {
        id
        ... on Product {
          name
          price
        }
      }
    }
  }
}
```

For global ID serialization and Node setup, see [Global Identifiers](./global-identifiers).

# Customize the field name

Change the field name when your schema already uses `query` for another payload field or when your schema naming convention requires another name.

```csharp
builder
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
    });
```

SDL excerpt:

```graphql
type UpdateProductPayload {
  product: Product
  rootQuery: Query!
}
```

If a payload already defines a field with the configured name, Hot Chocolate leaves that field unchanged and does not add another root query field.

# Customize which payloads receive the field

By default, Hot Chocolate uses this predicate:

```csharp
type => type.Name.EndsWith("Payload", StringComparison.Ordinal)
```

The check is name-based. It does not inspect whether a type represents a domain payload. If your schema uses a different suffix, configure `MutationPayloadPredicate`.

```csharp
builder
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.MutationPayloadPredicate =
            type => type.Name.EndsWith("Result", StringComparison.Ordinal);
    });
```

SDL excerpt:

```graphql
type UpdateProductResult {
  product: Product
  query: Query!
}
```

Keep predicates narrow. A broad predicate can add root query access to unrelated object types returned from mutation fields.

# Keep typed domain errors on the payload

The payload query field is not an error modeling feature. Use typed domain errors for expected business failures.

```graphql
type UpdateProductPayload {
  product: Product
  errors: [UpdateProductError!]
  query: Query!
}

union UpdateProductError = ProductNotFoundError | InvalidProductPriceError
```

Select the `errors` field when the client needs to handle expected failures:

```graphql
mutation UpdateProduct($input: UpdateProductInput!) {
  updateProduct(input: $input) {
    product {
      id
      name
    }
    errors {
      ... on ProductNotFoundError {
        message
      }
      ... on InvalidProductPriceError {
        message
      }
    }
    query {
      viewer {
        cartSummary {
          totalItems
        }
      }
    }
  }
}
```

Nested selections under `query` can still produce normal GraphQL execution errors. Treat those errors like errors from the same fields selected on root `Query`.

# Understand authorization and execution behavior

The helper exposes a path to the root query object. It does not grant additional access.

| Behavior                      | What to expect                                                                                                                                                                          |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Authorization                 | Fields under `query` use the same authorization rules as root query fields. Unauthorized fields can return GraphQL errors and `null` values according to normal Hot Chocolate behavior. |
| Resolver execution            | Query resolvers, DataLoaders, projections, filtering, sorting, paging, and field middleware still apply.                                                                                |
| Mutation ordering             | Root mutation fields execute serially. Child selections below one mutation payload are read selections over the returned payload and query root.                                        |
| Transactions and side effects | The helper does not create transactions or roll back application side effects. Coordinate those concerns in application code or transaction configuration.                              |

# Troubleshoot common symptoms

| Symptom                                                         | Check                                                                                                          | Fix                                                                                                        |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| The payload has no `query` field.                               | Confirm `.AddQueryFieldToMutationPayloads()` is registered in the `.AddGraphQL()` chain.                       | Add the helper after your schema setup.                                                                    |
| The field is still missing.                                     | Confirm the schema has both query and mutation roots.                                                          | Add or register the missing operation root.                                                                |
| A specific mutation payload has no field.                       | Confirm the mutation returns an object payload type used by a mutation field.                                  | Return a payload object, or use mutation conventions to generate one.                                      |
| A payload named `UpdateProductResult` has no field.             | The default predicate matches names ending in `Payload`.                                                       | Configure `MutationPayloadPredicate` for your suffix.                                                      |
| A payload already has a `query` field.                          | Hot Chocolate does not overwrite existing fields.                                                              | Rename your existing field or set `QueryFieldName` to another value.                                       |
| The client selected `query`, but the UI did not update.         | Server responses contain only selected fields. Client cache updates depend on the client and selected records. | Select the IDs and fields your UI reads. For Relay-style clients, include the relevant fragments.          |
| A field under `query` returns `null` or an authorization error. | The same field would behave that way from root `Query`.                                                        | Check field policies, type policies, resolver behavior, nullability, and the current user.                 |
| `node(id:)` under `query` fails.                                | Payload query fields do not add Node support.                                                                  | Configure global object identification and Node resolvers. See [Global Identifiers](./global-identifiers). |
| The generated field is named `rootQuery`, not `query`.          | The schema customized `QueryFieldName`.                                                                        | Use the configured name in client operations.                                                              |

# API reference

| API or option                                     | Kind                                | Notes                                                                                                           |
| ------------------------------------------------- | ----------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `.AddQueryFieldToMutationPayloads()`              | `IRequestExecutorBuilder` extension | Common setup path in the `.AddGraphQL()` chain.                                                                 |
| `.AddQueryFieldToMutationPayloads(...)`           | `ISchemaBuilder` extension          | Lower-level schema builder path.                                                                                |
| `MutationPayloadOptions`                          | Options type                        | Namespace: `HotChocolate.Types.Relay`.                                                                          |
| `MutationPayloadOptions.QueryFieldName`           | `string?`                           | Defaults to `query` when unset.                                                                                 |
| `MutationPayloadOptions.MutationPayloadPredicate` | `Func<ITypeDefinition, bool>`       | Defaults to `type => type.Name.EndsWith("Payload", StringComparison.Ordinal)`.                                  |
| Injected field type                               | Schema behavior                     | Non-null query root type, for example `query: Query!`.                                                          |
| Matching rule                                     | Schema behavior                     | Applies only to object return types used by mutation fields and accepted by the predicate.                      |
| Existing field conflict                           | Schema behavior                     | Existing payload fields with the configured field name are left unchanged.                                      |
| Feature dependency                                | Schema behavior                     | Requires query and mutation root types before it can add fields. Does not require global object identification. |

# Next steps

- Use [Mutations](../operations-mutations) to design mutation fields, inputs, payloads, and typed errors.
- Use [Global Identifiers](./global-identifiers) when clients need stable global IDs or `node(id:)`.
- Use [Connections](./connections) when follow-up reads need cursor pagination.
- Use [Authorization](/docs/hotchocolate/v16/build/security/authorization) to protect mutation and query fields.
- Use [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) to model typed domain errors.
