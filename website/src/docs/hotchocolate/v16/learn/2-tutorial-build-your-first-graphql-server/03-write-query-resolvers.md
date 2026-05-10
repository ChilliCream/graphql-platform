---
title: "Write query resolvers"
description: "Add product query resolvers, query selected fields in Nitro, pass arguments with variables, and learn how Hot Chocolate supplies resolver parameters."
---

In the previous chapter, you defined the first catalog types and exposed a `featuredProduct` field from the root `Query` type.

Now you will replace that single featured product with query resolvers for a small product catalog. The data will stay in memory for this chapter. In the next chapter, you will move it behind a small service.

In this chapter, you will:

- Return multiple products from the `products` field
- Add a `productById(id:)` field
- See how selection sets shape the JSON response
- Pass an argument with GraphQL variables
- Learn which resolver parameters come from the GraphQL operation and which are supplied by Hot Chocolate

# Start from the catalog schema

Open the `CatalogServer` project you created earlier.

Your project should contain these files:

```text
CatalogServer/
├── Program.cs
└── Types/
    ├── Brand.cs
    ├── Product.cs
    └── Query.cs
```

The `Product` type should have `id`, `name`, `description`, and `brand` fields in the schema. The `Brand` type should have `id` and `name`.

If the server is not running, start it:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint. For example:

```text
http://localhost:5095/graphql
```

Use the port shown in your terminal.

# Replace the products resolver

GraphQL queries begin at the root `Query` type. Each field on `Query` is an entry point for reading data.

Replace `Types/Query.cs` with:

```csharp
namespace CatalogServer.Types;

[QueryType]
public static partial class Query
{
    private static readonly Brand s_contoso = new()
    {
        Id = 1,
        Name = "Contoso"
    };

    private static readonly Brand s_adventureWorks = new()
    {
        Id = 2,
        Name = "Adventure Works"
    };

    private static readonly IReadOnlyList<Product> s_products =
    [
        new Product
        {
            Id = 1,
            Name = "Trailblazer Backpack",
            Description = "A durable backpack for everyday adventures.",
            Brand = s_contoso
        },
        new Product
        {
            Id = 2,
            Name = "Summit Water Bottle",
            Description = "An insulated bottle that keeps drinks cold.",
            Brand = s_contoso
        },
        new Product
        {
            Id = 3,
            Name = "City Bike Helmet",
            Description = "A lightweight helmet for urban rides.",
            Brand = s_adventureWorks
        }
    ];

    [GraphQLDescription("Gets the products currently available in the catalog.")]
    public static IReadOnlyList<Product> GetProducts()
        => s_products;
}
```

This method is a resolver. Hot Chocolate uses the `[QueryType]` attribute to add the method to the GraphQL `Query` type and calls it when an operation selects the field.

The generated GraphQL field is named `products`. Hot Chocolate removes the `Get` prefix and maps the rest of the method name to lower camel case.

| C# resolver method | GraphQL field |
| --- | --- |
| `GetProducts()` | `products` |

Build the project:

```bash
dotnet build
```

Expected build signal:

```text
Build succeeded.
```

Restart the server if it was already running. Refresh Nitro's schema information, then check that `Query` has a `products` field.

# Query selected product fields

Run this operation in Nitro:

```graphql
query GetProducts {
  products {
    id
    name
    description
    brand {
      id
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "products": [
      {
        "id": 1,
        "name": "Trailblazer Backpack",
        "description": "A durable backpack for everyday adventures.",
        "brand": {
          "id": 1,
          "name": "Contoso"
        }
      },
      {
        "id": 2,
        "name": "Summit Water Bottle",
        "description": "An insulated bottle that keeps drinks cold.",
        "brand": {
          "id": 1,
          "name": "Contoso"
        }
      },
      {
        "id": 3,
        "name": "City Bike Helmet",
        "description": "A lightweight helmet for urban rides.",
        "brand": {
          "id": 2,
          "name": "Adventure Works"
        }
      }
    ]
  }
}
```

The response follows the operation:

- The operation selects `products`, so the response has `data.products`.
- The operation selects `id`, `name`, `description`, and `brand`, so each product has those fields.
- The operation selects `id` and `name` under `brand`, so each brand object has those fields.

Change the operation so it asks for fewer fields:

```graphql
query GetProductNames {
  products {
    name
    brand {
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "products": [
      {
        "name": "Trailblazer Backpack",
        "brand": {
          "name": "Contoso"
        }
      },
      {
        "name": "Summit Water Bottle",
        "brand": {
          "name": "Contoso"
        }
      },
      {
        "name": "City Bike Helmet",
        "brand": {
          "name": "Adventure Works"
        }
      }
    ]
  }
}
```

You did not change the resolver method. You changed the selection set. GraphQL returns the fields the operation selected.

# Add a resolver with an argument

GraphQL operations often need to fetch one item by its identifier. Add a second resolver to `Types/Query.cs`:

