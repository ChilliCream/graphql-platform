---
title: "You did it"
description: "Review the Hot Chocolate tutorial server you built, keep the finished project as a checkpoint, and choose your next learning path."
---

You have completed building a Hot Chocolate GraphQL server, starting from the first ASP.NET Core endpoint and progressing through schema design, data access, testing, security, and production readiness.

This page wraps up the tutorial and shows how to keep your finished project as a reference for future work.

# Your first Hot Chocolate GraphQL server

Your server now runs as an ASP.NET Core application with a GraphQL endpoint at `/graphql`. With Nitro, you can inspect the schema, run operations, and see both requests and responses as you explore.

The schema supports queries, mutations, and realtime updates for the tutorial's library domain. The project includes a basic regression test suite and an initial production checklist.

Before moving on, run the server one more time:

```bash
dotnet run
```

At this checkpoint, you should see:

- The server prints a local `Now listening on:` URL
- The `/graphql` endpoint opens in Nitro
- The final query, mutation, and subscription examples from the tutorial all work

This is now a working reference project. Keep it nearby as you build your own GraphQL APIs.

# What you built

The completed tutorial server covers the essential features of a practical GraphQL API.

| Capability | What it lets a client do | Where you built it |
| --- | --- | --- |
| Schema contract | Discover the library API through the GraphQL schema | [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) |
| Resolvers and data | Read real project data through fields such as `books` and `bookById` | [Write resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-resolvers/) and [Connect to real data](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-connect-to-real-data/) |
| Arguments and filtering | Shape results by passing values and filter input | [Add arguments and filters](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-add-arguments-and-filters/) |
| DataLoader | Batch related data loading during one request | [Fix N+1 with DataLoader](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-fix-n-plus-1-with-dataloader/) |
| Pagination | Browse larger lists through a connection shape and page information | [Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/07-add-pagination/) |
| Mutations | Change data through explicit input and payload shapes | [Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/08-add-mutations/) |
| Subscriptions | Receive selected realtime events from the server | [Add subscriptions](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/09-add-subscriptions/) |
| Tests | Catch schema and execution regressions before clients see them | [Test your server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/10-test-your-server/) |
| Client calls | Send GraphQL operations over HTTP and read `data` and `errors` | [Call from a client](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/11-call-from-a-client/) |
| Security and production preparation | Add the first access boundary, request limits, persisted operation posture, warmup, telemetry, and smoke checks | [Secure your API](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/12-secure-your-api/) and [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/) |

This foundation is ready to adapt to your own domain. You have a contract clients can understand, resolvers that produce field values, intentional data access, and verification to protect the API as it evolves.

# What you learned

You have practiced the core concepts behind Hot Chocolate and GraphQL on .NET:

- **Schema:** Design the contract first. The schema is what clients discover, discuss, and depend on. Continue with [Building a schema](/docs/hotchocolate/v16/building-a-schema/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).
- **Resolvers:** Every field needs a value. Resolvers connect selected fields to C# code, services, and data. Continue with [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/).
- **Data:** Batch and shape data intentionally. DataLoader, filtering, projections, and pagination help the server do the right amount of work for each operation. Continue with [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/).
- **Operations:** Clients request the shape they need. A GraphQL request carries an operation document and, when needed, variables. The response returns `data`, `errors`, and optional `extensions`. Continue with [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) and the [GraphQL specification](https://spec.graphql.org/October2021/).
- **Production:** Control access, cost, observability, and rollout before accepting real traffic. Continue with [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis), and [Warmup](/docs/hotchocolate/v16/server/warmup).

If you remember one thing, let it be this: GraphQL work starts with the client contract, then flows through execution, resolvers, data access, verification, and production controls.

# Keep this project as your reference

Keep your finished tutorial project as a reliable baseline.

Refer to it when you want to:

- Compare a new schema idea with a working Hot Chocolate setup
- Test how a query, mutation, subscription, or client request should look
- Bring the tutorial's verification rhythm into another project
- Recover after an experiment changes more than you planned

If you have access to the tutorial source repository or checkpoint list, bookmark the final checkpoint. To compare or restore your project later, use [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/). If a local edit breaks the project, start with [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/).

# Strengthen this project

When you want to keep improving this project, choose a path below:

| Choose this if... | Next move |
| --- | --- |
| Your schema will be shared with another team | Review names, descriptions, nullability, payload shapes, and deprecation. Start with [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [Non-null](/docs/hotchocolate/v16/building-a-schema/non-null/), [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation/), and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/). |
| Your API will query larger datasets | Revisit provider fit, projections, pagination, filtering, and DataLoader boundaries. Start with [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/). |
| Clients need live updates | Review topics, subscription fields, transport behavior, and production provider choices. Start with [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) and [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/). |
| You need confidence before changing code | Add operation tests for important read, write, security, and production guardrail paths. Start with [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/). |
| You want application code to call the API | Try a typed client workflow after you understand the request and response model. Start with [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and the [Strawberry Shake documentation](/docs/strawberryshake/v16/). |
| This API will move toward production | Revisit request budgets, persisted or trusted operations, warmup, observability, authentication, authorization, and deployment smoke tests. Start with [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/), [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), and [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents). |

Pick a row, make improvements, and then rerun the tests from the testing chapter.

# Choose your next path

Let your next goal guide your next step.

| I want to... | Go here |
| --- | --- |
| Understand what happened when a query ran | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) and [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) |
| Design a schema for my own domain | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) and [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |
| Handle nullability, errors, and schema changes with care | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/), and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| Connect a real database or service | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) |
| Make the API safer for production | [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/) and [Securing your API](/docs/hotchocolate/v16/securing-your-api/) |
| Host Hot Chocolate in a different .NET shape | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/), [ASP.NET Core](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/), and [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app/) |
| Call this API from an application | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/) |
| Move from REST, OData, another GraphQL library, or an earlier Hot Chocolate version | [Coming from](/docs/hotchocolate/v16/learn/5-coming-from/) |
| Keep building from the tutorial baseline | [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) and [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) |

# Keep the checklist

Use this checklist as your final tutorial health check.

| Check | Expected status |
| --- | --- |
| Run the server | The local GraphQL endpoint responds. |
| Inspect the schema | Nitro shows the expected library query, mutation, subscription, book, author, input, payload, and connection types. |
| Run representative operations | A query returns library data, `addBook` returns its payload, and `onBookAdded` receives the event when a book is added. |
| Run the tests | The schema snapshot and execution or HTTP tests pass. |
| Review production notes | Known gaps are recorded before the project moves beyond local learning. |
| Preserve the final state | You know where the final checkpoint or source code lives. |
| Pick the next path | You selected one learning link from this page. |

# If something is not working

Match your symptom to the table below and return to the recovery pages.

| Symptom | Start here |
| --- | --- |
| The server will not start | [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) and [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) |
| Nitro opens, but the schema differs from the tutorial | [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) and [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) |
| A query, mutation, or subscription result differs | [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) and the chapter that introduced the operation |
| Tests fail after final changes | [Test your server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/10-test-your-server/) and [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) |
| The production checklist feels incomplete | [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/) and [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) |
| A client cannot reach the endpoint | [Call from a client](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/11-call-from-a-client/), [HTTP Transport](/docs/hotchocolate/v16/server/http-transport/), and [Endpoints](/docs/hotchocolate/v16/server/endpoints/) |

After recovery, return to the checklist above. Once each item passes, use the project as your baseline and continue with your chosen next step.
