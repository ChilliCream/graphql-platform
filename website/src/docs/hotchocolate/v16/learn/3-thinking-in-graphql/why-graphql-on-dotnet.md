---
title: "Why GraphQL on .NET"
description: "Decide whether GraphQL with Hot Chocolate is the right API shape for your .NET application."
---

# Why consider GraphQL for your .NET API?

When building APIs in .NET, you have many options for shaping your endpoints: REST, RPC, OData, or custom solutions. GraphQL offers a different approach. It provides a strongly typed schema where the server defines domain concepts and relationships, and each client operation specifies the fields it needs. This is especially valuable when different clients require different data shapes from the same domain.

For example, a product detail page might need product information, price, reviews, and recommendations. A mobile product card might only need the name, image, and price. An admin screen could require stock state, audit fields, and supplier data. GraphQL allows you to serve all these needs through a single schema, letting each operation select the relevant fields.

Hot Chocolate brings GraphQL to ASP.NET Core, integrating with .NET hosting, dependency injection, authentication, authorization, logging, configuration, and deployment. GraphQL becomes an endpoint within your application, not a separate system.

## When does GraphQL solve your API problem?

GraphQL shines when your main challenge is data shape. If your clients frequently request "the same thing, but with these extra fields" or "the same relationship, but not on this screen," GraphQL can reduce the need for constant coordination. The schema exposes capabilities once, and operations select the response shape as needed.

In GraphQL, the server owns the schema. Clients cannot query arbitrary tables or objects; they select only the fields and arguments defined in the schema, and responses match the operation's structure.

```graphql
query ProductCard {
  productById(id: 1) {
    name
    imageUrl
    price {
      amount
      currency
    }
  }
}
```

The response mirrors the selection:

```json
{
  "data": {
    "productById": {
      "name": "Road bike",
      "imageUrl": "https://example.com/bike.png",
      "price": {
        "amount": 1299,
        "currency": "USD"
      }
    }
  }
}
```

The same schema can support a different operation for a product detail page:

```graphql
query ProductDetail {
  productById(id: 1) {
    name
    price {
      amount
      currency
    }
    reviews(first: 3) {
      nodes {
        rating
        summary
      }
    }
    recommendations(first: 4) {
      nodes {
        name
      }
    }
  }
}
```

Both operations use the same typed contract. There is no need for separate "card" or "detail" endpoints. Each is a selection over the same graph.

For more on GraphQL fundamentals, see the [GraphQL specification](https://spec.graphql.org/) and [GraphQL Learn](https://graphql.org/learn/).

## What problem does GraphQL address?

GraphQL separates three decisions that often get mixed together in endpoint design:

| Decision | Who decides it | Example |
| --- | --- | --- |
| What the API can do | The server schema | `Product.price`, `Product.reviews`, `Review.rating` |
| What this client needs now | The operation | Select `name`, `imageUrl`, and `price` for a card |
| How data is fetched | The server implementation | Resolver calls EF Core, a REST API, a domain service, or another source |

This separation is important because client needs change more quickly than domain capabilities. A field like `Product.price` has a single meaning in the schema, but many operations can select it in different contexts.

Arguments allow the schema to expose targeted access without creating a new route for every variation:

```graphql
query BookWithReviews {
  bookById(id: 42) {
    title
    author {
      name
    }
    reviews(first: 5, order: NEWEST) {
      nodes {
        summary
      }
    }
  }
}
```

The schema is the key boundary. Clients can select `reviews` only if the schema exposes it, and can pass `first` and `order` only if those arguments exist. Authorization, cost limits, pagination, and resolver logic remain server responsibilities.

Schema design is API design. If the schema mirrors database tables without product review, it can become a generic data access API. A well-designed GraphQL schema uses domain language, exposes the right fields, and treats every field as a public contract decision.

The schema coordinate makes the contract explicit. For example, `User.address` always refers to the `address` field on the `User` type, with one documented meaning regardless of which operation selects it.

## Comparing GraphQL and REST

REST is not obsolete, and GraphQL is not a universal replacement. Each optimizes for different API needs.

| If your situation is... | GraphQL may fit when... | REST, RPC, or another API shape may fit when... |
| --- | --- | --- |
| Many clients need different projections | Clients select different fields from the same domain concepts. | One response shape serves all clients well. |
| UI screens combine related data | Nested selections reduce endpoint variants and client orchestration. | Separate resource calls are acceptable and cache well. |
| You expose a public API | Introspection, documentation, validation, pagination, authorization, and cost controls are part of the contract. | You want resource URLs, status semantics, and infrastructure patterns that consumers already know. |
| You control first-party clients | Trusted documents can make runtime operations known and auditable. | A small set of endpoints already matches the app workflow. |
| HTTP caching is central | Persisted GET operations, response caching, normalized client caches, and data-layer caches fit the use case. | Existing CDN and resource-URL cache rules are the primary requirement. |
| A system sends one command payload | GraphQL adds little value if there is one stable exchange. | REST, gRPC, messaging, or a command endpoint can be clearer. |

GraphQL supports caching, but the cache key is often different from a resource URL. Depending on your system, the cache key might be the operation document plus variables, a persisted operation identifier, normalized entity IDs, a server response cache, or data-layer cache entries behind resolvers.

Instead of asking "Can GraphQL be cached?", consider which layer owns the cache, which operations are stable, how invalidation works, and whether the cache key fits your infrastructure.

For Hot Chocolate features related to these decisions, see [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents), [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations), [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits), and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).