```csharp
namespace CatalogServer.Types;

[QueryType]
public static partial class Query
{
    private static readonly Brand s_contoso = new()
    {
        Id = 1,
        Name = "Contoso"
    };

    private static readonly Brand s_adventureWorks = new()
    {
        Id = 2,
        Name = "Adventure Works"
    };

    private static readonly IReadOnlyList<Product> s_products =
    [
        new Product
        {
            Id = 1,
            Name = "Trailblazer Backpack",
            Description = "A durable backpack for everyday adventures.",
            Brand = s_contoso
        },
        new Product
        {
            Id = 2,
            Name = "Summit Water Bottle",
            Description = "An insulated bottle that keeps drinks cold.",
            Brand = s_contoso
        },
        new Product
        {
            Id = 3,
            Name = "City Bike Helmet",
            Description = "A lightweight helmet for urban rides.",
            Brand = s_adventureWorks
        }
    ];

    [GraphQLDescription("Gets the products currently available in the catalog.")]
    public static IReadOnlyList<Product> GetProducts()
        => s_products;

    [GraphQLDescription("Gets a product by its identifier.")]
    public static Product? GetProductById(int id)
        => s_products.FirstOrDefault(product => product.Id == id);
}
```

`GetProductById` becomes a GraphQL field named `productById`. The `id` parameter becomes an argument on that field.

| C# code | GraphQL schema name |
| --- | --- |
| `GetProductById(int id)` | `productById(id: Int!)` |

Build and restart the server:

```bash
dotnet build
dotnet run
```

Refresh Nitro's schema information. The root `Query` type should now include both fields:

```graphql
type Query {
  products: [Product!]!
  productById(id: Int!): Product
}
```

`productById` returns `Product` instead of `Product!` because the C# resolver returns `Product?`. If there is no product with the requested ID, the field can return `null`.

# Query with variables

In Nitro, run this operation:

```graphql
query GetProductById($id: Int!) {
  productById(id: $id) {
    id
    name
    description
    brand {
      name
    }
  }
}
```

Add these variables in Nitro's variables panel:

```json
{
  "id": 1
}
```

Expected response:

```json
{
  "data": {
    "productById": {
      "id": 1,
      "name": "Trailblazer Backpack",
      "description": "A durable backpack for everyday adventures.",
      "brand": {
        "name": "Contoso"
      }
    }
  }
}
```

The variable declaration `($id: Int!)` says the operation expects a non-null integer variable. The field call `productById(id: $id)` passes that variable value into the resolver's `id` parameter.

Try a value that does not exist:

```json
{
  "id": 99
}
```

Expected response:

```json
{
  "data": {
    "productById": null
  }
}
```

The resolver returned `null`, so the nullable GraphQL field returns `null`.

# Understand resolver parameters

Resolver parameters can come from different places.

In this chapter, `id` comes from the GraphQL operation:

```csharp
public static Product? GetProductById(int id)
```

Because `id` is part of the GraphQL field contract, the operation provides it as `productById(id: 1)` or through a variable.

Hot Chocolate can also provide runtime parameters that are not part of the GraphQL field contract. A common example is `CancellationToken`:

```csharp
public static Product? GetProductById(
    int id,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    return s_products.FirstOrDefault(product => product.Id == id);
}
```

Do not add this version unless you want to practice the shape. The GraphQL field is still `productById(id:)`. `CancellationToken` is supplied by Hot Chocolate at runtime, not by the operation.

Hot Chocolate can also supply service parameters. If a type is registered with ASP.NET Core dependency injection, a resolver can receive that service as a method parameter:

```csharp
public static IReadOnlyList<Product> GetProducts(CatalogService catalogService)
    => catalogService.GetProducts();
```

In that shape, `catalogService` is not a GraphQL argument. It is a service parameter. You will add the `CatalogService` and register it in the next chapter.

# Checkpoint: run both query fields

Run this operation in Nitro:

```graphql
query QueryResolverCheckpoint($id: Int!) {
  products {
    id
    name
  }
  productById(id: $id) {
    id
    name
    brand {
      id
      name
    }
  }
}
```

Use these variables:

```json
{
  "id": 2
}
```

Expected response:

```json
{
  "data": {
    "products": [
      {
        "id": 1,
        "name": "Trailblazer Backpack"
      },
      {
        "id": 2,
        "name": "Summit Water Bottle"
      },
      {
        "id": 3,
        "name": "City Bike Helmet"
      }
    ],
    "productById": {
      "id": 2,
      "name": "Summit Water Bottle",
      "brand": {
        "id": 1,
        "name": "Contoso"
      }
    }
  }
}
```

You are ready for the next chapter when all of these are true:

- `dotnet build` reports `Build succeeded`.
- Nitro shows `Query.products`.
- Nitro shows `Query.productById(id:)`.
- The checkpoint operation returns a top-level `data` object with no `errors`.
- `products` returns three products.
- `productById` returns one product when the ID exists and `null` when it does not.

You should also be able to answer these questions:

| Question | Answer |
| --- | --- |
| Which resolver starts the list query? | `Query.GetProducts` resolves `Query.products`. |
| Which resolver uses an argument? | `Query.GetProductById(int id)` resolves `Query.productById(id:)`. |
| Why does a response include only some product fields? | The operation selection set controls which fields appear in the response. |
| Does the GraphQL operation provide `CancellationToken`? | No. Hot Chocolate supplies runtime parameters like `CancellationToken`. |

If a field does not appear, rebuild the project, restart the server, and refresh Nitro's schema information. If the response omits a field, confirm that the field is selected in the operation.

Continue to [Use a data service](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-use-a-data-service/).
