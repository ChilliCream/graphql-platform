---
title: "Add arguments and filters"
description: "Add a field argument, query it with GraphQL variables, enable Hot Chocolate filtering, and verify that clients can request a smaller set of books."
---

In the previous chapter, your server returned all library data through resolvers. The `books` field allowed clients to read the catalog, but every request returned the entire collection.

This chapter shows how to let clients request a smaller set of data at execution time.

You will implement two ways to narrow results:

- A hand-written argument for a specific lookup: `bookById(id:)`
- Generated filtering on the `books` list, so clients can specify flexible criteria

Arguments and filters both appear in the schema and are supplied by clients in GraphQL operations, but they serve different purposes. A hand-written argument is part of the field contract you design. Filtering is Hot Chocolate data middleware that adds a conventional `where` argument to a collection field.

# Start from the current books query

Before making changes, run the current list query in Nitro:

```graphql
{
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

The exact values will depend on the sample data you created earlier. This chapter uses the following catalog as an example:

```json
{
  "data": {
    "books": [
      {
        "id": "book-1",
        "title": "The Left Hand of Darkness",
        "author": {
          "id": "author-1",
          "name": "Ursula K. Le Guin"
        }
      },
      {
        "id": "book-2",
        "title": "Kindred",
        "author": {
          "id": "author-2",
          "name": "Octavia E. Butler"
        }
      }
    ]
  }
}
```

If your sample data is different, use IDs and titles that exist in your local response. The important thing is that the unfiltered `books` query returns more than one book, so you can see the effect of narrowing the results later.

# Add an argument to a query field

Let’s start with a task that has clear API meaning: fetch a single book by its stable identifier.

Open `Types/Query.cs` and add a `GetBookById` resolver next to your existing `GetBooks` resolver:

```csharp
using LibraryServer.Services;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    public static IReadOnlyList<Book> GetBooks(BookCatalog catalog)
        => catalog.GetBooks();

    [GraphQLDescription("Gets one book by its stable identifier.")]
    public static Book? GetBookById([ID] string id, BookCatalog catalog)
        => catalog.GetBooks().FirstOrDefault(book => book.Id == id);
}
```

If your previous code used a different service name, keep that name and copy the new `GetBookById` method shape. The `id` parameter becomes a GraphQL field argument. The `BookCatalog` parameter is a service parameter, so Hot Chocolate gets it from dependency injection and does not expose it as a client argument.

The `[ID]` attribute ensures the argument uses the GraphQL `ID` scalar:

```graphql
type Query {
  "Gets one book by its stable identifier."
  bookById(id: ID!): Book
}
```

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Restart the server if it is running, then refresh Nitro’s schema information. Confirm that the root `Query` type now includes a `bookById` field with a required `id` argument.

# Run the argument query in Nitro

First, call the field with a literal value. Paste this operation into Nitro:

```graphql
{
  bookById(id: "book-2") {
    id
    title
    author {
      name
    }
  }
}
```

Run the operation.

Expected response:

```json
{
  "data": {
    "bookById": {
      "id": "book-2",
      "title": "Kindred",
      "author": {
        "name": "Octavia E. Butler"
      }
    }
  }
}
```

The argument is part of the field call:

```graphql
bookById(id: "book-2")
```

The selection set still controls the response shape:

```graphql
{
  id
  title
  author {
    name
  }
}
```

Try changing the literal to another known ID, such as `"book-1"`, and run the operation again. The selected fields remain the same, but `data.bookById` changes.

If you pass an ID that does not exist in your sample data, the operation succeeds and the resolver returns `null`:

```graphql
{
  bookById(id: "missing-book") {
    id
    title
  }
}
```

Expected response:

```json
{
  "data": {
    "bookById": null
  }
}
```

This is valid output for this resolver. The input is valid, but no book matched it.

# Move changing values into variables

Literal values are useful for quick checks, but real clients usually keep the operation text stable and send changing values as variables.

Replace the operation with this named version:

```graphql
query GetBook($id: ID!) {
  bookById(id: $id) {
    id
    title
    author {
      name
    }
  }
}
```

Open Nitro’s variables panel and enter this JSON:

```json
{
  "id": "book-2"
}
```

Run the operation.

Expected response:

```json
{
  "data": {
    "bookById": {
      "id": "book-2",
      "title": "Kindred",
      "author": {
        "name": "Octavia E. Butler"
      }
    }
  }
}
```

Now change only the variables JSON:

```json
{
  "id": "book-1"
}
```

Run the operation again. The operation text stays the same, and the response changes to the book with ID `book-1`.

Here’s a mapping to help debug variables:

| Piece | Example | What it does |
| --- | --- | --- |
| C# resolver parameter | `[ID] string id` | Receives the input value in your resolver. |
| Schema argument | `bookById(id: ID!)` | Declares that clients can pass `id` to `bookById`. |
| Operation variable | `$id: ID!` | Declares a runtime value for this operation. |
| Field argument in the operation | `bookById(id: $id)` | Passes the variable value into the field argument. |
| Variables JSON | `{ "id": "book-2" }` | Supplies the runtime value. |

For more argument options, such as optional arguments, default values, argument renaming, input objects, and ID handling, see the [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) reference after you finish this tutorial step. The [GraphQL specification](https://spec.graphql.org/) defines the core rules for variables and arguments.

# Add filtering to the books list

The `bookById` field is useful for a specific lookup, but it is not practical to hand-write every possible list condition.

For flexible list narrowing, use Hot Chocolate filtering. Filtering is data middleware that examines the `Book` type and generates a `where` argument with filter input fields for properties like `id`, `title`, and `author`.

Filtering is provided by the `HotChocolate.Data` package. Add it from the `LibraryServer` project directory:

```bash
dotnet add package HotChocolate.Data
```

If your project uses pinned package versions, make sure the `HotChocolate.Data` version matches the other `HotChocolate.*` package versions in `LibraryServer.csproj`.

Open `Program.cs` and register filtering with the GraphQL builder:

```csharp
using LibraryServer.Services;
using LibraryServer.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookCatalog>();

