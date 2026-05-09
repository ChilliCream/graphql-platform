---
title: "At a Glance"
---

Curious what a Hot Chocolate API looks like before you start building? This page gives you a preview of the essentials, showing how a single read operation flows from C# code to a GraphQL response.

Hot Chocolate is a GraphQL server for .NET. You define C# types and resolver methods, and Hot Chocolate generates a strongly typed GraphQL schema. Clients then send GraphQL operations to request the data they need.

Here is the journey you will see:

```text
C# resolver -> GraphQL schema field -> Nitro query -> JSON response
```

This example uses the implementation-first approach featured throughout the v16 getting started guide. You do not need to run any commands on this page. When you are ready to try it yourself, begin with [Install and Scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold).

# Read a Field from a .NET Type

With implementation-first, your domain model remains plain C# code. A resolver method acts as the entry point for GraphQL queries.

```csharp
// Types/Author.cs
public record Author(string Name);

// Types/Book.cs
public record Book(string Title, Author Author);

// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]

// Types/Query.cs
[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
```

Key points in this setup:

| C# code | What Hot Chocolate does |
| --- | --- |
| `Book` and `Author` records | Expose object types with fields matching the public record members. |
| `[QueryType]` | Marks the class as part of the GraphQL `Query` root type. |
| `GetBook()` | Creates a query field named `book`. The `Get` prefix is removed and the name is camel-cased. |
| `builder.AddGraphQL().AddTypes()` | Registers the GraphQL server and the source-generated type registrations. |
| `app.MapGraphQL()` | Makes the GraphQL endpoint available, typically at `/graphql`. |

The `partial` keyword allows the source generator to add registration code at build time. You write the resolver as a standard C# method.

# The Schema Clients See

Clients interact with the GraphQL schema, not your C# classes directly. For the code above, Hot Chocolate exposes this schema:

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

The `Query` type is the entry point for reads. The `book` field comes from `GetBook()`. The `title`, `author`, and `name` fields are mapped from the record members.

GraphQL schemas are strongly typed contracts. Tools like [Nitro](/products/nitro) use introspection to help clients discover available fields. The [GraphQL specification](https://spec.graphql.org/) defines the language, type system, execution rules, and response format that Hot Chocolate follows.

For a deeper look at how C# maps to GraphQL, see [Building a Schema](/docs/hotchocolate/v16/building-a-schema). For now, remember: a resolver method on the query type creates a field that clients can request.

# Request Only the Data You Need

A GraphQL operation selects specific fields from the schema. Using Nitro or any GraphQL-compliant client, you can request the book title and the nested author name like this:

```graphql
query ReadBook {
  book {
    title
    author {
      name
    }
  }
}
```

The response matches the structure of your request:

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

The client only asked for `book.title` and `book.author.name`, so the JSON response includes those fields only in the same shape. The [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) describes how operations are sent over HTTP, while the GraphQL specification covers validation and execution.

To see how to run this query in Nitro, follow the walkthrough in [Run Your First Query](/docs/hotchocolate/v16/get-started/run-your-first-query).

# How Hot Chocolate Handles a Request

From the user's perspective, the request path is straightforward:

1. The client sends the `ReadBook` operation to the GraphQL endpoint.
2. Hot Chocolate parses the operation and checks it against the schema.
3. The `book` field triggers the `GetBook()` resolver, which returns a `Book` value.
4. The `title` and `author` fields access properties on that `Book`.
5. The `name` field accesses the property on the nested `Author`.
6. Hot Chocolate serializes the result as GraphQL JSON.

Your application logic runs in resolvers. In this example, that means `GetBook()` and the property accessors on `Book` and `Author`.

Query resolvers should not have side effects. Hot Chocolate may run sibling query fields in parallel, so a query field should only read data or compute a value. Use mutations for operations that change state. Learn more in [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) and [Queries](/docs/hotchocolate/v16/building-a-schema/queries).

# Next Steps for Real Applications

This sample keeps things focused. Real APIs add more features as requirements grow.

| When you need to... | Start here |
| --- | --- |
| Fetch from a database, REST API, or another service | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers), then [Fetching from Databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases) or [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest) |
| Avoid N+1 data access when many fields load related data | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| Add list fields clients can page through | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) |
| Let clients filter or sort lists | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) |
| Protect fields and operations | [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) |
| Accept only known operations from your own clients | [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) |
| See request behavior in production | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) |
| Add writes or real-time updates | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations) and [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) |

You do not need every feature on day one. Start with the schema and resolvers, then add capabilities as your needs evolve.

# Where to Go Next

Choose the path that fits your situation:

- **Check your machine first:** See [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites) before running commands.
- **Create a new Hot Chocolate project:** Go to [Install and Scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold).
- **Add Hot Chocolate to an existing ASP.NET Core app:** See [Add to Existing App](/docs/hotchocolate/v16/get-started/add-to-existing-app).
- **Run the query shown here:** Follow [Run Your First Query](/docs/hotchocolate/v16/get-started/run-your-first-query).
- **Want the mental model after your first success?** Return to the [Get Started overview](/docs/hotchocolate/v16/get-started) and pick the post-query explanation page.
- **Blocked by tooling, ports, packages, or Nitro?** Visit [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).
- **Looking for deeper GraphQL fundamentals?** Start with the [GraphQL introduction](https://graphql.org/learn/) and then see [Building a Schema](/docs/hotchocolate/v16/building-a-schema) for Hot Chocolate details.
