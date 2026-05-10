---
title: "Add mutations"
description: "Add one create-product mutation to the tutorial catalog server, use input and payload shapes, and verify the write with a paged query."
---

In the previous chapter, you changed the `products` field to return a paged connection. Now you will add the first write operation to the same catalog: a mutation that creates one product.

By the end of this chapter, you will:

- Enable Hot Chocolate mutation conventions
- Add a `CreateProductInput`
- Add a root `Mutation` type with `CreateProductAsync`
- Update `CatalogService` so it stores and returns the new product
- Run the mutation in Nitro with variables
- Verify the new product with the paged `products` query

# Enable mutation conventions

GraphQL uses the root `Query` type for reads and the root `Mutation` type for writes. A mutation field changes server state, then returns data the operation can read from the result.

Hot Chocolate can apply the common GraphQL mutation shape for you:

```graphql
type Mutation {
  createProduct(input: CreateProductInput!): CreateProductPayload!
}
```

The `input` object contains values supplied by the operation. The payload object contains the result of the mutation. For this first mutation, the payload will expose the created `product`.

Open `Program.cs` and add mutation conventions to the GraphQL builder:

```csharp
using CatalogServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogService>();

builder
    .AddGraphQL()
    .AddTypes()
    .AddPagingArguments()
    .AddMutationConventions();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

The important addition is:

```csharp
.AddMutationConventions();
```

# Add the input type

Create `Types/CreateProductInput.cs`:

```csharp
namespace CatalogServer.Types;

public sealed record CreateProductInput(
    string Name,
    string? Description,
    int BrandId);
```

This input type is the GraphQL contract for creating a product. The operation sends the product name, description, and the identifier of an existing brand.

# Update the catalog service

Open `Services/CatalogService.cs`.

Update the service so products are kept in a list and a new product can be added. Keep your paging helper methods from the previous chapter.

```csharp
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using CatalogServer.Types;
using GreenDonut.Data;

namespace CatalogServer.Services;

public sealed class CatalogService
{
    private readonly Brand[] _brands =
    [
        new()
        {
            Id = 1,
            Name = "Contoso"
        },
        new()
        {
            Id = 2,
            Name = "Adventure Works"
        }
    ];

    private readonly List<Product> _products;

    public CatalogService()
    {
        _products =
        [
            new Product
            {
                Id = 1,
                Name = "Trailblazer Backpack",
                Description = "A durable backpack for everyday adventures.",
                Brand = _brands[0]
            },
            new Product
            {
                Id = 2,
                Name = "Summit Water Bottle",
                Description = "An insulated bottle that keeps drinks cold.",
                Brand = _brands[0]
            },
            new Product
            {
                Id = 3,
                Name = "City Bike Helmet",
                Description = "A lightweight helmet for urban rides.",
                Brand = _brands[1]
            }
        ];
    }

    public Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var products = _products
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .ToArray();

        var first = pagingArguments.First ?? 10;
        var after = DecodeCursor(pagingArguments.After);
        var start = after is null
            ? 0
            : Array.FindIndex(products, product => product.Id == after.Value) + 1;

        var pageItems = products
            .Skip(start)
            .Take(first + 1)
            .ToArray();

        var hasNextPage = pageItems.Length > first;
        var items = pageItems
            .Take(first)
            .ToImmutableArray();

        return Task.FromResult(Page<Product>.Create(
            items,
            hasNextPage,
            start > 0,
            product => EncodeCursor(product.Id),
            products.Length));
    }

    public Product? GetProductById(int id)
        => _products.FirstOrDefault(product => product.Id == id);

    public Task<Product> CreateProductAsync(
        CreateProductInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var brand = _brands.First(brand => brand.Id == input.BrandId);
        var nextId = _products.Count == 0
            ? 1
            : _products.Max(product => product.Id) + 1;

        var product = new Product
        {
            Id = nextId,
            Name = input.Name,
            Description = input.Description,
            Brand = brand
        };

        _products.Add(product);

        return Task.FromResult(product);
    }

    private static string EncodeCursor(int id)
        => Convert.ToBase64String(
            Encoding.UTF8.GetBytes(id.ToString(CultureInfo.InvariantCulture)));

    private static int? DecodeCursor(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        return int.Parse(value, CultureInfo.InvariantCulture);
    }
}
```

`CatalogService` is registered as a singleton in this tutorial. That means the in-memory list is shared while the local server is running, so you can query a product after creating it.

# Add the mutation type

Create `Types/Mutation.cs`:

```csharp
using CatalogServer.Services;

