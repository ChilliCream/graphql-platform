---
title: "What Happened"
description: "Connect the generated Hot Chocolate project files, schema, Nitro query, resolver execution, and JSON response after your first successful query."
---

You now have an ASP.NET Core application running with a Hot Chocolate GraphQL endpoint. Nitro opened at `/graphql`, sent a query to that endpoint, and displayed a JSON response containing `data.book`.

This page explains how the different parts you interacted with fit together:

```text
C# types and resolver -> GraphQL schema -> GraphQL query -> JSON response
```

Keep your generated `GettingStarted` project open as you read. The aim here is not to cover every Hot Chocolate detail, but to help you identify which file produced each part of the result, what Nitro handled, and where to look next.

# Use the generated project as a guide

The scaffolded project is a minimal ASP.NET Core app. Each file plays a specific role:

| File or output | What it contributes | How it appeared in the first query |
| --- | --- | --- |
| `Program.cs` | Sets up the web app, registers GraphQL services with `builder.AddGraphQL().AddTypes()`, maps the GraphQL endpoint using `app.MapGraphQL()`, and starts the app with GraphQL command support. | Made `/graphql` available for Nitro and other clients. |
| `Types/Query.cs` | Defines fields on the root GraphQL `Query` type. | `GetBook()` became the `book` field you selected. |
| `Types/Book.cs` | Defines the C# structure returned by `GetBook()`. | `Title` and `Author` became the selectable `title` and `author` fields. |
| `Types/Author.cs` | Defines the nested C# structure inside a `Book`. | `Name` became the nested `name` field. |
| `Properties/ModuleInfo.cs` | Marks the generated module used by the Hot Chocolate source generator. | Supports type discovery for the starter types. No need to edit it in the first flow. |
| `GettingStarted.csproj` | References Hot Chocolate packages and enables C# project settings such as nullable reference types. | Provided the packages and analyzers that built the server. |
| Build output under `obj/` | Contains generated code produced during build. | Wires annotated types into the schema. Treat this as build output, not source code to edit. |

The files you will edit most often are:

- `Types/Query.cs` to return different data or add new query fields.
- `Types/Book.cs` and `Types/Author.cs` to change the shape clients can select.
- `Program.cs` to add server features, middleware, or integrations.

Leave the generated build output alone. If changes to your code do not seem to update the schema, rebuild and restart the server, then refresh Nitro. If issues persist, see the [getting-started troubleshooting guide](/docs/hotchocolate/v16/get-started/troubleshooting).

# How C# code becomes a GraphQL schema

A GraphQL schema defines what clients can query. Hot Chocolate generates this schema from your C# code.

The main starter resolver is in `Types/Query.cs`:

```csharp
[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}
```

The `[QueryType]` attribute marks the class as contributing fields to the root `Query` type. The method name `GetBook` is transformed into the GraphQL field name `book`: Hot Chocolate removes the `Get` prefix and applies camel casing.

The returned records define the nested fields:

```csharp
public record Book(string Title, Author Author);

public record Author(string Name);
```

This C# structure maps to the following GraphQL schema:

```graphql
type Query {
  book: Book!
}

type Book {
  title: String!
  author: Author!
}

type Author {
  name: String!
}
```

The `!` indicates that a field is non-null in the GraphQL schema. With nullable reference types enabled, non-nullable C# members such as `string Title` become non-null GraphQL fields.

Here is how the mapping works from several perspectives:

| C# symbol | Schema field | Query selection | Response key |
| --- | --- | --- | --- |
| `Query.GetBook()` | `Query.book` | `book` | `"book"` |
| `Book.Title` | `Book.title` | `title` | `"title"` |
| `Book.Author` | `Book.author` | `author` | `"author"` |
| `Author.Name` | `Author.name` | `name` | `"name"` |

The schema acts as the boundary between server and client. Clients do not call `GetBook()` directly. Instead, they request fields from the schema, and Hot Chocolate maps those selections to resolver methods and object members.

To learn more about schema mapping, see [Building a schema](/docs/hotchocolate/v16/building-a-schema), [Queries](/docs/hotchocolate/v16/building-a-schema/queries), and [Object types](/docs/hotchocolate/v16/building-a-schema/object-types).

# Trace the query from Nitro to the resolver

