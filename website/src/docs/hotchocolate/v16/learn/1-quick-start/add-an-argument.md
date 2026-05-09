---
title: "Add an argument"
description: "Add one argument to the starter book field, query it with a literal value, then pass the same value through a GraphQL variable."
---

The starter `book` field always returns the same book. In this lesson, you will update the field so the client can select which sample book to retrieve by providing an argument.

By the end of this lesson, you will:

- Add a C# resolver parameter
- See the corresponding GraphQL field argument
- Query the field using a literal value
- Query the field using a variable
- Learn how to handle common errors with missing or invalid values

Before you begin, make sure you have completed [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field). Your `Book` record should have a `PublishedYear` property, and your `Book` type should include the computed `IsRecent()` field from that lesson.

If your server is running, stop it with <kbd>Ctrl</kbd> + <kbd>C</kbd>. For a reliable experience, rebuild and restart the server after making C# changes.

# Add a parameter

Open `Types/Query.cs`. Replace the hard-coded `GetBook()` resolver with a version that accepts a `title` parameter and searches for a book in a small in-memory list:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    private static readonly Book[] s_books =
    [
        new Book("C# in depth.", new Author("Jon Skeet"), 2019),
        new Book("GraphQL in Action", new Author("Samer Buna"), 2021)
    ];

    public static Book? GetBook(string title)
        => s_books.FirstOrDefault(
            book => book.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
}
```

If your namespace is different because of your project name, keep your existing namespace and update only the resolver code.

Restart the server:

```bash
dotnet run
```

You should see output like:

```text
Now listening on: http://localhost:5095
```

The port may change after a restart. If it does, copy the new URL and open the corresponding `/graphql` endpoint.

Hot Chocolate automatically maps the C# resolver parameter to a GraphQL argument on the same field. The method remains `GetBook`, so the field is still `book`. The new `title` parameter appears as a `title` argument:

```graphql
type Query {
  book(title: String!): Book
}
```

The `String!` type means the argument is required. In the template project, nullable reference types are enabled, so `string` maps to `String!`. If you want to allow omitted input, use a nullable C# parameter like `string? title` and handle the fallback logic in your resolver.

This lesson uses a hand-written in-memory lookup so you can see how arguments flow. For production scenarios, see Hot Chocolate [filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) after this quick start.

# Query with a literal argument

Go back to Nitro. If autocomplete or the schema view still shows `book` without the `title` argument, refresh Nitro's schema information or reload the browser page.

Paste the following query into Nitro's operation editor:

```graphql
{
  book(title: "C# in depth.") {
    title
    publishedYear
    isRecent
    author {
      name
    }
  }
}
```

Click **Run**.

You should receive a response like:

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

Here, the argument is passed to the `book` field:

```graphql
book(title: "C# in depth.")
```

The selection set determines which fields appear in the JSON response:

```graphql
{
  title
  publishedYear
  isRecent
  author {
    name
  }
}
```

Try changing the literal value and running the query again:

```graphql
{
  book(title: "GraphQL in Action") {
    title
    publishedYear
    isRecent
    author {
      name
    }
  }
}
```

You should see:

```json
{
  "data": {
    "book": {
      "title": "GraphQL in Action",
      "publishedYear": 2021,
      "isRecent": true,
      "author": {
        "name": "Samer Buna"
      }
    }
  }
}
```

If changing the argument value changes `data.book`, your setup is working.

If you provide a valid string that does not match any sample data, the operation succeeds and the resolver returns `null`:

```graphql
{
  book(title: "Unknown book") {
    title
  }
}
```

Expected response:

```json
{
  "data": {
    "book": null
  }
}
```

This is not a validation error. The value `"Unknown book"` is valid input for the schema, but it does not match any sample data.

# Query with a variable

Literal values are helpful for learning, but real clients usually keep the operation text unchanged and send different values as variables.

Replace the previous query with this named operation:

```graphql
query GetBook($title: String!) {
  book(title: $title) {
    title
    publishedYear
    isRecent
    author {
      name
    }
  }
}
```

Open Nitro's variables panel and enter this JSON:

```json
{
  "title": "C# in depth."
}
```

Click **Run**.

You should see:

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

Now, change only the variables JSON:

```json
{
  "title": "GraphQL in Action"
}
```

Run the operation again. The operation text stays the same, but the response now returns the second sample book.

Here is how the pieces fit together:

| Piece | Example | What it does |
| --- | --- | --- |
| C# resolver parameter | `string title` | Receives the input value in your resolver |
| Schema argument | `book(title: String!)` | Declares that clients can pass `title` to `book` |
| Operation variable | `$title: String!` | Declares a runtime value for this operation |
| Field argument in the operation | `book(title: $title)` | Passes the variable value into the field argument |
| Variables JSON | `{ "title": "C# in depth." }` | Supplies the runtime value |

For a deeper explanation of operation names, variables, and selection sets, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations). The [GraphQL specification](https://spec.graphql.org/) describes the validation rules for variables and arguments.

# Handle missing or invalid values

Most argument mistakes are caught during GraphQL validation before your resolver runs. Use the following table to help diagnose and fix common issues:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| The response says the `title` argument is required. | The query called `book` without `title`. | Pass `title` or change the C# parameter to `string? title` and define omitted-value behavior. |
| The response says a variable is not defined, not used, or has the wrong type. | The variable declaration, field argument, and variables JSON do not use the same name and type. | Use `$title: String!`, `book(title: $title)`, and a JSON property named `"title"`. |
| The response says a `String!` value was expected. | The literal or JSON value has the wrong shape, or the variable declaration is nullable while the argument is non-null. | Pass a string value and match the variable declaration to the schema argument type. |
| The query succeeds but `book` is `null`. | The input is valid, but no sample book matches the title. | Use one of the sample titles or change your resolver fallback behavior. |
| Nitro does not show the `title` argument. | The server was not rebuilt or restarted, Nitro has stale schema information, or you edited a different method. | Restart the server, refresh Nitro's schema information or reload the page, and confirm you edited `Types/Query.cs`. |

For example, this query omits the required argument:

```graphql
{
  book {
    title
  }
}
```

Expected response shape:

```json
{
  "errors": [
    {
      "message": "The argument `title` is required."
    }
  ]
}
```

To fix this, provide the argument:

```graphql
{
  book(title: "C# in depth.") {
    title
  }
}
```

This variable declaration is invalid for the current schema:

```graphql
query GetBook($title: String) {
  book(title: $title) {
    title
  }
}
```

The schema requires `String!`, but `$title: String` allows `null`. Use the non-null variable type:

```graphql
query GetBook($title: String!) {
  book(title: $title) {
    title
  }
}
```

If your variables JSON uses the wrong property name, the required `$title` variable will have no value:

```json
{
  "bookTitle": "C# in depth."
}
```

Use the variable name from the operation, without the `$` prefix:

```json
{
  "title": "C# in depth."
}
```

# What to learn next

You have now added a resolver parameter, restarted the server, queried the new field argument with a literal value, and passed the same value through a variable.

Continue to [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview) if you want the starter data to come from a more realistic source.

Explore these pages for more details:

- [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments) for default values, argument renaming, ID arguments, or input object arguments
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for service parameters, parent values, async resolvers, or cancellation
- [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null) for details on C# nullable reference type mapping
- [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types) if you need several related inputs instead of a single scalar argument
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) if clients need to filter collections instead of selecting from a small sample list
- [Quick Start](/docs/hotchocolate/v16/learn/1-quick-start) to choose another lesson
