---
title: "Learn Hot Chocolate"
description: "Choose the right Hot Chocolate v16 learning path after your first GraphQL result."
---

Welcome to the Learn section, your guide for progressing from a basic working server to confidently designing, building, and operating a GraphQL API with Hot Chocolate.

After you complete [Get Started](/docs/hotchocolate/v16/get-started/) and see your first GraphQL response, whether from a running server, a `/graphql` endpoint, Nitro in the browser, or a client request, this area helps you understand what is happening behind the scenes. Here, you will connect schema design, resolvers, data access, client interactions, setup decisions, testing, security, and migration strategies.

For a structured journey, begin with the [full tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/). If you already have a server running and want to make quick progress, try the [quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/).

# Choose your learning path

Use the table below to find the best starting point for your current situation.

| Your situation | Start here | You will leave with | Continue with |
| --- | --- | --- | --- |
| You do not have a running server yet | [Get Started](/docs/hotchocolate/v16/get-started/) | A first working Hot Chocolate result and a verified endpoint | [Quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/) or the [full tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) |
| You have a running server and want small next steps | [Quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/) | A new field, an argument, a query variable, and an early data access preview | [Build the full tutorial project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) |
| You want a complete guided project | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) | A tutorial server with schema, resolvers, data access, DataLoader, pagination, mutations, subscriptions, tests, a client call, security basics, and production preparation | [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) when you need to recover |
| You want to understand GraphQL and Hot Chocolate concepts before coding more | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) | A mental model for schemas, operations, resolvers, data, nullability, errors, clients, performance, and schema evolution | The tutorial chapter that lets you apply the concept |
| You need to add Hot Chocolate to a specific host | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) | The setup route for your ASP.NET Core app, local environment, container, proxy, Azure Functions, Aspire, or worker-style execution | [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/) when version alignment is in question |
| You are translating previous experience | [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) | A bridge from REST, EF-backed REST APIs, OData, Apollo Server, GraphQL.NET, or earlier Hot Chocolate knowledge | Targeted Learn pages for the concepts that differ |
| You are blocked in the tutorial | [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) | A symptom-based recovery route for project state, schema output, query results, data, or checkpoints | [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) |

# Follow the recommended reading order

You do not need to read every page in order. Use this sequence when you want a curriculum instead of isolated links.

## If you are new to Hot Chocolate

1. Get a first result in [Get Started](/docs/hotchocolate/v16/get-started/) if you have not already done so.
2. Use the [quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/) to make small schema and resolver edits.
3. Build the [full tutorial project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/).
4. Read [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) when design questions appear.
5. Use [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) when you adapt the ideas to a real host.
6. Use [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) when older concepts influence your design choices.

After the first two steps, you will have a running server, a new field, an argument, and an understanding of how resolver data enters the graph.

## If you already know GraphQL or .NET server development

Skim the [quick start lessons](/docs/hotchchocolate/v16/learn/1-quick-start/) or the [tutorial overview](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) to get familiar with the documentation style. Then read [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) to understand how schema examples are presented.

From there, go to [setup guidance](/docs/hotchocolate/v16/learn/4-installation-and-setup/) or the concept essay that addresses your current design question.

## If you are returning from an earlier Hot Chocolate version

Start with the authoritative migration notes for your version. For v16 migration work, read [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16). If you are comparing current Learn material with older code or habits, use [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) and the bridge that matches your situation.

Before changing production code, align packages, validate the schema, and rerun important operations against the updated server.

# Start with quick lessons after first success

The quick start lessons are designed for short, focused edits once your server is running. Each lesson covers a single change and a visible result.

| Lesson | Choose this if | Checkpoint |
| --- | --- | --- |
| [Quick start overview](/docs/hotchocolate/v16/learn/1-quick-start/) | You want to see the lesson set before choosing a task | You know which small edit to make next |
| [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field/) | You want a new property-backed or resolver-backed field | Nitro shows the field in the schema and a query returns it |
| [Add an argument](/docs/hotchocolate/v16/learn/1-quick-start/add-an-argument/) | You want a field that changes based on client input | An operation with a variable changes the field result |
| [Use real data preview](/docs/hotchocolate/v16/learn/1-quick-start/use-real-data-preview/) | You want to replace sample data with a first data source | A resolver returns data from your chosen source and you know where deeper data guidance lives |

Use these lessons to build confidence with small changes. When you are ready for a complete server path with data modeling, batching, tests, and production hardening, move to the full tutorial.

# Build the full tutorial project

The full tutorial is the best way to gain a lasting understanding. It guides you through building a realistic server, from project setup to production readiness.

You will progress through these phases:

1. **Set up the project:** Create and run the tutorial baseline, open Nitro, and verify the server.
2. **Define the graph:** Add types, fields, arguments, filters, and resolver methods.
3. **Connect data:** Replace in-memory examples with real data access patterns, then use DataLoader to avoid repeated work.
4. **Shape client operations:** Add pagination, mutations, subscriptions, and a client request flow.
5. **Verify behavior:** Test the server and compare responses with checkpoints.
6. **Prepare for production:** Review security basics, field-level authorization, and readiness checks.

Start with [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/). Bookmark [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) and [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) for quick recovery if you get stuck.

If you only need a specific part, use the tutorial chapters as entry points. For example, open the DataLoader chapter if you see repeated data calls for the same relationship field, or the production chapter when preparing a deployment checklist.

# Use Thinking in GraphQL for design questions

Concept essays help you make informed design decisions, while tutorials provide hands-on practice. You do not need to read every essay before you start coding. Open the page that matches your current question.

