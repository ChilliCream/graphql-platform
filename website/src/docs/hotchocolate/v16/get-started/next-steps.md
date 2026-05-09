---
title: "Next steps"
description: "Choose where to go after your first Hot Chocolate server works."
---

Now that your Hot Chocolate server can answer a GraphQL request, your next step depends on what you want to accomplish. This page helps you find the right direction for your goals, without repeating setup instructions or code samples you have already seen.

# Where do you want to go next?

Use the table below to match your current goal with the recommended next page.

| Your goal | When to choose this | Go here |
| --- | --- | --- |
| Make another small change | You want to practice after your first working query and confirm each change as you go. | [Learn quick start](/docs/hotchocolate/v16/learn/1-quick-start) |
| Build a complete server | You want a step-by-step project that covers schema basics, data, clients, security, and production. | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) |
| Understand GraphQL concepts | You are new to GraphQL, coming from REST or controllers, or want to understand schemas, operations, execution, clients, nullability, errors, and data access. | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) |
| Add GraphQL to an existing app | You have an ASP.NET Core app and want to add `/graphql` alongside your current routes. | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) |
| Call the server from another process | Nitro is working, and now you need to send operations from an application, CLI tool, backend service, or generated client. | [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client) |
| Prepare for a production discussion | You need to review request limits, trusted documents, warmup, observability, smoke tests, and security before rollout. | [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production) |
| Recover or look up exact behavior | Something is not working, or you need details about endpoints, schema types, resolvers, data middleware, security, or transport. | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |

# Short, focused lessons: Learn quick start

Choose [Learn quick start](/docs/hotchocolate/v16/learn/1-quick-start) if you want to keep your feedback loop small. This path is ideal if you are still evaluating Hot Chocolate or want to practice with targeted changes before starting a larger tutorial. You will make one schema change at a time, run the server, and check the result with a query.

This approach is helpful when you want to:

- add a field and see how C# members appear in GraphQL
- add an argument and learn how callers pass values
- preview how a resolver can return data that matches your domain

If you have already finished [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours), the quick start lessons continue in the same style but with more structure.

# Build a complete server: Tutorial project

Choose [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server) if you want a guided project that covers more than isolated edits. This tutorial builds a realistic server over several chapters. You will define the schema, write resolvers, connect to data, call the server from clients, add security, test behavior, and prepare for production.

This path helps you see how all the parts fit together. The tutorial includes source code and checkpoints so you can compare your project to a known state if you need to get back on track.

# Deepen your understanding: GraphQL and Hot Chocolate concepts

Choose [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) if your setup works but the concepts are unfamiliar. This section is useful when you are asking questions like:

- Why use GraphQL on .NET instead of other endpoint styles?
- What is the difference between implementation-first and code-first?
- What does a GraphQL operation ask the server to do?
- How does execution affect resolver design and data access?
- Why do clients care about nullability, errors, and response shape?
- How should a schema model use cases without exposing implementation details?

These concept pages support design decisions and troubleshooting. You do not need to read them before your first installation, but they are helpful when a tutorial step raises a broader question.

For more background on the GraphQL language itself, see the [official GraphQL introduction](https://graphql.org/learn/).

# Add Hot Chocolate to an existing ASP.NET Core app

Choose the route below that matches the level of setup guidance you need:

| Need | Best route |
| --- | --- |
| First success in an app that already builds and runs | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) |
| More detail on middleware order, endpoint routing, authentication, controllers, Minimal APIs, or production settings | [Existing ASP.NET Core app setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app) |
| Package and version guidance | [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages) |
| Other hosting scenarios | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup) |

The Get Started page helps you reach a working `/graphql` endpoint with one query field. For planning how GraphQL fits into a real ASP.NET Core pipeline, the Learn setup page is the next recommended stop.

# Connect a client: When Nitro is not the final caller

Choose [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client) if your server works in Nitro and you need to call it from another process. This page explains what a GraphQL client sends over HTTP: the endpoint URL, operation text, variables, headers, and the response format with `data` and `errors`.

Use this as a bridge from "the server works" to "an application can use this API." For a deeper look at client contracts, see [client concepts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients). If you want to generate .NET client types, review [Strawberry Shake](/docs/strawberryshake/v16).

# Prepare for production

Choose [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production) when you need a checklist for team planning before rollout. This section introduces the topics to review before going live. It does not replace a deployment plan, security review, or platform-specific hosting guide.

Use this page to start discussions about:

- request limits and cost controls
- trusted documents or persisted operations
- server warmup before handling traffic
- observability and diagnostics
- deployment smoke tests
- authentication, authorization, and introspection policy

For more details on hardening your server, combine the production chapter with [Securing your API](/docs/hotchocolate/v16/securing-your-api), [Performance](/docs/hotchocolate/v16/performance), and [Server](/docs/hotchocolate/v16/server).

# Troubleshooting and reference

Choose [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) if you encounter a problem. Use this page when:

- the server will not start
- Nitro does not load
- `/graphql` returns an unexpected status
- a package restore or build fails
- the schema is missing a field you expected
- a client request reaches the server but returns an error

For exact behavior or configuration options, use these reference pages:

| Need | Reference |
| --- | --- |
| Endpoint paths, Nitro behavior, schema downloads, middleware mapping | [Endpoints](/docs/hotchocolate/v16/server/endpoints) |
| HTTP request and response rules | [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) |
| Queries, mutations, object types, arguments, nullability, scalars, schema modeling | [Building a schema](/docs/hotchocolate/v16/building-a-schema) |
| Resolvers, dependency injection, DataLoader, paging, filtering, sorting, projections, data access | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data) |
| Authentication, authorization, request limits, cost analysis, introspection | [Securing your API](/docs/hotchocolate/v16/securing-your-api) |
| Trusted documents, persisted operations, performance topics | [Performance](/docs/hotchocolate/v16/performance) |

# Not sure where to start?

If you are unsure which route to take, follow this order:

1. Practice with [Learn quick start](/docs/hotchocolate/v16/learn/1-quick-start).
2. Build the guided project in [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server).
3. Read [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql) if a concept is blocking your design.
4. Use [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup) when adapting the server to your hosting environment.
5. Review [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production) before rollout planning.

You can always skip ahead if your current work matches a more specific topic.