## Why is GraphQL often simpler to model?

GraphQL can make modeling relationships more straightforward by turning them into fields rather than endpoint variants.

Consider a user address. In REST, you might discuss several options:

| REST design choice | Question it raises |
| --- | --- |
| `GET /users/{id}/address` | Is address a separate resource for every client? |
| `GET /users/{id}?includeAddress=true` | Which include flags are supported, and how do they combine? |
| `GET /users/{id}?include=address,reviews` | Which relationships can be expanded together? |
| Sparse fieldsets | Which field selection syntax do clients use? |
| Always embed the address | Which clients pay for data they do not need? |
| Screen-specific projection endpoint | How many view-shaped endpoints will exist over time? |

With GraphQL, the operation defines the shape:

```graphql
query UserCard {
  userById(id: 1) {
    name
    address {
      city
      country
    }
  }
}
```

The schema decision is whether `User` should expose an `address` field, what type it returns, and who can select it. The operation decision is which address fields this client needs.

Not every relationship belongs in the schema. Each field is a contract and needs a clear name, ownership, authorization, nullability, pagination (if it returns a list), and manageable data access.

For more, see [modeling entities versus operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations), [connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data), and [schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution).

## Why does GraphQL fit many .NET APIs?

.NET teams are used to strong types, dependency injection, middleware, configuration, logging, and explicit hosting. GraphQL maps well to these patterns.

| .NET concept | GraphQL concept | Hot Chocolate role |
| --- | --- | --- |
| ASP.NET Core app | GraphQL endpoint | `MapGraphQL()` maps the endpoint beside other routes. |
| C# type | GraphQL type | Hot Chocolate builds the schema from your model and configuration. |
| C# method | Field resolver | Resolver methods provide values for fields. |
| C# parameter | GraphQL argument or injected service | Hot Chocolate binds request arguments and services. |
| DI service | Data or domain dependency | Resolvers call application services, repositories, EF Core queries, REST clients, or domain logic. |
| ASP.NET Core auth | Field or endpoint security | Hot Chocolate integrates with authentication and authorization patterns. |

In v16, implementation-first development lets you define schema entry points close to your C# code. Code-first configuration is available when you need more control. The [implementation-first versus code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first) guide can help you choose the right style.

Be careful not to expose storage entities without review. A GraphQL type is an API contract, even if it starts from a C# model. Use domain names, document fields, and keep implementation details out of the schema.

For data access, see [resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers), [dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader), [fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases), and [fetching from REST APIs](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest).

## How does Hot Chocolate fit into ASP.NET Core?

Hot Chocolate is a GraphQL server framework that runs inside your .NET application. You register GraphQL services, map a GraphQL endpoint, and keep the rest of the ASP.NET Core host model.

A typical request flow:

1. ASP.NET Core receives a request at the GraphQL endpoint.
2. Hot Chocolate parses and validates the operation against the schema.
3. The execution engine calls the resolvers for the selected fields.
4. Resolver methods use your services and data sources.
5. Hot Chocolate returns a GraphQL result over the configured transport.

The GraphQL endpoint can coexist with controllers, Minimal APIs, Razor Pages, health checks, static files, and other middleware. For more on the host model, see [ASP.NET Core fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/).

By default, `MapGraphQL()` exposes the server at `/graphql` and can also serve Nitro during local development. Nitro helps you explore the schema, author operations, run requests, and inspect responses before writing client code. See [endpoints](/docs/hotchocolate/v16/server/endpoints), [add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app), and [Nitro](/docs/nitro).

## Choosing between public, private, and internal GraphQL APIs

Your client ownership model affects the controls you need.

| API shape | Use it when... | Plan for... |
| --- | --- | --- |
| Public GraphQL API | Unknown or third-party clients compose operations against a published schema. | Documentation, schema review, pagination, authorization, cost analysis, request limits, introspection policy, and careful evolution. |
| Private or first-party GraphQL API | Web, mobile, desktop, or internal clients are owned by your organization. | Trusted documents, client registry workflows, known-operation review, client caching, and deployment coordination. |
| Internal team API | Multiple teams or services need a shared graph inside an organization. | Ownership boundaries, authorization, observability, schema conventions, and operational support. |

Public APIs require stronger defenses because clients can compose selections you did not anticipate. Private APIs can often make operations known at build time and reject unknown documents at runtime.