| Question | Read |
| --- | --- |
| Why should I use GraphQL on .NET? | [Why GraphQL on .NET](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/why-graphql-on-dotnet/) |
| Should my team use implementation-first or code-first? | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) |
| How does a GraphQL operation execute? | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) |
| Should this be a field, a query, or a mutation? | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) and [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |
| Why did a resolver run many times? | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) |
| How should clients use this graph? | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Why is my response partial or nullable? | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) |
| Which pagination style should I choose? | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |
| How should I model realtime behavior? | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) |
| How do I test or tune this API? | [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) |
| How should the schema evolve? | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| How do I connect to real data responsibly? | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) |

When a concept turns into a concrete task, move to the relevant reference area. Use [Building a schema](/docs/hotchocolate/v16/building-a-schema/) for type-system work, and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) for data fetching, DataLoader, filtering, sorting, projections, and pagination.

# Installation and setup guidance for your host

Refer to the setup material when you need to adapt Hot Chocolate to your application environment. Ensure all Hot Chocolate packages in your application use the same major and minor version before troubleshooting deeper issues.

| Host or setup need | Choose |
| --- | --- |
| A standard ASP.NET Core GraphQL service | [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) |
| An existing ASP.NET Core application | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) |
| Minimal APIs beside GraphQL | [Minimal APIs](/docs/hotchocolate/v16/learn/4-installation-and-setup/minimal-apis/) |
| Local tooling, URLs, and development checks | [Local development](/docs/hotchocolate/v16/learn/4-installation-and-setup/local-development/) |
| Azure Functions | [Azure Functions](/docs/hotchocolate/v16/learn/4-installation-and-setup/azure-functions/) |
| .NET Aspire | [Aspire](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspire/) |
| Package selection or version mismatch | [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/) |

If your goal is still to get your first server running, use the [Get Started](/docs/hotchocolate/v16/get-started/) guide. Return to the setup pages when you need host-specific instructions.

# Translate your existing knowledge

Many developers bring valuable mental models from other stacks. The bridge pages help you identify what transfers and what needs to change.

| Your background | Start with | Translation focus |
| --- | --- | --- |
| REST controllers | [Coming from REST controllers](/docs/hotchocolate/v16/learn/5-coming-from/rest-controllers/) | Routes and controller actions become designed schema fields and operations, not one-to-one endpoints |
| GraphQL.NET | [Coming from GraphQL.NET](/docs/hotchocolate/v16/learn/5-coming-from/graphql-dotnet/) | Similar goal, different defaults for schema construction, dependency injection, resolvers, and testing |
| Earlier Hot Chocolate versions | [Migration guides](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) | Prepare, align packages, validate schema snapshots, rerun operations, and follow authoritative migration notes |

Use these bridges as a secondary resource. Once you have clarity, return to the tutorial, concept, or reference pages that match your current task.

# Get help if you get stuck

Encountering obstacles is part of learning any framework. Use the table below to match your symptom to the best recovery resource.

| Symptom | Open |
| --- | --- |
| You do not know whether to start with Get Started, quick lessons, or the tutorial | [Choose your learning path](#choose-your-learning-path) |
| You are missing SDK, NuGet, editor, or Nitro prerequisites | [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/) |
| Template installation, restore, startup, endpoint, or Nitro fails during the first server path | [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) |
| Package restore or runtime errors suggest mixed Hot Chocolate versions | [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/) |
| The local server starts but `/graphql` returns 404 or does not show the expected tool | [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) and [server endpoints](/docs/hotchocolate/v16/server/endpoints/) |
| A new field or argument does not appear after an edit | [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field/) or [Building a schema](/docs/hotchocolate/v16/building-a-schema/) |
| Tutorial output differs from the expected result | [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) |
| You need to restore a known tutorial state | [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) |
| Data access or DataLoader behavior differs from the lesson | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Previous stack vocabulary is leading to the wrong design | [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) |

After you recover, rerun the checkpoint that failed. This might be a build, an endpoint response, a schema field in Nitro, a matching query result, or a restored tutorial state.

# What to read next

Once you know your learning path, move to the main documentation for task-specific details:

- **Design schema features.** Use [Building a schema](/docs/hotchocolate/v16/building-a-schema/) for queries, mutations, subscriptions, object types, arguments, nullability, directives, and schema evolution.
- **Fetch and shape data.** Use [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) for resolvers, dependency injection, DataLoader, databases, REST APIs, pagination, filtering, sorting, and projections.
- **Expose the server.** Use [Server](/docs/hotchocolate/v16/server/) for endpoints, HTTP transport, WebSockets, files, interceptors, global state, cache control, warmup, and command-line workflows.
- **Harden the API.** Use [Securing your API](/docs/hotchocolate/v16/securing-your-api/) for authentication, authorization, cost analysis, request limits, and introspection controls.
- **Tune operations.** Use [Performance](/docs/hotchocolate/v16/performance/) and [Performance guide](/docs/hotchocolate/v16/guides/performance/) for trusted documents, persisted operations, warmup, caching, and runtime tuning.
- **Test behavior.** Use [Testing guide](/docs/hotchocolate/v16/guides/testing/) when you need automated verification.
- **Split a larger graph across services.** Use [Fusion](/docs/fusion/v16) when one server is no longer the right ownership boundary.
- **Upgrade across versions.** Use [Migration guides](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) for authoritative breaking-change detail.

# Next steps

If you are still choosing, start with [Get Started](/docs/hotchocolate/v16/get-started/) for first success or [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) for the recommended learning path.

If you have a running server and want a short next task, open [Quick start lessons](/docs/hotchocolate/v16/learn/1-quick-start/). If a design question is blocking you, open [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/).
