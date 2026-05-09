---
title: "Coming from another stack"
description: "Choose the Hot Chocolate v16 starting point for REST controllers, EF-backed REST APIs, OData, Apollo Server, GraphQL.NET, or earlier Hot Chocolate experience."
---

Your API experience is valuable when you move to Hot Chocolate. This page helps you bridge your current mental model to Hot Chocolate v16. For each background, you'll find a translation of familiar concepts into GraphQL and Hot Chocolate terms, along with links to the canonical documentation for implementation.

Think of this page as a navigation guide. It is not a full learning path, feature reference, or version migration manual.

# Start from What You Know

Find the row that matches your first migration question. If your app combines several concerns, like REST controllers and EF Core data access, begin with the area that carries the most risk for your first migration slice.

| You are coming from | Start here | What you need to translate | Next success signal |
| --- | --- | --- | --- |
| ASP.NET Core controllers | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) | Map controllers, actions, routes, DTOs, model binding, status codes, and middleware to schema fields, operations, inputs, payloads, errors, and endpoint setup | You can describe how a controller action becomes a GraphQL field or mutation |
| EF-backed REST APIs | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) | Map entity shapes, relationships, includes, projections, paging, and N+1 risks to a client-facing schema and resolver data plan | You can separate the public GraphQL type from the database model |
| OData | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) | Map `$select`, `$filter`, `$orderby`, paging, and query shaping to GraphQL selection sets and Hot Chocolate data middleware | You can identify which query capabilities belong in the schema and which in server policy |
| Apollo Server | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) | Map resolver maps, context, plugins, schema conventions, and federation expectations to ASP.NET Core, dependency injection, Hot Chocolate resolvers, and server hooks | You can distinguish which GraphQL concepts carry over and which runtime conventions change |
| GraphQL.NET | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) | Map .NET GraphQL schema construction, resolver signatures, dependency injection, execution, and tests to Hot Chocolate conventions | You can plan a schema and resolver migration for a bounded area |
| Earlier Hot Chocolate versions | [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) | Map your existing Hot Chocolate habits, package alignment, schema checks, operation validation, and migration notes to a safe v16 upgrade plan | You can capture the current schema, align packages, and run representative operations before completing the migration |

Checkpoint: Leave this section with one clear next link, not a list of options. Return here if your next migration slice touches a different background.

# Translate Concepts Before Code

Migration is safer when you translate concepts first. Hot Chocolate is not a mechanical rewrite of routes, tables, resolver maps, or older framework APIs.

| Familiar concept | Hot Chocolate concept to learn | Start here | Canonical docs |
| --- | --- | --- | --- |
| REST route or controller action | A field on `Query`, `Mutation`, or `Subscription` in the schema contract | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) and [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |
| HTTP response and status-code thinking | GraphQL response shape with `data`, `errors`, nullability, and transport behavior | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| EF entity or table | A client-facing GraphQL type, field, or input based on domain language | [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| OData query options | Selection sets plus explicit filtering, sorting, paging, and projection rules | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) | [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) |
| Apollo context | ASP.NET Core services, scoped dependencies, request data, and resolver parameters | [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) |
| GraphQL.NET type definitions | Hot Chocolate schema definition style and binding conventions | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) |

Watch out for these common pitfalls:

- A REST route does not always map to a single GraphQL field.
- An EF entity does not automatically become a public GraphQL type.
- OData query options are not the same as GraphQL selection sets.
- Apollo context is not a direct replacement for .NET dependency injection.
- GraphQL.NET type definitions may not map one-to-one to Hot Chocolate configuration.

The core GraphQL concepts remain useful. The [GraphQL specification](https://spec.graphql.org/October2021/) defines schemas, operations, selection sets, variables, validation, execution, and response shape. Hot Chocolate brings these concepts to .NET with ASP.NET Core hosting, dependency injection, schema tooling, resolvers, data middleware, and production guardrails.

# Coming from REST: Choose Your Migration Path

When migrating from REST, you usually face three main decisions. Focus on the one that matches your first migration slice.

| If your first concern is | Start here | What to translate |
| --- | --- | --- |
| Replacing or supplementing controller actions | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) | How controllers, actions, routes, request DTOs, response DTOs, status codes, and authorization habits map to schema fields, operations, inputs, payloads, errors, and endpoint setup |
| Exposing data from an EF-backed REST API | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) | How to decide which domain shapes belong in the schema, where projections help, and when relationship loading needs DataLoader |
| Preserving flexible query behavior | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) | How `$select`, `$filter`, `$orderby`, paging, and query shaping compare to selection sets and Hot Chocolate filtering, sorting, paging, and projections |

