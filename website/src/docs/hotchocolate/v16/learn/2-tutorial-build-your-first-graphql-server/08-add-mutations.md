---
title: "Add mutations"
description: "Add a write operation to the tutorial server, use an input and payload shape, handle a predictable domain error, and prove the write with a follow-up query."
---

In the previous chapter, you updated the `books` field to support paging, allowing clients to browse the catalog efficiently. Now, you will add your first write operation: a mutation that creates a new book, returns the created book in a payload, and models a common business error in the same response.

By the end of this chapter, you will have:

- Added a root `Mutation` type
- Enabled Hot Chocolate mutation conventions
- Created a book using an `AddBookInput`
- Returned an `AddBookPayload`
- Handled duplicate titles as a domain error
- Verified the write by querying the updated data

# Enabling Data Changes with Mutations

GraphQL distinguishes between read and write operations at the root level. Query fields are for reading data, while mutation fields are for operations that change server state.

Here is the workflow you will follow:

```
Create a book with addBook
Read the created book from the mutation payload
Query the catalog again to confirm the write
Try creating a book with the same title and observe the modeled domain error
```

Hot Chocolate ensures that [top-level mutation fields run serially](https://spec.graphql.org/October2021/#sec-Mutation) within a single operation, so their order is predictable. However, your application is responsible for data-store concerns such as constraints, transactions, idempotency, and validation.

For a deeper dive after this chapter, see [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/). For now, continue here to add the tutorial mutation.

# Enable Mutation Conventions

The recommended GraphQL mutation pattern uses a single `input` argument and returns a payload object:

```graphql
type Mutation {
  addBook(input: AddBookInput!): AddBookPayload!
}
```

The input object contains client-supplied values. The payload object contains the result data and any expected domain errors.

Hot Chocolate can generate these input and payload wrappers for you. Open `Program.cs` and add mutation conventions to the GraphQL builder:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(
    options => options.UseSqlite("Data Source=library.db"));

builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddTypes();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Keep any development logging or other registrations from earlier chapters. The key addition is:

```csharp
.AddMutationConventions(applyToAllMutations: true)
```

Build the project:

```bash
dotnet build
```

You should see:

```
Build succeeded.
```

# Add the Mutation Type

A mutation type is the write-side counterpart to the query type. Create a new file at `Types/Mutation.cs`:

```csharp
using LibraryServer.Data;

namespace LibraryServer.Types;

[MutationType]
public static partial class Mutation
{
    [GraphQLDescription("Adds a book to the library catalog.")]
    public static async Task<Book> AddBookAsync(
        string title,
        int authorId,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var book = new Book
        {
            Title = title.Trim(),
            AuthorId = authorId
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        return book;
    }
}
```

The `[MutationType]` attribute tells the source generator to include this class in the GraphQL root `Mutation` type. The class is marked `partial` so the source generator can add code at build time.

The resolver method parameters are:

| Parameter | Source | In `AddBookInput`? |
| --- | --- | --- |
| `title` | Client input | Yes |
| `authorId` | Client input | Yes |
| `LibraryDbContext db` | Dependency injection | No |
| `CancellationToken cancellationToken` | Request runtime | No |

Only client-supplied values become input fields. Services and runtime values are not included in the input object.

Build the project again:

```bash
dotnet build
```

You should see:

```
Build succeeded.
```

If the server is running, restart it:

```bash
dotnet run
```

Refresh Nitro's schema information. You should now see a `Mutation` root type:

```graphql
type Mutation {
  addBook(input: AddBookInput!): AddBookPayload!
}

input AddBookInput {
  title: String!
  authorId: Int!
}

type AddBookPayload {
  book: Book
}
```

Your schema may include descriptions or additional built-in fields. At this point, confirm:

- `Mutation` exists
- `addBook` accepts a single `input` argument
- `AddBookInput` contains `title` and `authorId`
- `AddBookPayload` exposes `book`

If `addBook` appears with separate arguments, check that `.AddMutationConventions(applyToAllMutations: true)` is present in `Program.cs`, then rebuild, restart the server, and refresh Nitro.

# Input and Payload Contracts

The generated schema defines the client contract:

```graphql
mutation AddBook($input: AddBookInput!) {
  addBook(input: $input) {
    book {
      id
      title
    }
  }
}
```

This contract is flexible. You can add fields to `AddBookInput` or `AddBookPayload` later without changing the overall operation shape. Service dependencies and runtime values remain in the resolver signature and do not appear in the GraphQL input.

For more on input types, see [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types/). For mutation naming and convention options, see [Mutation conventions](/docs/hotchocolate/v16/building-a-schema/mutations/#mutation-conventions).

# Run the Mutation in Nitro

Try the following operation in Nitro:

```graphql
mutation AddBook($input: AddBookInput!) {
  addBook(input: $input) {
    book {
      id
      title
      author {
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
    "title": "The Dispossessed",
    "authorId": 1
  }
}
```

The tutorial seed data includes an author with ID `1` (Ursula K. Le Guin). This mutation creates a new book for that author.

After running the operation, you should receive a response like:

```json
{
  "data": {
    "addBook": {
      "book": {
        "id": 6,
        "title": "The Dispossessed",
        "author": {
          "id": 1,
          "name": "Ursula K. Le Guin"
        }
      }
    }
  }
}
```

Your `id` may differ if you have added other books. Use the returned `id` for later checks.

The response structure is:

- `data.addBook` is the mutation payload
- `data.addBook.book` is the created book
- The nested `author` field is resolved by the same resolver as in previous chapters

# Verifying the Write with a Query

A successful mutation response is helpful, but a follow-up query confirms the new data is available for reading.

Run this query using the title you created:

```graphql
query FindCreatedBook($title: String!) {
  books(first: 10, where: { title: { eq: $title } }) {
    nodes {
      id
      title
      author {
        id
        name
      }
    }
  }
}
```

With these variables:

```json
{
  "title": "The Dispossessed"
}
```

You should see a response like:

```json
{
  "data": {
    "books": {
      "nodes": [
        {
          "id": 6,
          "title": "The Dispossessed",
          "author": {
            "id": 1,
            "name": "Ursula K. Le Guin"
          }
        }
      ]
    }
  }
}
```

If your `id` is different, compare the `title` and `author` fields. The important point is that the query returns the book you created.

This query uses both filtering and paging features you added earlier.

# Handling a Predictable Domain Error

Currently, the mutation allows inserting the same title multiple times. In this tutorial, duplicate titles are a predictable business error. Clients can handle this by displaying a helpful message.

Expected business errors should be included in the mutation payload. Unexpected errors, such as database outages or programming mistakes, should still appear as top-level GraphQL errors.

First, create `Types/BookTitleAlreadyExistsException.cs`:

```csharp
namespace LibraryServer.Types;

public sealed class BookTitleAlreadyExistsException(string title)
    : Exception($"A book with the title '{title}' already exists.")
{
    public string Title { get; } = title;
}
```

Next, update `Types/Mutation.cs` to check for an existing title before saving. Add the `Microsoft.EntityFrameworkCore` using, apply the `[Error]` attribute, and throw the domain exception if the title already exists:

```csharp
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[MutationType]
public static partial class Mutation
{
    [GraphQLDescription("Adds a book to the library catalog.")]
    [Error(typeof(BookTitleAlreadyExistsException))]
    public static async Task<Book> AddBookAsync(
        string title,
        int authorId,
        LibraryDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = title.Trim();

        var titleExists = await db.Books
            .AnyAsync(b => b.Title == normalizedTitle, cancellationToken);

        if (titleExists)
        {
            throw new BookTitleAlreadyExistsException(normalizedTitle);
        }

        var book = new Book
        {
            Title = normalizedTitle,
            AuthorId = authorId
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        return book;
    }
}
```

The `[Error(typeof(BookTitleAlreadyExistsException))]` attribute tells Hot Chocolate to treat this exception as a modeled domain error for the mutation. Hot Chocolate adds an `errors` field to the payload and maps the exception to a schema error type.

Build and restart the server:

```bash
dotnet build
dotnet run
```

Refresh Nitro's schema information. The payload should now look like this:

```graphql
type AddBookPayload {
  book: Book
  errors: [AddBookError!]
}

interface Error {
  message: String!
}

type BookTitleAlreadyExistsError implements Error {
  message: String!
}

union AddBookError = BookTitleAlreadyExistsError
```

The exception type name is rewritten for the schema: `BookTitleAlreadyExistsException` becomes `BookTitleAlreadyExistsError`.

For more on error design, including custom error fields and factories, see [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [Domain errors](/docs/hotchocolate/v16/building-a-schema/mutations/#domain-errors).

# Testing the Failure Case

Try running the mutation again with the same title. This time, select the `errors` field in the payload:

```graphql
mutation AddDuplicateBook($input: AddBookInput!) {
  addBook(input: $input) {
    book {
      id
      title
    }
    errors {
      __typename
      ... on Error {
        message
      }
    }
  }
}
```

Use the same variables as before:

```json
{
  "input": {
    "title": "The Dispossessed",
    "authorId": 1
  }
}
```

You should receive a response like:

```json
{
  "data": {
    "addBook": {
      "book": null,
      "errors": [
        {
          "__typename": "BookTitleAlreadyExistsError",
          "message": "A book with the title 'The Dispossessed' already exists."
        }
      ]
    }
  }
}
```

Notice that the failure appears in `data.addBook.errors`, not as a top-level GraphQL error. This means the server handled the expected business error and returned it as part of the mutation contract.

Run the verification query again to confirm only one book exists with that title:

```graphql
query FindCreatedBook($title: String!) {
  books(first: 10, where: { title: { eq: $title } }) {
    nodes {
      id
      title
    }
  }
}
```

With these variables:

```json
{
  "title": "The Dispossessed"
}
```

Check that:

- The query returns one matching book
- The duplicate mutation did not create a second book
- The duplicate response contains `BookTitleAlreadyExistsError`

# Production Write Considerations

The tutorial now has a working local create operation. In production, you will likely need to address additional concerns:

- Authorization: Who can create or modify data
- Validation: Input rules and user-facing messages
- Database constraints: Uniqueness and referential integrity
- Transactions: When an operation changes multiple records
- Idempotency: Handling retries
- Audit logging
- Monitoring and operational alerts

Hot Chocolate supports transaction handling. See [Transactions](/docs/hotchocolate/v16/building-a-schema/mutations/#transactions) for details on the built-in transaction scope handler.

This chapter keeps the write path focused so you can understand the GraphQL contract first.

# Checkpoint: Mutations Work

If any verification step fails, review the relevant section before continuing.

You are ready for the next chapter when all of these are true:

- `.AddMutationConventions(applyToAllMutations: true)` is present in `Program.cs`
- `Types/Mutation.cs` contains a `[MutationType]` class with `AddBookAsync`
- `Types/BookTitleAlreadyExistsException.cs` exists
- `dotnet build` succeeds
- Nitro shows a root `Mutation` type
- Nitro shows `addBook(input: AddBookInput!): AddBookPayload!`
- `AddBookInput` contains `title` and `authorId`
- `AddBookPayload` exposes `book` and `errors`
- Creating a new title returns a payload with the created book
- A follow-up `books` query finds the created book
- Running the same title again returns `BookTitleAlreadyExistsError` in the payload
- The duplicate failure does not create a second book

To repeat the success path, try creating a book with a new title:

```json
{
  "input": {
    "title": "Always Coming Home",
    "authorId": 1
  }
}
```

# Troubleshooting Common Mutation Issues

Use this table to resolve common problems:

| Symptom | Likely cause | Fix | Verify |
| --- | --- | --- | --- |
| `Mutation` does not appear in Nitro | App was not rebuilt, server is running an old build, or mutation type is missing `[MutationType]` | Compare `Types/Mutation.cs` with this chapter, run `dotnet build`, restart the server, and refresh Nitro | Nitro shows `Mutation` |
| `addBook` has separate `title` and `authorId` arguments | Mutation conventions are not enabled for all mutations | Add `.AddMutationConventions(applyToAllMutations: true)` before `.AddTypes()` in `Program.cs` | `addBook` accepts a single `input` argument |
| `LibraryDbContext` or `cancellationToken` appears in the input | Resolver parameter is not recognized as a service or runtime value | Confirm `LibraryDbContext` is registered with `AddDbContext` and the parameter type is `CancellationToken` | `AddBookInput` contains only client values |
| Variables fail validation | JSON variable shape does not match `AddBookInput` | Inspect `AddBookInput` in Nitro and align variable names and value types | Mutation validates before execution |
| Duplicate input returns a top-level GraphQL error | Mutation is missing `[Error(typeof(BookTitleAlreadyExistsException))]` or a different exception type is thrown | Confirm the attribute and thrown exception type match | Duplicate response has `data.addBook.errors` |
| Follow-up query does not show the new book | Title filter differs, server is using a different database file, or database was reset | Copy the title from the mutation response and query again, or use the created `id` with `bookById` | Query returns the created book |

# Next Steps

Continue to [Add subscriptions](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/09-add-subscriptions/) when both the create mutation and duplicate error work as expected.

If your project no longer matches the tutorial, use [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) and [Stuck](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) to recover before moving on.
