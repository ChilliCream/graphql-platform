---
title: "Add a field"
description: "Add one property-backed field and one computed resolver field to the scaffolded Hot Chocolate schema, then query both fields in Nitro."
---

This lesson builds on the scaffolded `GettingStarted` server and demonstrates how to make a minimal but meaningful schema change: adding a new field to the `Book` type.

By the end, you will have:

- Verified the starter `book` query
- Added a stored value to `Book`
- Added a computed field to `Book`
- Refreshed Nitro's schema information
- Queried the new fields and checked the JSON response

Before you begin, ensure your project matches the state from [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) and that you can run the starter query from [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query). If your server is not running, open a terminal in the `GettingStarted` project directory and start it with:

```bash
dotnet run
```

Keep this terminal open until you reach the code editing step.

# Start with the working `book` query

Open Nitro at your local `/graphql` endpoint. The port may vary depending on your setup:

```text
http://localhost:5095/graphql
```

Paste the starter query into Nitro's operation editor:

```graphql
{
  book {
    title
    author {
      name
    }
  }
}
```

Click **Run** to execute the query.

You should see this response:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

This is your starting point. The schema currently defines a `Book` object type with `title` and `author` fields. Next, you will add new fields to this type.

# Add a field that stores a value

Stop the running server by pressing <kbd>Ctrl</kbd> + <kbd>C</kbd>. For a reliable workflow, rebuild and restart the server after making C# changes.

Open `Types/Book.cs`. The scaffolded file should look like this:

```csharp
namespace GettingStarted.Types;

public record Book(string Title, Author Author);
```

Add a `PublishedYear` value to the record:

```csharp
namespace GettingStarted.Types;

public record Book(string Title, Author Author, int PublishedYear);
```

Since the `Book` constructor now expects a third value, update the returned `Book` in `Types/Query.cs`:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"), 2019);
}
```

If your namespace differs due to a different project name, keep your existing namespace and update only the `Book` record and the `new Book(...)` call.

Restart the server:

```bash
dotnet run
```

You should see output similar to:

```text
Now listening on: http://localhost:5095
```

The port may change after a restart. If it does, copy the new URL and open the corresponding `/graphql` endpoint.

Hot Chocolate exposes public C# properties as GraphQL fields on object types. The `PublishedYear` parameter creates a public property, which appears in the GraphQL schema as `publishedYear`.

# Select the new field in the query

Return to Nitro. If the schema view or autocomplete still shows the old `Book` type, refresh Nitro's schema information or reload the browser page.

Add `publishedYear` to the `book` selection set:

```graphql
{
  book {
    title
    publishedYear
    author {
      name
    }
  }
}
```

Click **Run**.

You should receive this response:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "publishedYear": 2019,
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

The response now includes `publishedYear` because you selected it in the query. If you remove `publishedYear` from the selection set and run the query again, the field will not appear in the JSON response, even though it still exists in the schema.

# Add a field that calculates a value

Fields do not have to store data directly. You can define a method that computes a value. Stop the server with <kbd>Ctrl</kbd> + <kbd>C</kbd>, then add a method to `Book` that derives a value from `PublishedYear`.

Replace the contents of `Types/Book.cs` with:

```csharp
namespace GettingStarted.Types;

public record Book(string Title, Author Author, int PublishedYear)
{
    public bool IsRecent()
        => PublishedYear >= 2010;
}
```

Restart the server:

```bash
dotnet run
```

Public methods on object types become resolver-backed fields. The `IsRecent()` method appears in the GraphQL schema as `isRecent`.

Refresh Nitro's schema information if needed, then query both new fields:

```graphql
{
  book {
    title
    publishedYear
    isRecent
    author {
      name
    }
  }
}
```

Expected response:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "publishedYear": 2019,
      "isRecent": true,
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

This computed field is part of the same `Book` object type. The difference is in how the value is provided:

| Field           | Backing C# member      | What happens                                 |
|-----------------|-----------------------|-----------------------------------------------|
| `publishedYear` | `PublishedYear` property | Hot Chocolate returns the stored value.       |
| `isRecent`      | `IsRecent()` method      | Hot Chocolate calls the method for the value. |

# Confirm what changed in the schema

You have modified the `Book` object type, not the root `book` query field.

The flow is:

```text
GetBook() returns Book -> Book fields are available in book { ... } -> selected fields appear in data.book
```

Keep this model in mind when adding fields:

- `Book` is a GraphQL [object type](/docs/hotchocolate/v16/building-a-schema/object-types).
- A field is a member that clients can request in a selection set.
- A property-backed field returns data from a public C# property.
- A resolver-backed field runs code to produce a value. Public C# methods are one way to define [resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).
- GraphQL field names are case-sensitive. Hot Chocolate uses .NET naming for C# members and camelCase names for GraphQL fields by default.
- Nitro may show outdated schema information after a rebuild. Refresh Nitro's schema or reload the browser page if a new field does not appear.

The [GraphQL specification](https://spec.graphql.org/) defines selection sets and response shapes. The key rule for this lesson: the server returns only the fields selected in the operation.

# Troubleshoot this field edit

If your results do not match the expected response, consult this table:

| Symptom                                         | What to check                                                                                 |
|-------------------------------------------------|----------------------------------------------------------------------------------------------|
| The project does not build after editing `Book`. | Update both `Types/Book.cs` and the `new Book(...)` call in `Types/Query.cs`. The record constructor and resolver call must match. |
| Nitro does not show `publishedYear` or `isRecent`.| Restart the server after editing C#, then refresh Nitro's schema or reload the browser page.  |
| The query says `Cannot query field`.             | Copy the field name from Nitro's schema view. Use `publishedYear` and `isRecent`, not `PublishedYear` or `IsRecent`. |
| The response does not include the new field.     | Add the field inside `book { ... }` and run the query again. The response only contains selected fields. |
| The port changed after restart.                  | Copy the latest `Now listening on:` URL from the terminal and open the matching `/graphql` endpoint. |

For setup and endpoint issues, see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Go to the next small schema edit

You have added one stored value and one computed value to the `Book` object type, restarted the server, refreshed Nitro, and confirmed both fields with a query.

Continue to [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument) to learn how to create a field that accepts client input.

For more details, see:

- [Object types](/docs/hotchocolate/v16/building-a-schema/object-types) for how C# members become GraphQL fields
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for methods, async work, dependency injection, and parent values
- [Get started](/docs/hotchocolate/v16/get-started) for the scaffolded project flow
