---
title: "You did it"
description: "Review the first Hot Chocolate GraphQL server you built and choose what to learn next."
---

Congratulations! You built your first Hot Chocolate GraphQL server.

Your `CatalogServer` project now exposes a small product catalog through GraphQL. You can run the server, open Nitro, inspect the schema, query products, page through results, and create a product with a mutation.

# What you built

This tutorial stayed focused on the first server experience. You built the core pieces of a GraphQL API without adding advanced data loading, realtime features, clients, tests, security, or production work.

| Step | What you added | Where you built it |
| --- | --- | --- |
| Project setup | Created the `CatalogServer` project, added Hot Chocolate, ran the server, and opened Nitro at `/graphql` | [Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/) |
| Schema types | Modeled the product catalog with `Product`, `Brand`, and the root query type | [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/) |
| Query resolvers | Added query fields that return products and look up a product by ID | [Write query resolvers](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/03-write-query-resolvers/) |
| Data service | Moved the in-memory catalog behind a small service registered with dependency injection | [Use a data service](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/04-use-a-data-service/) |
| Pagination | Added cursor pagination for browsing product results in stable pages | [Add pagination](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/05-add-pagination/) |
| Mutation | Added a create product mutation and verified the new product with a follow-up query | [Add mutations](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/06-add-mutations/) |

# Final checklist

Before you move on, run through this checklist:

- [ ] `dotnet run` starts the `CatalogServer` project without errors.
- [ ] Nitro opens from the `/graphql` endpoint.
- [ ] The schema contains the product catalog query and mutation fields.
- [ ] A products query returns data from the catalog service.
- [ ] A paged products query returns edges, nodes, and page information.
- [ ] The create product mutation returns a payload.
- [ ] A follow-up products query can include the product created by the mutation.

If one of these checks fails, return to the step that introduced that feature and compare your code with the page.

# What to learn next

The topics below are next steps. They were not part of this first tutorial, but they are common follow-up areas once the basic server is working.

| Goal | Start here |
| --- | --- |
| Let clients filter product lists | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) |
| Let clients sort results | [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/) |
| Batch related data fetching | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Connect the API to Entity Framework Core or another data source | [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/) and [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) |
| Add automated checks for schema and operations | [Testing](/docs/hotchocolate/v16/guides/testing/) and [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) |
| Call the API from an application | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Strawberry Shake](/docs/strawberryshake/v16/) |
| Send realtime updates to clients | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) |
| Add access control and request boundaries | [Securing your API](/docs/hotchocolate/v16/securing-your-api/) |
| Prepare a server for real traffic | [Performance](/docs/hotchocolate/v16/performance/) and [Warmup](/docs/hotchocolate/v16/server/warmup/) |

# Keep going

You now have a small, working GraphQL server that you can use as a reference while you learn more. Keep the project nearby, try one next-step topic at a time, and return to this checklist when you want to confirm that the tutorial baseline still works.