builder
    .AddGraphQL()
    .AddFiltering()
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Keep any service registrations you added in earlier chapters. The important new call is `.AddFiltering()`.

Now apply filtering to the `books` field. Update `GetBooks` in `Types/Query.cs`:

```csharp
using LibraryServer.Services;

namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [UseFiltering]
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    public static IQueryable<Book> GetBooks(BookCatalog catalog)
        => catalog.GetBooks().AsQueryable();

    [GraphQLDescription("Gets one book by its stable identifier.")]
    public static Book? GetBookById([ID] string id, BookCatalog catalog)
        => catalog.GetBooks().FirstOrDefault(book => book.Id == id);
}
```

The `[UseFiltering]` attribute adds filtering middleware to the field. The resolver now returns an `IQueryable<Book>` so the filtering middleware can apply the generated filter expression to the in-memory sample data. In the next chapter, this same client query shape will become more useful when the data comes from a real data source.

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Restart the server and refresh Nitro’s schema information. The `books` field should now have a `where` argument:

```graphql
type Query {
  books(where: BookFilterInput): [Book!]!
}
```

Nitro should also show a generated input type with fields for `Book`:

```graphql
input BookFilterInput {
  and: [BookFilterInput!]
  or: [BookFilterInput!]
  id: StringOperationFilterInput
  title: StringOperationFilterInput
  author: AuthorFilterInput
}
```

The exact generated input shape may include additional fields or slightly different scalar operation types depending on your model. Use the schema shown by Nitro as the source of truth.

# Write a filtered query

Now run a query that filters the `books` list by title. Paste this operation into Nitro:

```graphql
query GetKindredBooks {
  books(where: { title: { contains: "Kindred" } }) {
    id
    title
    author {
      name
    }
  }
}
```