You can run REST and GraphQL side by side in the same ASP.NET Core app while clients transition. Use [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) for setup guidance when you need both endpoints.

Avoid copying every route or table name into your schema. Instead, start with a single client task, use the domain language for that task, and design the response shape the client should select.

Checkpoint: You know which REST concern to tackle first.

# Coming from Another GraphQL Server: Focus on Conventions

If you already use a GraphQL server, you do not need to relearn the fundamentals. Schemas, types, fields, resolvers, operations, variables, validation, execution, nullability, and errors are the shared foundation.

Your migration is about adapting to new server conventions:

| Your runtime | Start here | Migration focus | Success signal |
| --- | --- | --- | --- |
| GraphQL.NET in .NET | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) | Schema construction styles, resolver signatures, dependency injection, execution, and testing | You understand how a type area and its resolvers would move into Hot Chocolate |
| Apollo Server or Node.js GraphQL | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) | Resolver maps versus resolver methods and type extensions, context versus .NET dependency injection, plugin concepts versus Hot Chocolate hooks, and ASP.NET Core hosting | You know which concepts carry over and which conventions must change |

Use [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) if you want to refresh core concepts. Use [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) for questions about hosting, packages, endpoint mapping, Nitro, or local development.

If your Apollo Server migration involves a distributed graph or federation, treat that as a separate design track. Start with resolver and hosting conventions, then use the [Fusion](/docs/fusion/v16) or Hot Chocolate distributed graph docs when you reach that stage.

# Upgrading from Earlier Hot Chocolate Versions: Plan Before You Change Packages

If you are upgrading from Hot Chocolate v15 or earlier, your existing mental model is still valuable. Schema design, resolvers, dependency injection, operations, authorization, DataLoader, tests, and production checks remain the right categories to focus on.

Treat version upgrades with extra care:

- Read [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) for the recommended migration path.
- Use migration notes and release notes to identify breaking changes from your current version.
- Keep all `HotChocolate.*` packages aligned.
- Capture the schema shape before and after the upgrade.
- Run representative client operations before and after the change.
- Compare error handling, authorization, data loading, and transport behavior before declaring the upgrade complete.

Checkpoint: You are ready to validate the schema and key operations, not only update package references.

# Use a Safe Migration Loop for Any Starting Point

The safest migrations are staged and verifiable. Follow this loop before diving into reference docs or production code:

1. Pick one entry point, operation, or client task.
2. Choose the docs area that matches your main risk.
3. Identify the client contract you want to preserve or improve.
4. Map your familiar concepts to Hot Chocolate concepts.
5. Define the desired GraphQL contract in domain language.
6. Build or upgrade that slice.
7. Compare schema shape, response shape, error handling, authorization, and data-access behavior.
8. Add tests, schema snapshots, or operation checks that match the migration risk.
9. Keep old and new entry points side by side if consumers need time to move.
10. Decide whether to migrate the next slice or adjust your design.

This approach helps you avoid one-to-one rewrites that move code without improving the contract. Validation keeps your migration safe.

For more on testing, see [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) and the [Testing guide](/docs/hotchocolate/v16/guides/testing/).

# Go Deeper Only When the Next Task Requires It

Move on from this gateway once you know your next implementation step. Use the canonical docs for details.

## If you need to model the schema

- See [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) for the concept map.
- Use [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) to review your public contract.
- Read [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) to choose a schema definition style.
- Reference [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [Queries](/docs/hotchocolate/v16/building-a-schema/queries/), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/), [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/), and [Input object types](/docs/hotchocolate/v16/building-a-schema/input-object-types/) for details.

## If you need to connect data

- Use [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) for service integration.
- Use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) for batching and caching relationship loads.
- See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) for list and provider-backed fields.

## If you need to preserve security behavior

- Use [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) for field and type access rules.
- See the [Public API guide](/docs/hotchocolate/v16/guides/public-api/) if unknown external consumers can send operations.
- See the [First-party API guide](/docs/hotchocolate/v16/guides/private-api/) if you control the clients and can use trusted documents.
- Use [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection/) for production guardrails.

## If you need to test the migration

- See [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) for migration-focused test strategies.
- Use the [Testing guide](/docs/hotchocolate/v16/guides/testing/) for server tests.
- Read [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) if clients already depend on your current schema.

## If you need endpoint, tooling, or production readiness guidance

- Use [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) for host-specific setup.
- See [Endpoints](/docs/hotchocolate/v16/server/endpoints/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) for endpoint and transport details.
- Use [Nitro](/docs/nitro/) for schema exploration and local operation testing.
- See [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) to prepare for production.

Checkpoint: You know when to leave this migration gateway and use the tutorial, concept, or reference page that matches your next task.
