---
title: "Define your first types"
description: "Model the first library domain types, expose them through the Query root, add schema descriptions, and inspect the generated contract in Nitro."
---

In the previous chapter, you set up the `LibraryServer` project, ran it, and opened Nitro at `/graphql`.

Now, you will transform the starter schema into the first version of your library's GraphQL contract. The schema defines the public API that clients explore and query. Object types describe the shapes of data clients can request.

In this chapter, you will:

- Replace the starter book model with library domain types
- Expose those types through a `books` query field
- Add descriptions that appear in schema tools
- Inspect the generated schema in Nitro

Resolver logic and real data sources will come later. For now, the focus is on the schema's structure, keeping the return value minimal to highlight the contract.

# Create the first domain types

The library domain begins with two main entities: `Book` and `Author`.

These are standard C# classes. Hot Chocolate includes them in the GraphQL schema when they are returned by a GraphQL field. In this step, that field will be defined on the root `Query` type.

Replace the contents of `Types/Author.cs` with:

```csharp
namespace LibraryServer.Types;

public sealed class Author
{
    public required string Id { get; init; }

    public required string Name { get; init; }
}
```

Next, update `Types/Book.cs`:

```csharp
namespace LibraryServer.Types;

public sealed class Book
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required Author Author { get; init; }
}
```

If your project uses a different namespace, use the one generated for your project. You can check the correct namespace by looking at the existing files in the `Types` folder before making changes.

Do not build the project yet. The starter `Query.cs` still references the old sample types. Update the root query in the next section before running a build.

At this point, the schema has not changed in a way that is visible to clients. The new types will appear in the schema once a root field returns them.

# Add object types through a query field

The root query type is the entry point for reading data from the schema. The starter project includes `Types/Query.cs`. Replace its contents with a `books` field that returns a list of `Book` objects:

```csharp
namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    public static IReadOnlyList<Book> GetBooks()
        => new[]
        {
            new Book
            {
                Id = "book-1",
                Title = "C# in Depth",
                Author = new Author
                {
                    Id = "author-1",
                    Name = "Jon Skeet"
                }
            }
        };
}
```

Hot Chocolate exposes this method as a GraphQL field named `books`. Since the method returns `Book`, and `Book` contains an `Author`, both types are included in the generated schema.

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

If the server was already running, stop it with <kbd>Ctrl</kbd> + <kbd>C</kbd> and start it again. Restarting ensures Nitro loads the updated schema.

# Read the generated schema names

Type and field names form the public API. Clients use the names defined in the GraphQL schema, not the C# member names.

For this project, the mapping is as follows:

| C# code | GraphQL schema name |
| --- | --- |
| `Book` | `Book` |
| `Author` | `Author` |
| `GetBooks()` | `books` |
| `Book.Id` | `id` |
| `Book.Title` | `title` |
| `Book.Author` | `author` |
| `Author.Name` | `name` |

C# type names appear as PascalCase object types in GraphQL. Public properties are mapped to lower camel case field names.

Once clients depend on these names, keep them stable. Later chapters will update resolver logic and data sources, but the client-facing field names should remain consistent.

For more details, see the [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) reference after completing this chapter.

# Add descriptions for schema readers

Descriptions are part of the schema's metadata. Tools like Nitro, IDEs, and introspection clients display them to developers exploring the API.

Add descriptions using `[GraphQLDescription]`. Update `Types/Author.cs` as follows:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A person who wrote one or more books in the library catalog.")]
public sealed class Author
{
    [GraphQLDescription("The stable identifier for the author.")]
    public required string Id { get; init; }

    [GraphQLDescription("The author's display name.")]
    public required string Name { get; init; }
}
```

Update `Types/Book.cs`:

```csharp
namespace LibraryServer.Types;

[GraphQLDescription("A book that can be browsed in the library catalog.")]
public sealed class Book
{
    [GraphQLDescription("The stable identifier for the book.")]
    public required string Id { get; init; }

    [GraphQLDescription("The title printed for readers.")]
    public required string Title { get; init; }