Run the operation.

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "book-2",
        "title": "Kindred",
        "author": {
          "name": "Octavia E. Butler"
        }
      }
    ]
  }
}
```

Compare this response with the unfiltered `books` query. The filtered response contains fewer books because the `where` object narrowed the collection to titles containing `Kindred`.

You can also move the filter value into a variable. Replace the operation with this version:

```graphql
query GetBooksByTitle($title: String!) {
  books(where: { title: { contains: $title } }) {
    id
    title
    author {
      name
    }
  }
}
```

Use this variables JSON:

```json
{
  "title": "Kindred"
}
```

Run the operation again. The result should match the previous filtered response. Change the variable value to `"Left"` and run it again to return `The Left Hand of Darkness`.

# Choose between an argument and a filter

You now have both `bookById(id:)` and `books(where:)` available. Both patterns are useful in different situations:

| Client need | Use | Why |
| --- | --- | --- |
| Lookup by a known identifier | Hand-written argument | The field has specific API meaning: fetch one book by ID. |
| Business-specific operation | Hand-written argument | You design the resolver contract and behavior. |
| Narrow a list by several possible fields | Filtering | Hot Chocolate generates the `where` input from your model. |
| Let clients combine open-ended criteria | Filtering | Clients can compose supported filter operations without new resolver methods. |

Filtering does not replace domain-specific fields. It gives list fields a conventional way to accept flexible criteria. Hand-written arguments are still the right choice when the field name and argument together express a specific operation.

# Understand middleware order

Filtering is middleware applied to a field. When a field uses more than one data middleware, order matters.

The recommended order is:

```
UsePaging > UseProjection > UseFiltering > UseSorting
```

This chapter uses filtering alone, so there is no visible ordering decision on the `books` field yet. Later chapters will add more data features and return to this rule.

For more details after the tutorial, see [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/).

# Fix common mismatches

Use this section if the schema or response does not match the checkpoint.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `bookById` does not appear on `Query` | The project did not rebuild, the server is still running old code, or the method was added outside the registered query type | Run `dotnet build`, restart the server, refresh Nitro's schema information, and confirm the method is in `Types/Query.cs` under `[QueryType]`. |
| Nitro reports that the `id` variable is missing or has the wrong type | The variable declaration, field argument, or variables JSON do not match | Use `$id: ID!`, `bookById(id: $id)`, and a variables JSON property named `"id"`. |
| `bookById` returns `null` | No sample book has the ID you passed | Run the unfiltered `books` query, copy a visible `id`, and retry. |
| `.AddFiltering()` or `[UseFiltering]` is not recognized | `HotChocolate.Data` is missing or the package version does not match the other Hot Chocolate packages | Add `HotChocolate.Data`, restore packages, and keep package versions aligned. |
| `books` does not show a `where` argument | Filtering was registered but not applied to the field, filtering was applied but not registered, or Nitro has stale schema information | Confirm both `.AddFiltering()` in `Program.cs` and `[UseFiltering]` on `GetBooks`, then rebuild, restart, and refresh Nitro. |
| The filter operation fails validation | The operation does not match the generated filter input type | Inspect `BookFilterInput` in Nitro and use an operation shown for the selected field. |
| The filtered response is not smaller | The filter value matches every row or your sample data differs | Choose a value that appears in only part of your unfiltered response. |

For broader troubleshooting, see [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) or [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/).

# Checkpoint: arguments and filters are working

Stop here if any verification item fails. Fix any mismatches before continuing.

You are ready for the next chapter when all of these are true:

- `dotnet build` succeeds.
- `dotnet run` starts the server.
- Nitro connects to your local `/graphql` endpoint.
- The root `Query` type includes `bookById(id: ID!)`.
- The variables-based `bookById` query returns the expected book for a known ID.
- The root `Query` type includes `books(where: BookFilterInput)`.
- The filtered `books` query is accepted.
- The filtered response contains fewer books than the unfiltered response.
- You can explain when to use a hand-written argument and when to use filtering.

You have now added client-driven narrowing to the tutorial API. Continue to [Connect to real data](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-connect-to-real-data/) to keep the same API shape while changing where the data comes from.
