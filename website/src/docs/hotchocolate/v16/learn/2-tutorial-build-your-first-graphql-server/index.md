---
title: "Build your first GraphQL server"
description: "Start the full Hot Chocolate v16 tutorial with the project goal, prerequisites, chapter map, checkpoints, recovery route, and next step."
---

This tutorial guides you through building a complete Hot Chocolate server project. You will begin with a basic ASP.NET Core application, set up a GraphQL endpoint at `/graphql`, and use Nitro to inspect your schema. As you progress, you will expand the server step by step: designing the schema, writing resolvers, connecting to data sources, using DataLoader, adding pagination, implementing mutations and subscriptions, writing tests, making client calls, securing the API, and preparing for production.

The tutorial uses a library domain for its example. Books have authors, readers borrow books, and clients browse or search collections. The API evolves as client needs grow, providing a consistent story throughout the project.

This page serves as your starting point. It is not a replacement for the feature reference, migration notes, or production hardening guides. Use this page to decide if you want to follow the tutorial, check your prerequisites, understand the chapter sequence, and learn how to recover your progress if needed.

# What you will build

You will create a GraphQL server for a library application.

The project begins with:

- An ASP.NET Core app
- A Hot Chocolate GraphQL endpoint at `/graphql`
- Nitro available during development
- An initial `book` or `books` query
- C# types and resolver methods that define the GraphQL contract

From there, you will expand the project in clear steps:

- **Schema:** Add object types, fields, arguments, input object types, mutations, and subscriptions
- **Data:** Move from starter data to a real data source, then use DataLoader to batch relationship loading
- **Client shaping:** Add filtering, pagination, and operation patterns so clients can request the data they need
- **Writes and realtime behavior:** Add mutation operations and a subscription to demonstrate event publishing
- **Verification:** Add tests and checkpoints to compare the server state after each chapter
- **Readiness:** Review security, authorization, request safeguards, schema visibility, and production preparation

Your first milestone is concrete: the server runs, `/graphql` is available, Nitro can read the schema, and a query returns library data. Each later milestone is also tangible: a nested author field resolves, a filtered query returns fewer books, DataLoader reduces repeated data work, a mutation changes state, a subscription receives an event, and a test confirms expected behavior.

# Decide whether this is the right path

Choose this tutorial if you want a guided build that connects the main Hot Chocolate concepts into one project.

| Your goal | Best starting point |
| --- | --- |
| You want a complete project from setup through production preparation | Continue with this tutorial |
| You want the fastest route to a running server | Use [Get Started](/docs/hotchocolate/v16/get-started/) |
| You already ran your first query and want short practice lessons | Use [Quick Start](/docs/hotchocolate/v16/learn/1-quick-start/) |
| You know the exact feature you need | Jump to [Building a schema](/docs/hotchocolate/v16/building-a-schema/) or [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) |
| You are adding GraphQL to an existing ASP.NET Core app | Start with [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) |
| You are upgrading from an earlier Hot Chocolate version | Read the [migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) before changing production code |

Plan to work through the tutorial across several focused sessions. Each chapter has a visible checkpoint, so you can stop after a successful verification step and resume later.

# Check the prerequisites

Before you start chapter 1, confirm that the required setup works on the machine you will use.

| Requirement | Check |
| --- | --- |
| .NET SDK | Hot Chocolate v16 getting-started material requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) or later. Run `dotnet --info` and confirm an SDK version `8.0.x` or later appears. |
| Editor or CLI workflow | Use Visual Studio, VS Code, Rider, or a terminal workflow that can edit C#, restore packages, build, and run an ASP.NET Core app. |
| NuGet access | Run `dotnet nuget list source` and confirm your configured package source can provide Hot Chocolate packages and templates. |
| Browser access | Confirm your browser can open local `localhost` URLs. Nitro loads from the local GraphQL endpoint during development. |
| C# and ASP.NET Core basics | You should be comfortable editing C# files, reading compiler errors, running `dotnet build`, and starting an app with `dotnet run`. |
| GraphQL vocabulary | Know the words schema, query, field, resolver, selection set, response, mutation, and subscription at a high level. |

If a setup check fails, use [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/) and [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) before starting the tutorial.

Optional tools can help, but they are not required for the first chapter:

- Git, if you want to commit your work after each checkpoint or compare local files with a reference state.
- An HTTP client, if you want to send operations outside Nitro.
- Database tooling, when you reach the data access chapter and want to inspect the backing data source.