Many teams benefit from a controlled set of clients with different needs, such as web, mobile, admin, and partner portals selecting different shapes from the same domain graph.

See the [public API guide](/docs/hotchocolate/v16/guides/public-api) or [private API guide](/docs/hotchocolate/v16/guides/private-api) after you identify your API shape.

## When is GraphQL not the right fit?

GraphQL introduces a schema layer, operation validation, resolver design, and runtime controls. This investment pays off when you have shape, relationship, or contract challenges. It is not necessary for every endpoint.

| GraphQL may be a poor fit when... | Consider instead... |
| --- | --- |
| One consumer needs one stable request and response shape. | Keep the existing REST, RPC, or Minimal API endpoint. |
| The payload is mainly file upload or download content. | Use a purpose-built upload or download endpoint. |
| A small CRUD back-office tool has no endpoint sprawl or client variation. | Keep a direct CRUD API if it serves the team well. |
| You need bulk exports, warehouse feeds, reporting extracts, or analytics synchronization. | Use background jobs, file exports, streaming pipelines, or export APIs. |
| Existing infrastructure depends on resource URLs, HTTP status semantics, and CDN cache rules. | REST may be the clearer fit, although GraphQL can still be cached through other keys and layers. |
| The system is event ingestion, webhooks, or command streams. | Use messaging, webhooks, gRPC, or command endpoints. |
| The team cannot yet own schema design, authorization, pagination, and operation controls. | Start with a smaller spike or keep the current API until ownership is clear. |

Mixed architectures are common. A system might use GraphQL for client-shaped read models, REST for file downloads, gRPC for service-to-service calls, queues for async work, and webhooks for events.

## How to evaluate GraphQL for your project

Do not start by migrating every endpoint. Instead, pick one representative client problem.

Use this worksheet:

| Question | Write down |
| --- | --- |
| What current pain are you testing? | Multiple calls, overfetching, endpoint variants, client coordination, or unclear contract. |
| Which client or screen proves the idea? | One real web, mobile, admin, partner, or internal workflow. |
| What is the smallest schema slice? | The types, fields, arguments, and relationships needed for that workflow. |
| Which data sources are involved? | EF Core, REST APIs, services, repositories, search, or other systems. |
| Which API shape is it? | Public, private, first-party, partner, or internal. |
| What cache strategy applies? | Operation plus variables, persisted operation ID, normalized client cache, response cache, or data-layer cache. |
| What makes the spike successful? | A representative operation runs in Nitro or another client, returns the selected shape, has a clear authorization boundary, and shows predictable data access. |

Keep the first spike focused. You do not need full production hardening, complete schema coverage, or a migration plan for every endpoint to learn whether GraphQL fits your needs.

If the spike shows that a domain-shaped schema can serve multiple client shapes with less coordination, continue. If it only recreates a fixed endpoint with extra complexity, your current API shape may be better.

## Next steps

Choose your next page based on your current decision:

| If your decision is... | Go next | You will learn |
| --- | --- | --- |
| "I want a first running server." | [Get started](/docs/hotchocolate/v16/get-started/) | Create a Hot Chocolate server and run a first query. |
| "I already have an ASP.NET Core app." | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) | Map `/graphql` beside existing routes. |
| "I want a guided project." | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) | Build queries, mutations, subscriptions, data access, tests, and hardening step by step. |
| "I am translating from REST, OData, Apollo Server, or GraphQL.NET." | [Coming from another API or GraphQL server](/docs/hotchocolate/v16/learn/5-coming-from/) | Start with the overview, then use the [REST controllers](/docs/hotchocolate/v16/learn/5-coming-from/rest-controllers) or [GraphQL.NET](/docs/hotchocolate/v16/learn/5-coming-from/graphql-dotnet) path when it matches your stack. |
| "I need to choose a schema style." | [Implementation-first versus code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first) | Decide how C# code and schema configuration should relate. |
| "I need the execution mental model." | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes) | Understand validation, resolvers, results, and data fetching at a higher level. |
| "I am evaluating a public API." | [Public API guide](/docs/hotchocolate/v16/guides/public-api) | Plan schema documentation, cost controls, introspection, pagination, and security. |
| "I control the clients." | [Private API guide](/docs/hotchocolate/v16/guides/private-api) | Use trusted documents and known-operation workflows. |
| "I need data access depth." | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) | Connect fields to databases, services, DataLoader, filtering, sorting, projections, and pagination. |
| "I am planning production controls." | [Securing your API](/docs/hotchocolate/v16/securing-your-api/) and [performance](/docs/hotchocolate/v16/performance/) | Configure authentication, authorization, request limits, cost analysis, trusted documents, caching, and operational behavior. |

GraphQL is a strong fit when it lets your clients request related data in a way that preserves a typed, server-owned contract. Hot Chocolate is the .NET server framework that helps you build that contract within ASP.NET Core.