The query you ran selected a `book`, then chose fields from the returned `Book` and its nested `Author`:

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

Here is what happens during the request:

| Step | What happens | Why it matters |
| --- | --- | --- |
| 1. Nitro sends the operation | Nitro sends an HTTP request to the `/graphql` endpoint mapped by `app.MapGraphQL()`. | The response came from your running app. Nitro was the client. |
| 2. Hot Chocolate parses the document | The GraphQL text is parsed into a GraphQL document. | Syntax issues are caught before resolver code runs. |
| 3. Hot Chocolate validates the document | The selected fields are checked against the schema. | A misspelled field, such as `titel`, fails validation because `Book` has `title`, not `titel`. |
| 4. Hot Chocolate executes `book` | The `book` field calls `GetBook()` and receives a `Book` instance. | This is where your starter resolver returned `"C# in depth."` and `"Jon Skeet"`. |
| 5. Hot Chocolate resolves child fields | The selected `title`, `author`, and `name` fields are read from the returned objects. | Nested fields are resolved because the client requested them. |
| 6. Hot Chocolate writes the response | The result is serialized as GraphQL JSON. | The JSON structure matches the query selection. |

The response you saw matched the fields you selected:

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

A successful GraphQL response includes a `data` entry. If validation fails or a resolver encounters a problem, the response can also contain an `errors` array. The [GraphQL specification](https://spec.graphql.org/) defines validation, execution, and response structure. The [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/draft/) describes the standard HTTP transport behavior for GraphQL clients and servers.

For more details on how Hot Chocolate executes requests, see [Execution engine](/docs/hotchocolate/v16/execution-engine). You do not need to understand request middleware for the first tutorial.

# What Nitro handled in the process

Nitro is the GraphQL IDE and client used in the starter flow. It allowed you to inspect and interact with your server, but it did not generate the schema or own the response data.

| Nitro area | What it means |
| --- | --- |
| Endpoint setting | The URL Nitro sends requests to, such as `http://localhost:5095/graphql`. This must match the URL printed by `dotnet run`, plus `/graphql`. |
| Request pane | The GraphQL document Nitro sends to the server. This document is not part of your C# project unless you save or copy it into your own files. |
| Schema view | A client-side view of the schema Nitro read from the running endpoint. If the schema is available, Nitro could inspect the server. |
| Response pane | The JSON returned by your Hot Chocolate server, including `data` and any `errors`. |

If something does not match your expectations, use this table to help diagnose the issue:

| Symptom | Likely area to check |
| --- | --- |
| Nitro cannot connect to `/graphql`. | Server startup, port, endpoint URL, browser access, or whether `app.MapGraphQL()` is present. |
| Nitro connects but the schema does not contain `book`. | `Types/Query.cs`, `[QueryType]`, build output, package version alignment, or schema refresh. |
| The query returns an unknown field error. | Field spelling, schema naming conventions, or stale schema in Nitro after a code change. |
| The response contains `data: null` or an `errors` array. | Resolver exceptions, nullability, or server-side failures. |

For step-by-step recovery, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Where to go next

Choose the next step that matches your goal:

| Your goal | Go here | Outcome |
| --- | --- | --- |
| Change the starter domain and keep the scaffold small | [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours) | Edit the starter types and query without leaving the first project. |
| Add one field, one argument, then preview real data | [Quick start](/docs/hotchocolate/v16/learn/1-quick-start) | Practice focused schema and resolver changes in short steps. |
| Build a larger server with checkpoints | [Tutorial: build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) | Move from the starter sample to data access, DataLoader, pagination, mutations, subscriptions, clients, tests, and production preparation. |
| Add Hot Chocolate to an app you already own | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) | Apply the same schema and endpoint model without the scaffolded template. |
| Understand schema design and resolver concepts | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) | Build a stronger mental model for operations, execution, nullability, clients, and schema evolution. |
| Configure hosting, packages, or deployment shape | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup) | Choose the right ASP.NET Core, worker, Azure Functions, Aspire, container, or reverse proxy setup. |
| Recover from a mismatch in the first flow | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) | Match the symptom to a fix and rerun the failed checkpoint. |

If you want the quickest next action, continue to [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours). For a complete overview of next steps, see [Next steps](/docs/hotchocolate/v16/get-started/next-steps).
