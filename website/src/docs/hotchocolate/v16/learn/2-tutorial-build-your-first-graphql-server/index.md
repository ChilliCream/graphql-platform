---
title: "Build your first GraphQL server"
description: "Start the Hot Chocolate v16 tutorial by setting the scope, project goal, prerequisites, and page path."
---

This tutorial helps you build your first GraphQL server with Hot Chocolate v16.

You will create a small product catalog API named `CatalogServer`. The project starts as an ASP.NET Core app, exposes a GraphQL endpoint at `/graphql`, and uses Nitro to run operations while you build. Each page adds one small piece until the server can query products, page through the catalog, and create a product with a mutation.

This is a shallow tutorial for the first server experience. It does not try to teach every Hot Chocolate feature. When you finish, you can choose deeper reference pages for topics such as DataLoader, subscriptions, tests, clients, security, production readiness, distributed schemas, or schema layering.

# What you will build

You will build a GraphQL server for a small product catalog.

The domain stays intentionally small:

- `Product` has an `Id`, `Name`, `Description`, and a link to a brand.
- `Brand` has an `Id` and `Name`.
- Catalog data lives in memory behind a small service.
- The project uses implementation-first schema types with C# code.

By the end, your server will support:

- A `/graphql` endpoint.
- Nitro for exploring the schema and running operations.
- Query fields for products and product lookup.
- A data service registered with dependency injection.
- Cursor pagination over products.
- One mutation that creates a product.

# What this tutorial does not cover

The tutorial stops at mutations. It does not include database setup, DataLoader, subscriptions, automated tests, client applications, authentication, authorization, production configuration, distributed schemas, or schema architecture lessons.

Filtering and sorting are also not in the main path. Learn them after you have the first server running.

# Prerequisites

Before you start, make sure you have:

| Requirement | Check |
| --- | --- |
| .NET SDK | Install the [.NET 8 SDK](https://dotnet.microsoft.com/download) or later. Run `dotnet --info` to confirm the SDK is available. |
| Editor or terminal | Use Visual Studio, VS Code, Rider, or a terminal workflow that can edit C# and run `dotnet` commands. |
| NuGet access | Confirm your package sources can restore Hot Chocolate packages and templates. |
| Browser | Use a browser that can open local `localhost` URLs for Nitro. |
| C# basics | Be comfortable editing C# files, reading compiler errors, and running an ASP.NET Core app. |
| GraphQL basics | Know the words schema, query, field, resolver, selection set, response, and mutation at a high level. |

If you need setup help first, see [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/) or [Get Started](/docs/hotchocolate/v16/get-started/).

# The tutorial path

Follow the pages in order. Each page builds on the previous one.

| Page | What you add |
| --- | --- |
| [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) | Understand the scope, project goal, prerequisites, and path. |
| [1. Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/) | Create `CatalogServer`, run it, open `/graphql`, and use Nitro. |
| [2. Define your first schema types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) | Add the first `Product` and `Brand` types and expose a query root. |
| [3. Write query resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-query-resolvers/) | Return catalog data through query resolver methods. |
| [4. Use a data service](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-use-a-data-service/) | Move catalog data behind a small dependency-injected service. |
| [5. Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-add-pagination/) | Page through products with cursor pagination. |
| [6. Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-add-mutations/) | Add one create-product mutation and verify it with a follow-up query. |
| [You did it](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/you-did-it/) | Review what you built and choose where to go next. |

# How to use the pages

Work through one page at a time:

1. Read the goal for the page.
2. Make the described code changes.
3. Run or restart the server.
4. Open Nitro at `/graphql`.
5. Run the operation from the page.
6. Continue only after the result matches the page.

The tutorial is meant to stay small. If you want more detail while you work, use the focused reference pages for [Building a schema](/docs/hotchocolate/v16/building-a-schema/) and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/).

# Start the tutorial

Continue to [Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/).

At the end of the next page, you will have a running `CatalogServer` project, a reachable `/graphql` endpoint, and Nitro connected to your local server.
