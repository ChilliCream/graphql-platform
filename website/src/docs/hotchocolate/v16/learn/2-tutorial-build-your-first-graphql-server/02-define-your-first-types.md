---
title: "Define your first types"
description: "Replace the starter schema with a small catalog domain, expose a product through the Query root, and inspect the generated schema in Nitro."
---

In the previous chapter, you created the `CatalogServer` project, ran it, and opened Nitro at `/graphql`.

Now you will replace the starter `Book` and `Author` example with the first version of a product catalog.

In this chapter, you will:

- Add `Product` and `Brand` types
- Expose one product through the root `Query` type
- See how Hot Chocolate turns C# types into a GraphQL schema
- Run a query against the new schema in Nitro

# Define the catalog types

Hot Chocolate can build a GraphQL schema from your C# implementation. This is called implementation-first schema building.

You write normal C# types and resolver methods. Hot Chocolate then infers the GraphQL types and fields from that implementation.

In the `Types` folder, rename `Author.cs` to `Brand.cs` and replace its contents with:

```csharp
namespace CatalogServer.Types;

public sealed record Brand
{
    public int Id { get; init; }

    public string Name { get; init; } = default!;
}
```

Next, rename `Book.cs` to `Product.cs` and replace its contents with:

```csharp
namespace CatalogServer.Types;

public sealed record Product
{
    public int Id { get; init; }

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public Brand Brand { get; init; } = default!;
}
```

These records are plain C# types. They become part of the GraphQL schema once a query field returns them.

# Update the root query

Open `Types/Query.cs` and replace its contents with:

```csharp
namespace CatalogServer.Types;

[QueryType]
public static partial class Query
{
    public static Product GetFeaturedProduct()
        => new()
        {
            Id = 1,
            Name = "Touring Bike",
            Description = "A bike designed for comfort on long rides.",
            Brand = new Brand
            {
                Id = 1,
                Name = "Adventure Works"
            }
        };
}
```

The `[QueryType]` attribute tells Hot Chocolate that this class contributes fields to the root GraphQL `Query` type.

The method `GetFeaturedProduct` becomes a GraphQL field named `featuredProduct`. Hot Chocolate removes the `Get` prefix and uses lower camel case for the field name.

# Build and run the server

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If the server is not running, start it:

```bash
dotnet run
```

If the server was already running, stop it with <kbd>Ctrl</kbd> + <kbd>C</kbd> and start it again so Nitro loads the updated schema.

# Inspect the generated schema

Open Nitro at your local GraphQL endpoint.

For example:

```text
http://localhost:5095/graphql
```

Use the port shown in your terminal.

The generated schema should include these types:

```graphql
type Query {
  featuredProduct: Product!
}

type Product {
  id: Int!
  name: String!
  description: String
  brand: Brand!
}

type Brand {
  id: Int!
  name: String!
}
```

Notice how the C# names map to GraphQL names:

| C# code | GraphQL schema name |
| --- | --- |
| `Product` | `Product` |
| `Brand` | `Brand` |
| `GetFeaturedProduct()` | `featuredProduct` |
| `Product.Id` | `id` |
| `Product.Name` | `name` |
| `Product.Description` | `description` |
| `Product.Brand` | `brand` |
| `Brand.Id` | `id` |
| `Brand.Name` | `name` |

GraphQL field names are case-sensitive. Clients use the names from the GraphQL schema, not the C# property names.

# Run a query

Paste this query into Nitro:

```graphql
{
  featuredProduct {
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

Run the query. The response should look like this:

```json
{
  "data": {
    "featuredProduct": {
      "id": 1,
      "name": "Touring Bike",
      "description": "A bike designed for comfort on long rides.",
      "brand": {
        "id": 1,
        "name": "Adventure Works"
      }
    }
  }
}
```

The query only asks for selected fields. Try removing `description` or `brand { name }` and run the query again. The JSON response changes to match the selection.

# Checkpoint

Continue when all of these are true:

- `dotnet build` succeeds
- `dotnet run` starts the server
- Nitro connects to your local `/graphql` endpoint
- The schema includes `featuredProduct`, `Product`, and `Brand`
- The query returns the expected product response

You have now replaced the starter schema with the first catalog types. Next, continue to [Write query resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-query-resolvers/).
