---
title: "Use a data service"
description: "Move the tutorial product catalog data into a small service, register it with dependency injection, and keep the products and productById query fields working."
---

In the previous chapter, your query resolvers returned product catalog data directly.

That works for a first resolver, but the resolver method is starting to do two jobs:

- Define the GraphQL field
- Hold the data used by that field

In this chapter, you will move the in-memory product and brand data into a small service. The GraphQL fields stay the same, and the resolver methods delegate to the service.

By the end of this chapter, you will:

- Create `Services/CatalogService.cs`
- Move the product and brand data into that service
- Register the service with dependency injection
- Update `products` and `productById` to receive the service as a resolver parameter
- Verify both fields in Nitro

This is only a small seam for data access in the tutorial project. It is not an architecture pattern you need to copy into every app.

# Create the catalog service

Create a `Services` folder in the `CatalogServer` project.

Add `Services/CatalogService.cs`:

```csharp
using CatalogServer.Types;

namespace CatalogServer.Services;

public sealed class CatalogService
{
    private static readonly Brand[] s_brands =
    [
        new Brand
        {
            Id = 1,
            Name = "Contoso"
        },
        new Brand
        {
            Id = 2,
            Name = "Adventure Works"
        }
    ];

    private static readonly Product[] s_products =
    [
        new Product
        {
            Id = 1,
            Name = "Trailblazer Backpack",
            Description = "A durable backpack for everyday adventures.",
            Brand = s_brands[0]
        },
        new Product
        {
            Id = 2,
            Name = "Summit Water Bottle",
            Description = "An insulated bottle that keeps drinks cold.",
            Brand = s_brands[0]
        },
        new Product
        {
            Id = 3,
            Name = "City Bike Helmet",
            Description = "A lightweight helmet for urban rides.",
            Brand = s_brands[1]
        }
    ];

    public IReadOnlyList<Product> GetProducts()
        => s_products;

    public Product? GetProductById(int id)
        => s_products.FirstOrDefault(product => product.Id == id);
}
```

The service still uses in-memory data. The difference is that the resolver no longer creates the data itself.

# Register the service

Open `Program.cs` and add the service registration before `builder.Build()`:

```csharp
using CatalogServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogService>();

builder
    .AddGraphQL()
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

`AddSingleton` is fine for this tutorial because the catalog data is in memory and shared for the local server run. The next chapters keep using this service while you add paging and mutations.

# Update the query resolvers

Open `Types/Query.cs`.

Replace the inline catalog data with resolver methods that receive `CatalogService`:

```csharp
using CatalogServer.Services;

namespace CatalogServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the products currently available in the catalog.")]
    public static IReadOnlyList<Product> GetProducts(CatalogService catalogService)
        => catalogService.GetProducts();

    [GraphQLDescription("Gets one product by its identifier.")]
    public static Product? GetProductById(int id, CatalogService catalogService)
        => catalogService.GetProductById(id);
}
```

Hot Chocolate gets `CatalogService` from ASP.NET Core dependency injection and passes it to the resolver method. The GraphQL field names stay the same:

| C# resolver | GraphQL field |
| --- | --- |
| `GetProducts` | `products` |
| `GetProductById` | `productById` |

The service parameter is not a GraphQL argument. The `id` parameter is a GraphQL argument because it is a simple input value supplied by the operation.

# Build and run

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Start the server:

```bash
dotnet run
```

Open Nitro at your local `/graphql` endpoint. For example:

```text
http://localhost:5095/graphql
```

Use the port shown by your terminal.

# Verify the product list

Run the list query:

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

The response shape did not change because the GraphQL schema did not change. Only the resolver implementation changed.

# Verify the by-id lookup

Run a query with a variable:

```graphql
query GetProductById($id: Int!) {
  productById(id: $id) {
    id
    name
    brand {
      name
    }
  }
}
```

Use this variables JSON in Nitro:

```json
{
  "id": 2
}
```

Expected response:

```json
{
  "data": {
    "productById": {
      "id": 2,
      "name": "Summit Water Bottle",
      "brand": {
        "name": "Contoso"
      }
    }
  }
}
```

If you request an id that is not in the catalog, `productById` returns `null`:

```json
{
  "id": 999
}
```

That is why the resolver returns `Product?`.

# Checkpoint: data service is wired in

You are ready to continue when all of these are true:

- `dotnet build` reports `Build succeeded`.
- `Program.cs` registers `CatalogService`.
- `Types/Query.cs` receives `CatalogService` as a resolver parameter.
- Nitro shows `products` on the `Query` type.
- Nitro shows `productById(id:)` on the `Query` type.
- The `products` query returns the three products from `CatalogService`.
- The `productById` query returns one product for `{ "id": 2 }`.

If service injection fails, confirm that `builder.Services.AddSingleton<CatalogService>()` appears before `builder.Build()` and that `Types/Query.cs` imports `CatalogServer.Services`.

Continue to [Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-add-pagination/). You now have a single place to read the product list, which makes the next change smaller.
