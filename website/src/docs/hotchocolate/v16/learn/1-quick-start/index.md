---
title: "Quick Start"
description: "Choose a short Hot Chocolate lesson after your first successful query, then add one field, one argument, or one real-data preview with a clear checkpoint."
---

After running your first query with Hot Chocolate, you should have seen a response from your local `/graphql` endpoint. Nitro displayed your schema, and the result included `data.book`.

This quick start is designed to help you take the next step. It does not replace [Get started](/docs/hotchocolate/v16/get-started) or the full [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) tutorial. Instead, it offers focused lessons, each teaching a single concept through a single code change.

# Build on your first running server

Use these lessons to practice making safe, incremental changes before moving on to larger projects.

Each lesson follows a consistent process:

1. Modify one part of your C# code.
2. Build or restart the server.
3. Refresh Nitro's schema information if needed.
4. Run a query to confirm the change.
5. Compare the JSON response to the expected result.

The aim is not to cover every possible field, argument, or resolver here. Instead, you will learn how to select the next small lesson and recognize when you have succeeded.

# Make sure you are ready for these lessons

Begin here after you have completed a successful first query. If your server is not running yet, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) and [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).

You are ready to continue if:

| Check | What to confirm |
| --- | --- |
| Your project builds | `dotnet run` starts the ASP.NET Core app. |
| Your endpoint opens | You can open the local `/graphql` URL in a browser. |
| Nitro can read the schema | Nitro displays schema information for the running endpoint. |
| The starter query works | The response includes `data.book.title` and `data.book.author.name`. |
| Your source files are open | You can edit the starter files under `Types/`. |

These lessons use the scaffolded `GettingStarted` project unless otherwise noted. If you added Hot Chocolate to an existing ASP.NET Core app, you can still follow along by mapping the edits to your own query type and model files. For guidance on this path, see [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app).

If Nitro loads but the schema appears outdated after an edit, rebuild or restart the server and refresh Nitro's schema or reload the browser. For setup or endpoint issues, refer to [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Choose the smallest change for your goal

Select a lesson based on what you want the client to do next.

| Goal | Start here | You will change | You will verify |
| --- | --- | --- | --- |
| Allow the client to request an additional value | [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field) | Add a selectable value to the schema. | Nitro accepts the new field and the response includes a new JSON property. |
| Let the client shape or filter the result | [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument) | Add an input value to a field. | Changing the literal or variable value changes the result. |
| Use data beyond hard-coded samples | [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview) | Replace the starter data source in a limited, reversible way. | The same query shape returns values from the preview source. |
| Build a complete guided server | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) | Follow a larger project with cumulative chapters. | You progress through schema, data, clients, security, tests, and production checkpoints. |
| Learn the concepts before editing | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) | Read about schemas, operations, resolvers, execution, clients, nullability, and errors. | You can identify which concept applies to your next change. |

# Lesson 1: Add a field and verify the schema

Choose [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field) when the client needs to select one more value.

You will add a visible value to the starter model or resolver result, restart the server, refresh Nitro if needed, and request the new field in a query. You will know it worked when Nitro recognizes the new field and the JSON response includes the matching property.

This lesson introduces the core schema flow:

```text
C# member or resolver result -> GraphQL field -> query selection -> JSON response key
```

After completing this lesson, see [Object types](/docs/hotchocolate/v16/building-a-schema/object-types), [Queries](/docs/hotchocolate/v16/building-a-schema/queries), and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for more details.

# Lesson 2: Add an argument so clients can request specific results

Choose [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument) when returning the same value every time is no longer enough to demonstrate client flow.

You will add an argument to a field, first calling it with a literal value, then with a variable. Variables are important because real clients typically keep the operation text stable and pass changing values separately.

The checkpoint is clear: when you change the argument value, the result changes. If you omit a required argument or provide a value with the wrong shape, the GraphQL response gives validation feedback before you need to inspect resolver code.

After this lesson, see [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments), [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null), and [Execution engine](/docs/hotchocolate/v16/execution-engine) for more on inputs, validation, and execution.

# Lesson 3: Preview real data without a full data layer

Choose [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview) when the starter data shows the server works, but you want to see more realistic values.

This lesson acts as a bridge. It replaces the sample data in a controlled way, while keeping the same query-first verification loop. This is not a full production database pattern.

The checkpoint is that the same GraphQL query shape returns data from the preview source. Afterward, choose the deeper page that matches your real data plan:

| Need | Go here |
| --- | --- |
| Learn about resolver shape and dependencies | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) and [Dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection) |
| Connect to a database | [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases) |
| Prevent repeated nested data fetches | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| Add list features later | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections) |

If the preview works with a few rows but slows down after adding nested data, consider learning about DataLoader before building more data access code.

# Use the same verification loop in every lesson

Keep your feedback loop small so you can see which edit caused which result.

1. Edit the smallest unit: one member, one resolver parameter, or one data source boundary.
2. Build or restart the app.
3. Open the current `/graphql` endpoint.
4. Refresh Nitro's schema information if autocomplete or the schema view appears outdated.
5. Run the query that demonstrates the change.
6. Inspect both `data` and `errors`.
7. Summarize the result in one sentence before moving on.

For example: "The schema now exposes `publishedYear`, and the response includes `data.book.publishedYear`."

If you see a validation error, treat it as schema feedback first. Unknown field, missing argument, wrong casing, and variable type messages usually mean the query does not match the schema currently loaded by the server.

# When to move beyond the quick start

Move on from this path when your goal expands beyond a single safe edit.

| You want to | Go here |
| --- | --- |
| Build a complete server with continuity | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) |
| Strengthen your mental model | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) |
| Adapt setup for real hosting | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup) |
| Recover from a failed checkpoint | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| Look up exact schema behavior | [Building a schema](/docs/hotchocolate/v16/building-a-schema) |
| Look up resolver and data behavior | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data) |
| Review the generated project flow | [Get started](/docs/hotchocolate/v16/get-started) |

If you are unsure, follow the lessons in order:

1. [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field)
2. [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument)
3. [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview)

Then continue to the full [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) tutorial when you are ready to build a complete application.
