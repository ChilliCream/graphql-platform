---
title: "Add pagination"
description: "Turn the tutorial books field into a cursor-paginated connection, query the first page, continue with a cursor, and read edges and pageInfo."
---

In the previous chapter, you optimized nested author loading with DataLoader. Now, the `books` field can efficiently return more data, but its API still exposes a plain list.

This chapter updates that contract. Instead of returning all books, the server will provide a page of books along with the information needed to fetch more. Clients will request a specific number of items, and the server will supply navigation details for paging.

By the end of this chapter, you will:

- Add cursor-based pagination to the `books` field
- Confirm the schema exposes a connection type
- Query the first page of books
- Fetch the next page using `pageInfo.endCursor`
- Understand the `edges`, `node`, `cursor`, and `pageInfo` structure in the response

Hot Chocolate uses the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm) for pagination by default. You do not need to use the Relay client; this pattern benefits any GraphQL client that needs to page through results.



Try running the current query in Nitro:

```graphql
query GetBooks {
  books {
    id
    title
    author {
      id
      name
    }
  }
}
```

The response is a simple JSON array:

```json
{
  "data": {
    "books": [
      {
        "id": "1",
        "title": "The Left Hand of Darkness",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "2",
        "title": "A Wizard of Earthsea",
        "author": {
          "id": "1",
          "name": "Ursula K. Le Guin"
        }
      }
    ]
  }
}
```

Your actual response will include all seeded books, not only the two shown here.

While this format works for a small catalog, it does not scale well. The client cannot control how many items it receives, which can lead to large, slow responses and extra work for both server and client as the data set grows.

Pagination improves this by letting the client specify how many items to fetch and by providing navigation metadata:

```
Client requests a page size and optional cursor.
Server returns a page of items and navigation info.
Client uses the returned cursor to fetch more when needed.
```

Here are the key terms for this chapter:

| Term        | Meaning in this chapter                                 |
|-------------|--------------------------------------------------------|
| Connection  | The paginated wrapper around the `books` field.        |
| Edge        | A single item in the current page.                     |
| Node        | The `Book` object inside an edge.                      |
| Cursor      | An opaque value identifying an edge's position.        |
| `pageInfo`  | Metadata about page boundaries and navigation.         |
| Page size   | The number of items requested with `first`.            |

For a broader discussion, see [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) after this chapter.

# Add cursor paging to the books field

Open `Types/Query.cs`.

Your `GetBooks` resolver should look like this, with `[UseFiltering]` and a stable order by `Id`:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    [UseFiltering]
    public static IQueryable<Book> GetBooks(LibraryDbContext db)
        => db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id);

    [GraphQLDescription("Gets one book from the library catalog by its identifier.")]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
```

To enable paging, add `[UsePaging]` above `[UseFiltering]`:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    [UsePaging]
    [UseFiltering]
    public static IQueryable<Book> GetBooks(LibraryDbContext db)
        => db.Books
            .AsNoTracking()
            .OrderBy(b => b.Id);

    [GraphQLDescription("Gets one book from the library catalog by its identifier.")]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        return await db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
```

The resolver still returns `IQueryable<Book>`. Hot Chocolate applies paging to that queryable result and exposes a GraphQL connection shape to clients.

Keep the `OrderBy(b => b.Id)` call. Cursor pagination needs a stable order so the first page and the next page do not overlap or skip items while the same data set is being read.

Build the project:

```bash
dotnet build
```

Expected checkpoint:

```text
Build succeeded.
```

Restart the server if it is running:

```bash
dotnet run
```

# Verify the schema changed to a connection

Open Nitro at your local `/graphql` endpoint. Use the port printed by the server:

```text
http://localhost:5095/graphql
```

Refresh Nitro's schema information.

Find the `books` field on the root `Query` type. It should no longer return a plain list of `Book`. It should accept pagination arguments and return a connection type.

The schema outline should look similar to this:

```graphql
type Query {
  books(
    first: Int
    after: String
    last: Int
    before: String
    where: BookFilterInput
  ): BooksConnection
}

type BooksConnection {
  pageInfo: PageInfo!
  edges: [BooksEdge!]
  nodes: [Book!]
}

type BooksEdge {
  cursor: String!
  node: Book!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

The key checkpoints are:

- `books` accepts `first` and `after`
- `books` returns a connection type
- The connection exposes `edges` and `pageInfo`
- Each edge exposes `cursor` and `node`

If Nitro still shows `books: [Book!]`, restart the server and refresh the schema. Also check that `[UsePaging]` is on `GetBooks`, not `GetBookByIdAsync`.

# Fetch the first page

Now, query for a page of two books. The tutorial seed data includes five books, so `first: 2` will leave more to fetch.

Paste this query into Nitro:

```graphql
query GetFirstBooksPage {
  books(first: 2) {
    edges {
      cursor
      node {
        id
        title
        author {
          id
          name
        }
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Run the query.

You should receive a response like this:

```json
{
  "data": {
    "books": {
      "edges": [
        {
          "cursor": "MA==",
          "node": {
            "id": "1",
            "title": "The Left Hand of Darkness",
            "author": {
              "id": "1",
              "name": "Ursula K. Le Guin"
            }
          }
        },
        {
          "cursor": "MQ==",
          "node": {
            "id": "2",
            "title": "A Wizard of Earthsea",
            "author": {
              "id": "1",
              "name": "Ursula K. Le Guin"
            }
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "MQ=="
      }
    }
  }
}
```

Cursor values may differ. Treat them as opaque: clients should store and resend cursors, not decode or construct them.

Key elements in this response:

- `edges[0].node` and `edges[1].node`: the two `Book` objects for this page
- `edges[0].cursor` and `edges[1].cursor`: the position of each item
- `pageInfo.hasNextPage`: whether more pages exist
- `pageInfo.endCursor`: the cursor to use as `after` for the next page

# Fetch the next page with endCursor

Copy the `pageInfo.endCursor` value from your first response.

Use it as the `after` variable in the next query:

```graphql
query GetNextBooksPage($after: String!) {
  books(first: 2, after: $after) {
    edges {
      cursor
      node {
        id
        title
        author {
          id
          name
        }
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

In Nitro, set the variables to:

```json
{
  "after": "MQ=="
}
```

Run the query.

You should see a response like this:

```json
{
  "data": {
    "books": {
      "edges": [
        {
          "cursor": "Mg==",
          "node": {
            "id": "3",
            "title": "Kindred",
            "author": {
              "id": "2",
              "name": "Octavia E. Butler"
            }
          }
        },
        {
          "cursor": "Mw==",
          "node": {
            "id": "4",
            "title": "Parable of the Sower",
            "author": {
              "id": "2",
              "name": "Octavia E. Butler"
            }
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "Mw=="
      }
    }
  }
}
```

Compare this with the first page. The `node.id` values should be later in the catalog and should not repeat the first page.

Clients repeat this process until `hasNextPage` is `false`:

```
Run books(first: 2)
Read pageInfo.endCursor
Run books(first: 2, after: endCursor)
Continue while pageInfo.hasNextPage is true
```

With the tutorial data, a third request using the second response's `endCursor` will return the final book. If fewer items remain than the requested page size, the page will have fewer edges and `hasNextPage` will be `false`.

# Read the connection result

After two queries, the connection structure should be clearer:

```json
{
  "data": {
    "books": {
      "edges": [
        {
          "cursor": "cursor-for-this-position",
          "node": {
            "id": "book-id",
            "title": "book-title"
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "endCursor": "cursor-for-the-last-edge"
      }
    }
  }
}
```

To interpret this structure:

1. `books` is the paginated connection
2. `edges` lists the items in the current page
3. `edge.node` is the `Book` object
4. `edge.cursor` marks the item's position
5. `pageInfo.hasNextPage` indicates if more pages are available
6. `pageInfo.endCursor` is used as `after` for the next request

The `node` field inside an edge refers to the object for that edge. This is unrelated to Relay's global object identification. For more on global IDs and the top-level `node` query, see [Relay](/docs/hotchocolate/v16/building-a-schema/relay/).

# Checkpoint: paged queries work

If any verification step fails, pause here and resolve the issue.

You are ready to continue when all of these are true:

- `[UsePaging]` is above `[UseFiltering]` on `GetBooks` in `Types/Query.cs`
- `GetBooks` still returns `IQueryable<Book>`
- `GetBooks` uses a stable `OrderBy(b => b.Id)`
- `dotnet build` succeeds
- `dotnet run` starts the server
- Nitro shows `books` with `first` and `after` arguments
- Nitro shows a connection return type for `books`
- Querying `books(first: 2)` returns two edges and `pageInfo`
- `pageInfo.hasNextPage` is `true` for the first page with the tutorial data
- The next page query uses the previous response's `endCursor` as `after`
- The second page returns later books, not duplicates

You can rerun this operation at any time:

```graphql
query CheckPagedBooks($after: String) {
  books(first: 2, after: $after) {
    edges {
      cursor
      node {
        id
        title
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Run it once with no variables or with:

```json
{}
```

Then run it again with:

```json
{
  "after": "<endCursor from the previous response>"
}
```

# Fix common pagination mistakes

Use this table to troubleshoot common issues:

| Symptom | Likely cause | Fix | Verify |
|---------|--------------|-----|--------|
| `books` still returns a plain list | Paging was added to the wrong resolver, the app was not restarted, or Nitro has stale schema info | Add `[UsePaging]` to `GetBooks`, restart the server, and refresh Nitro | `books` accepts `first` and `after` |
| `first` or `after` is rejected | The operation targets the old schema or the server is still running the previous build | Stop the server, run `dotnet build`, start it again, and refresh Nitro | The first page query validates |
| The same books appear on multiple pages | The resolver does not have a deterministic order before paging | Keep `.OrderBy(b => b.Id)` on the `IQueryable<Book>` | The second page starts after the first page's last book |
| The second page is empty | `hasNextPage` was already `false`, the cursor came from another field, or the data set is too small | Use the `endCursor` from the same `books` response and a page size smaller than the seed count | The next page returns later books |
| The response returns fewer items than `first` | Fewer items remain, the data set has fewer rows than expected, or a maximum page size applies | Check the seed data and current page, then inspect paging options if needed | The final page can have fewer edges and `hasNextPage: false` |
| `UsePaging` cannot be found | The project is missing the package or package versions are not aligned | Make sure the Hot Chocolate packages in the project use the same version | `dotnet build` succeeds |

For more on pagination options, including maximum page size, total count, backward pagination, connection naming, and custom providers, see the [Pagination reference](/docs/hotchocolate/v16/resolvers-and-data/pagination/).

# Next steps

Continue to [Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/08-add-mutations/) when your paged `books` query works.

If your project no longer matches the tutorial, use [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) and [Stuck](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) to get back on track before moving on.
