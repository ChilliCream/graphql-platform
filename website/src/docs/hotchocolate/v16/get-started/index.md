---
title: "Getting Started"
description: "Choose the right first path for building, running, or calling a Hot Chocolate GraphQL server."
---

Hot Chocolate is a GraphQL server for .NET. This page will help you select the best starting point: previewing a Hot Chocolate app, creating a new server, adding GraphQL to an existing ASP.NET Core app, or calling a running endpoint from a client.

# Find Your Starting Point

Use this page as a guide to the main entry routes. Each linked page contains step-by-step instructions, commands, and troubleshooting details. Choose the path that matches your goal.

If you are unsure where to begin, start with [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold). This route walks you through creating a running server, exposing a GraphQL endpoint, opening Nitro in your browser, and then [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query) to verify everything works.

# Choose Based on Your Goal

| Your goal | Plan for | Choose this when | First success looks like | Start here |
| --- | --- | --- | --- | --- |
| Preview the Hot Chocolate shape | 5 minutes | You want to see how a C# type becomes a schema field before committing to a full tutorial. | You can connect the C# type, generated schema, query, and JSON response in a small example. | [At a glance](/docs/hotchocolate/v16/get-started/at-a-glance) |
| Create a new GraphQL server | 15 to 20 minutes | You are new to Hot Chocolate, evaluating the framework, or want a guided first server with checkpoints. | A new ASP.NET Core app exposes `/graphql` in Nitro, then [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query) verifies the response. | [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) |
| Add GraphQL to your ASP.NET Core app | 10 to 15 minutes | You already have a web app and want to expose a GraphQL endpoint alongside existing routes. | Your current app maps `/graphql` and resolves at least one query field. | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) |
| Call a running GraphQL endpoint | 10 minutes | You already have a server URL and want to send an operation from a client. | An HTTP request returns a GraphQL response with `data` or `errors`. | [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client) |
| Recover from a setup problem | As needed | Install, restore, startup, endpoint, Nitro, schema, or client requests do not behave as expected. | You can match the symptom to a fix and return to your selected path. | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |

Time estimates are for planning. Your environment, network, and editor setup may affect them.

# Check Your Requirements

Before you begin, make sure your setup matches the requirements for your chosen route:

- You have a supported .NET SDK for Hot Chocolate v16. The guided server path assumes .NET 8 SDK or later.
- You can run `dotnet` commands, restore NuGet packages, and start an ASP.NET Core app.
- You can open a local browser URL so Nitro can load at the GraphQL endpoint.
- For the existing app route, you already have an ASP.NET Core project that starts successfully.
- For the client route, you already have a GraphQL server endpoint that you can reach from your machine or application.

If you are unsure about any of these, visit [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites) before starting. This page keeps setup checks separate from the main tutorials.

# What Does First Success Look Like?

No matter which route you take, your first success will have these elements:

1. An ASP.NET Core application is running.
2. A GraphQL endpoint is available, usually at `/graphql`.
3. The schema exposes at least one query field.
4. Nitro or another client sends a GraphQL operation to the endpoint.
5. The server returns a JSON GraphQL response.

A successful response contains a `data` property. If you see an `errors` property, it still means your request reached the server and received a GraphQL response. Use the error message to decide whether to retry a step or check [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

To understand the generated project, visit [the explanation page](/docs/hotchocolate/v16/get-started/what-just-happened) after you run your first query.

# Recommended Route for Most Users

If you do not have a specific reason to choose another route, start with [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold). This is the full first-success path and leads directly to [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query) for response verification.

This route is recommended because it covers the complete server loop:

- Create a Hot Chocolate project
- Run the ASP.NET Core app
- Open the `/graphql` endpoint in Nitro
- Execute a query on the next route page
- Confirm the response

Choose a different route if your needs are more specific. Use [At a glance](/docs/hotchocolate/v16/get-started/at-a-glance) for a quick evaluation, [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) if you already have an app, or [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client) if your server is already running.

# Troubleshooting: Get Unstuck Fast

Setup issues are common. If you run into problems, use the troubleshooting guide to get back on track.

| Symptom | Where to go |
| --- | --- |
| Template installation or project creation fails | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| `dotnet restore` fails or packages do not align | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| The server does not start, the port is in use, or HTTPS blocks the browser | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| `/graphql` returns 404 or Nitro does not load | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| The schema does not contain the field you expected | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| The app fails during schema creation or dependency injection | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |
| A client request fails because of URL, request body, CORS, authentication, or HTTP method issues | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) |

After fixing the issue, return to your chosen route and repeat the step that failed.

# Next Steps After Your First Query

Once you have a working query, decide what you want to do next:

- **Make the scaffold match your domain.** Continue with [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours).
- **Understand the generated project.** Read [the explanation page](/docs/hotchocolate/v16/get-started/what-just-happened) to see how the project and first query flow work.
- **Learn the core server path.** Visit [Learn Hot Chocolate](/docs/hotchocolate/v16/learn) for schema design, resolvers, data fetching, clients, testing, and production topics.
- **Build schema types directly.** Use [Building a schema](/docs/hotchocolate/v16/building-a-schema) for reference on queries, mutations, object types, arguments, nullability, and more.
- **Prepare a real API.** Review [Securing your API](/docs/hotchocolate/v16/securing-your-api), [Performance](/docs/hotchocolate/v16/performance), and [Server](/docs/hotchocolate/v16/server) as you move beyond local development.
- **Learn GraphQL concepts.** The [official GraphQL introduction](https://graphql.org/learn/) covers the language, type system, operations, and execution model that Hot Chocolate implements.
- **Upgrade from an older Hot Chocolate version.** Start with [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).