# Understand the project shape

Hot Chocolate maps C# code to GraphQL concepts. You will use that mapping throughout the tutorial.

```text
C# type or resolver method -> GraphQL schema field -> client operation -> JSON response
```

For example, a C# resolver method can expose a `books` field. A client selects `books { title author { name } }`. The server runs the resolvers needed for that selection and returns JSON with the same shape under `data`.

The tutorial keeps this mapping visible:

- You define C# types that become GraphQL object types.
- You add resolver methods that produce field values.
- You inspect the schema in Nitro after schema changes.
- You run an operation that proves the new field, argument, mutation, or subscription works.
- You compare the response with the chapter checkpoint before moving on.

When you want the deeper reference, use [Building a schema](/docs/hotchocolate/v16/building-a-schema/) for schema concepts and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) for resolver, DataLoader, and data access concepts.

# Follow the chapter map

Use this map to see what each chapter adds and where you can pause or leave for focused reference material.

| Chapter | Outcome | Hot Chocolate concept | Checkpoint |
| --- | --- | --- | --- |
| [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) | Learn where starter, chapter-end, and final states live | Recovery workflow | You know how to compare or resume from a known state |
| [1. Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/) | Create and run the tutorial server | ASP.NET Core hosting and `MapGraphQL` | `/graphql` opens and Nitro can connect |
| [2. Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) | Add the first library types and inspect the schema | Object types and the Query root | Nitro shows the fields you added |
| [3. Write resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-resolvers/) | Return library data through resolver methods | Resolver methods and selection-shaped execution | A query returns the expected book data |
| [4. Add arguments and filters](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-add-arguments-and-filters/) | Let clients ask for a specific subset of books | Arguments and filtering middleware | Changing an argument or filter changes the result |
| [5. Connect to real data](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-connect-to-real-data/) | Replace starter data with a realistic data source | Data access boundary and dependency injection | The same query shape returns stored data |
| [6. Fix N+1 with DataLoader](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-fix-n-plus-1-with-dataloader/) | Batch relationship loading for nested fields | DataLoader and request-scoped caching | Nested author or loan data resolves without repeated work |
| [7. Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/07-add-pagination/) | Page through a collection field | Cursor pagination and connection shape | A paged query returns connection data and page information |
| [8. Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/08-add-mutations/) | Add a write operation for the library domain | Mutation root, input types, payloads, and errors | A mutation changes state and a follow-up query proves it |
| [9. Add subscriptions](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/09-add-subscriptions/) | Publish and receive a realtime event | Subscription root and WebSocket transport | A subscription receives the event triggered by the project |
| [10. Test your server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/10-test-your-server/) | Add tests for important GraphQL behavior | Server execution in tests | Test output proves the expected operation result |
| [11. Call from a client](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/11-call-from-a-client/) | Send an operation from a consuming application or client flow | GraphQL over HTTP and client operation shape | The client receives `data` or useful `errors` from the server |
| [12. Secure your API](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/12-secure-your-api/) | Add the first security layer | Authentication, authorization, and field-aware access | Protected fields behave differently for allowed and disallowed users |
| [13. Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/) | Review readiness before the project leaves local development | Request limits, schema visibility, diagnostics, and deployment checks | You have a production preparation checklist for the tutorial server |
| [You did it](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/you-did-it/) | Review what you built and choose the next learning route | Learning summary | You know which reference area to use next |

Good pause points are after chapter 1, after chapter 3, after chapter 6, after chapter 10, and before the production chapter. Each pause point ends with something you can run or inspect.

# Use checkpoints before you get blocked

Open [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) before you start coding. That page is the tutorial's recovery map.

Use checkpoints for three jobs:

1. **Compare.** Check whether your local files match the expected state for the chapter.
2. **Recover.** Start again from the last successful chapter if local edits are hard to untangle.
3. **Resume.** Return to the chapter that matches your last verified output.

Record the last chapter where your output matched the docs. A useful note includes:

- chapter number and title,
- checkpoint name,
- command you ran,
- GraphQL operation you executed,
- expected and actual response shape,
- package versions if setup or restore is involved.

If you ask for help, include that information. It helps other people separate environment issues, project state issues, schema mismatches, and operation mistakes.

# Work through each chapter with the same rhythm

Use the same loop in every chapter:

1. Read the chapter goal and checkpoint before editing code.
2. Make the smallest described change.
3. Build or run the project.
4. Open the current `/graphql` endpoint.
5. Refresh Nitro's schema information when the schema changes.
6. Run the operation from the chapter.
7. Compare the response, schema view, terminal output, or test result with the checkpoint.
8. Commit, note, or bookmark the checkpoint before continuing.

Verification is part of the tutorial. Do not save it for the end. If the result differs, stop at that chapter, compare with the checkpoint, and use the help route below.

# Know what you will learn

By the end of the tutorial, you should be able to explain how a GraphQL operation reaches your C# code and returns data to a client.

You will practice these reusable ideas:

- The schema is the contract between the server and clients.
- Query fields expose read entry points.
- Resolver methods bridge GraphQL fields to C# data access.
- Arguments and filters let clients ask for a narrower result.
- DataLoader is the default pattern for batched relationship loading.
- Pagination protects collection fields and gives clients a stable result shape.
- Mutations use input and payload types for write operations and user-facing errors.
- Subscriptions use GraphQL for realtime events.
- Tests protect the operation shapes clients depend on.
- Security and production preparation are part of API design, not a final afterthought.

The tutorial introduces each idea at the moment you need it. When a concept becomes your main focus, continue with the canonical docs:

- [Queries](/docs/hotchocolate/v16/building-a-schema/queries/)
- [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/)
- [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/)
- [Input object types](/docs/hotchocolate/v16/building-a-schema/input-object-types/)
- [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/)
- [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/)
- [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/)
- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/)
- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/)
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/)
- [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/)
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/)
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/)
- [Testing guide](/docs/hotchocolate/v16/guides/testing/)

# Jump out if you already know what you need

You can leave the tutorial path when a focused reference page better matches your task.

| If you need to | Go to |
| --- | --- |
| Create a first server faster | [Get Started](/docs/hotchocolate/v16/get-started/) |
| Practice one small schema edit | [Quick Start](/docs/hotchocolate/v16/learn/1-quick-start/) |
| Add Hot Chocolate to an existing app | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) |
| Configure endpoints or understand `/graphql` | [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Define schema types | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) |
| Fetch data or inject services into resolvers | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) |
| Connect to a database | [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/) |
| Reduce repeated relationship fetches | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Add list features | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) |
| Add authentication or authorization | [Securing your API](/docs/hotchocolate/v16/securing-your-api/) |
| Prepare for operational concerns | [Server](/docs/hotchocolate/v16/server/), [Performance](/docs/hotchocolate/v16/performance/), and [Performance guide](/docs/hotchocolate/v16/guides/performance/) |

Return to the tutorial when you want the pieces to connect in one project again.

# Get help when something does not match

When a chapter result differs from the docs, identify the symptom before changing more code.

| Symptom | Likely cause | Next step |
| --- | --- | --- |
| `dotnet new` cannot find the GraphQL template | Template package is missing, package restore is blocked, or the SDK differs from the expected one | Use [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) |
| `dotnet restore` or `dotnet build` fails | NuGet source, package version, project directory, or copied code issue | Check the terminal output, then compare with the chapter checkpoint |
| Server starts on a different port | Local launch settings or available ports differ | Use the latest `Now listening on:` URL and append `/graphql` |
| `/graphql` returns 404 | Endpoint is not mapped, the URL is wrong, or the app did not restart | Confirm the server is running and check [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| Nitro loads but cannot connect | Nitro points at a stale URL, HTTPS trust failed, or the server stopped | Update the Nitro endpoint to the current `/graphql` URL |
| A field is missing in Nitro | The schema did not rebuild, the server still runs old code, or the C# naming does not map to the field name you queried | Rebuild, restart, refresh Nitro, and compare the schema with the chapter |
| The response contains `errors` | The operation does not match the current schema, validation failed, or resolver code returned an error | Read the error message and compare the operation with the checkpoint |
| Local files no longer match the chapter | Edits from multiple chapters are mixed | Use [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) to resume from the last successful state |

For tutorial-specific recovery, open [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/). Include the chapter, checkpoint, command output, package versions, endpoint URL, and GraphQL operation when you ask for help.

# Start the first chapter

Start with the checkpoint page, then move to the setup chapter:

1. Open [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) so you know how to recover.
2. Continue to [Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/).

At the end of chapter 1, you should have a running project, a reachable `/graphql` endpoint, and Nitro connected to the local server.
