---
title: "Add pagination"
description: "Return products as a cursor-paginated connection, query the first page, and continue with an after cursor."
---

The `products` field currently returns the whole catalog at once. That is fine for a small demo list, but a collection can grow over time. Clients should be able to ask for a small page of products and then request the next page when they need it.

In this chapter, you will:

- Add paging argument support to the schema
- Return a `Page<Product>` from `CatalogService`
- Convert that page to a GraphQL connection in the resolver
- Query the first page and then continue with `pageInfo.endCursor`

Hot Chocolate uses the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm) for cursor pagination. You do not need to use Relay on the client. The important idea is that the server returns a page of edges, and each edge has an opaque cursor that can be used to continue later.

# Add paging arguments

Open `Program.cs` and make sure the GraphQL builder includes `AddPagingArguments()`:

```csharp
builder.Services.AddSingleton<CatalogService>();

builder
    .AddGraphQL()
    .AddTypes()
    .AddPagingArguments();
```

`AddPagingArguments()` lets Hot Chocolate bind GraphQL paging arguments to a `PagingArguments` parameter in your resolver. This tutorial uses the forward paging arguments, `first` and `after`.

# Return a page from the service

Open `Services/CatalogService.cs`.

Add these `using` directives:

```csharp
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using GreenDonut.Data;
```

Then update the products method so the service returns `Page<Product>`:

```csharp
public Task<Page<Product>> GetProductsAsync(
    PagingArguments pagingArguments,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    var products = s_products
        .OrderBy(t => t.Name)
        .ThenBy(t => t.Id)
        .ToArray();

    var first = pagingArguments.First ?? 10;
    var after = DecodeCursor(pagingArguments.After);
    var start = after is null
        ? 0
        : Array.FindIndex(products, t => t.Id == after.Value) + 1;

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
```

The service sorts products by `Name` and then by `Id` before it applies the cursor. Cursor pagination needs a stable order so the next page starts after the previous page instead of repeating or skipping items.

The service also fetches one extra item with `Take(first + 1)`. That extra item is not returned to the client. It only tells the service whether another page exists.

# Return a connection from the resolver

Open `Types/Query.cs`.

Add these `using` directives:

```csharp
using CatalogServer.Services;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
```

Update the `products` resolver:

```csharp
[QueryType]
public static partial class Query
{
    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        PagingArguments pagingArguments,
        CatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var page = await catalogService.GetProductsAsync(
            pagingArguments,
            cancellationToken);

        return page.ToConnection();
    }

    public static Product? GetProductById(
        int id,
        CatalogService catalogService)
        => catalogService.GetProductById(id);
}
```

The service owns the paging logic and returns `Page<Product>`. The resolver owns the GraphQL shape and converts the page to `Connection<Product>` with `ToConnection()`.

Build and run the server:

```bash
dotnet build
dotnet run
```

# Query the first page

Open Nitro at your local `/graphql` endpoint.

Run this query:

```graphql
query GetFirstProductsPage {
  products(first: 2) {
    edges {
      cursor
      node {
        id
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

The response should include two product edges and paging information:

```json
{
  "data": {
    "products": {
      "edges": [
        {
          "cursor": "Mw==",
          "node": {
            "id": 3,
            "name": "City Bike Helmet"
          }
        },
        {
          "cursor": "Mg==",
          "node": {
            "id": 2,
            "name": "Summit Water Bottle"
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "Mg=="
      }
    }
  }
}
```

Your product names and cursor values may be different. Treat cursors as opaque values. Clients should store and resend them, not decode or create them.

# Query the next page

Copy the `pageInfo.endCursor` value from the first response.

Use it as the `after` variable:

```graphql
query GetNextProductsPage($after: String!) {
  products(first: 2, after: $after) {
    edges {
      cursor
      node {
        id
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

In Nitro, set the variables:

```json
{
  "after": "Mg=="
}
```

Run the query. The next response should contain later products from the same stable order.

Clients repeat this process until `pageInfo.hasNextPage` is `false`:

```text
Run products(first: 2)
Read pageInfo.endCursor
Run products(first: 2, after: endCursor)
Continue while pageInfo.hasNextPage is true
```

# Checkpoint

You are ready to continue when:

- `Program.cs` calls `AddPagingArguments()`
- `CatalogService.GetProductsAsync` returns `Task<Page<Product>>`
- `CatalogService.GetProductsAsync` orders products before slicing the page
- `Query.GetProductsAsync` has `[UsePaging]`
- `Query.GetProductsAsync` returns `Task<Connection<Product>>`
- The resolver converts the service result with `page.ToConnection()`
- `products(first: 2)` returns `edges` and `pageInfo`
- A second query can pass the previous `endCursor` as `after`

For more options, see the [Pagination reference](/docs/hotchocolate/v16/resolvers-and-data/pagination/).

Next, continue to [Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-add-mutations/).