    [GraphQLDescription("The person who wrote the book.")]
    public required Author Author { get; init; }
}
```

Add a description to the query field in `Types/Query.cs`:

```csharp
namespace LibraryServer.Types;

[QueryType]
public static partial class Query
{
    [GraphQLDescription("Gets the books currently available in the library catalog.")]
    public static IReadOnlyList<Book> GetBooks()
        => new[]
        {
            new Book
            {
                Id = "book-1",
                Title = "C# in Depth",
                Author = new Author
                {
                    Id = "author-1",
                    Name = "Jon Skeet"
                }
            }
        };
}
```

Descriptions should clarify the meaning or purpose of a type or field, rather than restating its name.

Build the project again:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

Restart the server if it is running.

For more on schema documentation, see the [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation/) reference after this step.

# Inspect the schema contract in Nitro

Return to the Nitro tab you opened earlier. If it is closed, open the server URL from the terminal and add `/graphql`.

For example:

```text
http://localhost:5095/graphql
```

Use your local port if it differs from the example.

Open the schema view or explorer in Nitro and refresh the schema if it still shows the starter version.

Check for the following:

- The root `Query` type includes a `books` field
- The `books` field returns a list of `Book` objects
- The `Book` type has `id`, `title`, and `author` fields
- The `Author` type has `id` and `name` fields
- Descriptions are present on `Book`, `Author`, `books`, and the described fields

The schema should look like this:

```graphql
type Query {
  "Gets the books currently available in the library catalog."
  books: [Book!]!
}

"A book that can be browsed in the library catalog."
type Book {
  "The stable identifier for the book."
  id: String!
  "The title printed for readers."
  title: String!
  "The person who wrote the book."
  author: Author!
}

"A person who wrote one or more books in the library catalog."
type Author {
  "The stable identifier for the author."
  id: String!
  "The author's display name."
  name: String!
}
```

If your nullability markers differ, compare your C# property types with the code above. Properties marked as `required string` or `required Author` should result in non-null fields. The `Id` properties are strings, so they appear as `String!` in the schema.

You can also run a query to confirm the field names clients will use:

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

Expected response:

```json
{
  "data": {
    "books": [
      {
        "id": "book-1",
        "title": "C# in Depth",
        "author": {
          "id": "author-1",
          "name": "Jon Skeet"
        }
      }
    ]
  }
}
```

Treat Nitro's schema view as the source of truth for client-facing names. GraphQL field names are case-sensitive. For example, a client query using `Title` instead of `title` will not work.

# Fix common mismatches

If your schema does not match the expected result, use this table to troubleshoot:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Book` or `Author` does not appear | The type is not reachable from a root field, or Nitro has stale schema information | Confirm `GetBooks()` returns `IReadOnlyList<Book>`, rebuild, restart the server, and refresh Nitro |
| `books` does not appear on `Query` | `Types/Query.cs` was not replaced or the project did not rebuild | Compare `Query.cs` with this chapter and run `dotnet build` |
| Descriptions do not appear | The attribute was added to the wrong type or member, or the server was not restarted | Confirm `[GraphQLDescription]` is on the class, property, or method, then rebuild and restart |
| Field names use different casing than your C# properties | Hot Chocolate maps C# member names to GraphQL naming conventions | Use the names shown in Nitro when writing client operations |
| The query returns an error | The placeholder object is missing a required property | Confirm every `required` property in `Book` and `Author` is assigned in `GetBooks()` |

If you need to compare your local state, see [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) for reference implementations and checkpoints.

# Checkpoint: your first schema contract

Continue when all of these are true:

- `dotnet build` succeeds
- `dotnet run` starts the server
- Nitro connects to your local `/graphql` endpoint
- The root `Query` type includes `books`
- The `Book` type includes `id`, `title`, and `author`
- The `Author` type includes `id` and `name`
- The schema uses lower camel case for field names
- Descriptions appear for the types and fields you annotated
- The verification query returns the expected `books` response

You have now defined the first version of your library schema contract. Next, proceed to [Write resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-resolvers/) to move from placeholder data to resolver logic you will build on throughout the tutorial.