namespace CatalogServer.Types;

[MutationType]
public static partial class Mutation
{
    [GraphQLDescription("Creates a product in the catalog.")]
    public static Task<Product> CreateProductAsync(
        CreateProductInput input,
        CatalogService catalogService,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return catalogService.CreateProductAsync(input, cancellationToken);
    }
}
```

The `[MutationType]` attribute adds this class to the GraphQL root `Mutation` type. The method name `CreateProductAsync` becomes the GraphQL field `createProduct`.

Only `input` is supplied by the operation. `CatalogService` comes from dependency injection, and `CancellationToken` comes from the request runtime.

# Build and inspect the schema

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Restart the server if it is running:

```bash
dotnet run
```

Open Nitro and refresh the schema. The schema should include a mutation root similar to this:

```graphql
type Mutation {
  createProduct(input: CreateProductInput!): CreateProductPayload!
}

input CreateProductInput {
  name: String!
  description: String
  brandId: Int!
}

type CreateProductPayload {
  product: Product
}
```

The exact schema can include descriptions and other built-in fields. Check that `createProduct` accepts one `input` argument and returns a payload with `product`.

# Run the create mutation

Run this mutation in Nitro:

```graphql
mutation CreateProduct($input: CreateProductInput!) {
  createProduct(input: $input) {
    product {
      id
      name
      description
      brand {
        id
        name
      }
    }
  }
}
```

Use these variables:

```json
{
  "input": {
    "name": "Camp Mug",
    "description": "A lightweight mug for camping trips.",
    "brandId": 1
  }
}
```

You should receive a response like this:

```json
{
  "data": {
    "createProduct": {
      "product": {
        "id": 4,
        "name": "Camp Mug",
        "description": "A lightweight mug for camping trips.",
        "brand": {
          "id": 1,
          "name": "Contoso"
        }
      }
    }
  }
}
```

The response is nested because the mutation returns a payload. The created product is available at `data.createProduct.product`.

# Verify with the paged products query

Run the paged query from the previous chapter:

```graphql
query GetProducts {
  products(first: 10) {
    nodes {
      id
      name
      description
      brand {
        name
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

The response should include the product you created:

```json
{
  "data": {
    "products": {
      "nodes": [
        {
          "id": 4,
          "name": "Camp Mug",
          "description": "A lightweight mug for camping trips.",
          "brand": {
            "name": "Contoso"
          }
        },
        {
          "id": 3,
          "name": "City Bike Helmet",
          "description": "A lightweight helmet for urban rides.",
          "brand": {
            "name": "Adventure Works"
          }
        },
        {
          "id": 2,
          "name": "Summit Water Bottle",
          "description": "An insulated bottle that keeps drinks cold.",
          "brand": {
            "name": "Contoso"
          }
        },
        {
          "id": 1,
          "name": "Trailblazer Backpack",
          "description": "A durable backpack for everyday adventures.",
          "brand": {
            "name": "Contoso"
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": false,
        "endCursor": "MQ=="
      }
    }
  }
}
```

The important checkpoint is that `Camp Mug` appears in the `products` connection after the mutation runs.

# Checkpoint: mutations work

You are ready to finish the tutorial when all of these are true:

- `Program.cs` calls `.AddMutationConventions()`.
- `Types/CreateProductInput.cs` exists.
- `Types/Mutation.cs` contains a `[MutationType]` class with `CreateProductAsync`.
- `CatalogService` adds the new product to its in-memory list and returns it.
- `dotnet build` reports `Build succeeded`.
- Nitro shows `createProduct(input: CreateProductInput!): CreateProductPayload!`.
- Running the mutation returns the created product in the payload.
- A follow-up `products(first: 10)` query includes the created product.

Continue to [You did it](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/you-did-it/).
