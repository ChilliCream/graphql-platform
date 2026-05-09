---
title: "Run your first query"
description: "Use Nitro to execute the scaffolded Hot Chocolate query, read the JSON response, change the C# model, refresh the schema, and query the new field."
---

This guide will help you turn your running server from [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) into a working GraphQL endpoint you can interact with.

By the end, you will have:

- Opened Nitro at your local `/graphql` endpoint
- Run the scaffolded `book` query
- Compared the query selection to the JSON response
- Added a new C# member to the starter model
- Refreshed the schema and queried the new field in Nitro

Before you begin, make sure your `GettingStarted` server is running. If it is not, open a terminal in the `GettingStarted` project directory and start it with:

```bash
dotnet run
```

Keep this terminal open while you use Nitro.

# Open Nitro for your running server

After running `dotnet run`, look for the listening URL in the terminal output. The port number may be different on your machine. You should see output like:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

To access the GraphQL endpoint, add `/graphql` to the URL:

```text
http://localhost:5095/graphql
```

Nitro is the browser-based GraphQL IDE served at this endpoint. You will use four main areas in Nitro:

| Nitro area | Purpose |
| --- | --- |
| Document or operation editor | Paste and edit your GraphQL query |
| Run action | Send the query to your server |
| Response area | View the JSON result |
| Schema status or schema view | Check which fields your server exposes |

If prompted to create a document, select **Create Document**. Make sure the HTTP endpoint matches your local `/graphql` URL, then confirm the setting.

At this point, check that:

- The `dotnet run` terminal is still active
- The browser URL ends with `/graphql`
- Nitro allows you to edit a GraphQL document
- Nitro shows the schema is available or can display schema information

If Nitro does not load or connects to a different URL, verify the server is running, copy the current listening URL, and open the correct `/graphql` endpoint. For more help, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Run the starter query

Paste the following query into Nitro's operation editor:

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

Click **Run**.

Nitro will send the query to your local Hot Chocolate server. The server executes the generated C# code and returns a GraphQL response.

You should see a response like this:

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

Check that the response includes a top-level `data` property with a `book` object inside.

If you see an `errors` property, read the error message. The most common cause is a typo in a field name, such as `Book` instead of `book` or `Title` instead of `title`. GraphQL field names are case-sensitive.

# Read the JSON result

The query specifies which fields the server should return. The fields inside the braces form a **selection set**:

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

The JSON response matches this structure:

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

Here is how the query selection maps to the JSON response:

| Query selection | JSON response path |
| --- | --- |
| `book` | `data.book` |
| `title` | `data.book.title` |
| `author` | `data.book.author` |
| `name` inside `author` | `data.book.author.name` |

GraphQL responses are always JSON. For successful queries, the data appears under `data`. If the server cannot validate or execute part of the operation, the response may include an `errors` property.

The [GraphQL specification](https://spec.graphql.org/) defines how operations, selection sets, and responses work. For this tutorial, the key rule is that clients select the fields they need, and the response mirrors that selection.

# Make a small schema change in C#

Next, update the C# model to expose an additional field in the schema.

Stop the running server with <kbd>Ctrl</kbd> + <kbd>C</kbd>. For this tutorial, it is best to rebuild and restart after making changes.

Open `Types/Book.cs` and add a `PublishedYear` value to the `Book` record:

```csharp
namespace GettingStarted.Types;

public record Book(string Title, Author Author, int PublishedYear);
```

Now update the resolver in `Types/Query.cs` to provide a value for the new field:

```csharp
namespace GettingStarted.Types;

[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"), 2019);
}
```

If your namespace is different because you used another project name, keep your existing namespace and update only the `Book` record and the `new Book(...)` call.

Restart the server:

```bash
dotnet run
```

Look for output like:

```text
Now listening on: http://localhost:5095
```

The port may change after a restart. If it does, copy the new listening URL and open the correct `/graphql` endpoint.

Hot Chocolate uses .NET naming conventions for C# and GraphQL naming conventions for the schema. The C# property `PublishedYear` will appear as the GraphQL field `publishedYear`.

# Run the query again with the new field

Return to Nitro. If Nitro still shows the old schema, refresh the schema in Nitro or reload the browser page.

Add `publishedYear` to your existing `book` selection:

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

You should now see a response like:

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

This confirms the full feedback loop:

1. You changed a C# type
2. You updated the resolver data
3. You restarted the ASP.NET Core server
4. Nitro loaded the updated schema
5. The GraphQL query selected the new field
6. The JSON response included the new value

# Troubleshoot the first query loop

Use this table to resolve common issues without leaving the tutorial:

| Symptom | What to check |
| --- | --- |
| Nitro does not load. | Make sure the server is running and the browser URL uses the current listening port plus `/graphql`. |
| Nitro opens but cannot connect. | Check that Nitro's HTTP endpoint matches the browser address with `/graphql`. |
| The query returns `Cannot query field`. | Check spelling and casing. Use `book`, `title`, `author`, `name`, and later `publishedYear`. |
| `publishedYear` does not autocomplete or validate. | Restart the server after editing C#, then refresh Nitro's schema or reload the browser page. |
| The project does not build after editing `Book`. | Update both `Types/Book.cs` and the `new Book(...)` call in `Types/Query.cs`. The record constructor and resolver call must match. |
| The response still does not include `publishedYear`. | Make sure your query includes `publishedYear` inside `book { ... }` and you are connected to the restarted server. |
| The port changed after restart. | Copy the latest `Now listening on:` URL from the terminal and open the correct `/graphql` endpoint. |

For more help, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Understand what you proved

You have completed the smallest end-to-end Hot Chocolate loop:

- The ASP.NET Core app exposes a GraphQL endpoint at `/graphql`
- Nitro sends GraphQL operations to that endpoint
- The schema tells Nitro and clients which fields are available
- The query selection controls the JSON response shape
- C# types and resolver code define the schema and the data clients can request

This is the core development loop for implementation-first GraphQL in Hot Chocolate. You change the .NET code that describes or resolves data, rebuild the server, refresh the schema in your client, and query the fields the schema exposes.

# Continue after your first query

Choose your next step:

- **Understand the generated project.** See [What happened in the generated project](/docs/hotchocolate/v16/get-started/what-just-happened) for details on `Program.cs`, starter types, and the first request flow.
- **Make the scaffold match your domain.** Continue to [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours).
- **Add GraphQL to an existing ASP.NET Core app.** See [Add Hot Chocolate to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app).
- **Learn schema concepts in more depth.** Start with [Building a schema](/docs/hotchocolate/v16/building-a-schema).
- **Learn resolver concepts.** Read [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers).
- **Learn broader GraphQL concepts.** Visit the [official GraphQL learning site](https://graphql.org/learn/).
- **Recover from a problem.** Open [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).
