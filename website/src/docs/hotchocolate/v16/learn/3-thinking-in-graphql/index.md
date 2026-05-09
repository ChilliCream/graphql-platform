---
title: "Thinking in GraphQL"
description: "Choose the Hot Chocolate v16 concept page that matches your schema, operation, resolver, client, data, error, performance, or production design question."
---

After your first query succeeds, you may wonder how your API should evolve as more clients, fields, data sources, and production requirements appear. This section guides you through those design decisions, bridging the gap between the hands-on tutorial and the reference documentation. Here, you will find the mental models behind schemas, operations, resolvers, execution, clients, nullability, errors, pagination, realtime updates, testing, performance, security boundaries, and schema evolution.

You do not need to memorize every Hot Chocolate option. Instead, focus on recognizing the design question you are facing, selecting the right concept page, and then moving to the tutorial or reference that helps you implement your decision.

# Recommended reading path

If you are new to GraphQL design, follow this path. If you are already familiar with a topic, skip ahead to the row that matches your current project phase.

| If you are trying to | Read | You should be able to decide |
| --- | --- | --- |
| Confirm GraphQL fits your .NET API | [Why GraphQL on .NET](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/why-graphql-on-dotnet/) | Whether GraphQL addresses a real client and API coordination problem for your team |
| Review the schema as a product contract | [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) | Which fields, names, descriptions, nullability rules, and evolution risks belong in the schema |
| Choose how to define the schema | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) | Whether to start with implementation-first types or code-first descriptors |
| Design the first larger schema area | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) | Whether a field should expose a domain object, a task-focused lookup, or a mutation |
| Choose object identity or polymorphism patterns | [Polymorphism and identity](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/polymorphism-and-identity/) | Whether IDs, interfaces, unions, or Node-style identity help clients use the graph |
| Understand what clients send | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) and [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) | How operation documents, variables, operation names, and client applications relate |
| Debug response behavior | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/), [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) | Whether a symptom belongs to validation, resolver execution, result completion, or error propagation |
| Connect real data | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) and [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) | Where resolver boundaries, provider translation, DataLoader, paging, filtering, sorting, and projections belong |
| Add realtime behavior | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) | Whether an event stream, topic model, transport, and provider fit the client need |
| Prepare for wider use | [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/), [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/), Security and API boundaries (planned), and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) | Which risks to plan before more clients depend on the schema |

If you prefer to learn by building, follow the [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) tutorial and return here when you encounter a design decision that needs a deeper explanation.

# Find guidance by your current design question

Select the page that matches the question you are working through now.

| Current question | Start here | Then read | Success signal |
| --- | --- | --- | --- |
| Is GraphQL the right fit for my .NET API? | [Why GraphQL on .NET](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/why-graphql-on-dotnet/) | [Public API guide](/docs/hotchocolate/v16/guides/public-api/) or [First-party API guide](/docs/hotchocolate/v16/guides/private-api/) | You can describe the client coordination problem GraphQL will solve |
| How should I review the schema before clients depend on it? | [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) | You have a review path for field purpose, names, descriptions, nullability, cost, authorization, and change risk |
| Should I use implementation-first or code-first? | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) | You know which schema definition style your team will use first |
| How should I model a new query field? | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) and [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) | The field has a clear purpose, name, arguments, result shape, and client task |
| Should this result use an interface, union, or global identity pattern? | [Polymorphism and identity](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/polymorphism-and-identity/) | [Interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces/), [Unions](/docs/hotchocolate/v16/building-a-schema/unions/), and [Relay](/docs/hotchocolate/v16/building-a-schema/relay/) | Clients can identify objects and handle heterogeneous results without schema guesswork |
| How should I model a write? | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) | The mutation has an input, payload, domain outcome, and error strategy |
| Why did a resolver run many times? | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) | You can separate field execution from data access and spot N+1 risk |
| Why did the response contain `data` and `errors`? | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and the [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) | You can explain partial data, resolver errors, and non-null propagation |
| Which pagination style should this list use? | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) | The list contract matches expected size, navigation needs, and client stability |
| Does this use case need live updates? | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) | You know whether subscription fields, topics, transports, and scale-out providers fit the event stream |
| How should clients explore or call the API? | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) | [Nitro](/docs/nitro/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) | You know whether the client needs exploration, raw HTTP, generated code, or trusted operations |
| How should operation caching and trusted documents shape the contract? | [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/) | [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) | You can separate response caching, client caching, DataLoader caching, and operation trust |
| What must be ready before production? | [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) | Security and API boundaries (planned), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/), and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) | You can name the tests and guardrails that protect real clients |

# Build your mental model of GraphQL

Every GraphQL API begins with a schema. The schema is the contract that clients discover, read, and depend on. It defines the types, fields, arguments, operations, nullability rules, and descriptions that make the API understandable outside your C# project.

Clients interact with the API by sending operations. An operation document selects fields from the schema, passes variables, and names the work the client wants the server to perform. The [GraphQL specification](https://spec.graphql.org/October2021/) describes these rules, and Hot Chocolate implements them for .NET applications.

Before execution, Hot Chocolate validates the operation against the schema. If the operation requests an unknown field or sends a value with the wrong shape, validation fails before resolver behavior is relevant.

Resolvers are responsible for producing field values. A resolver might return a value from memory, a service, a database, another API, or a DataLoader. Data middleware such as paging, filtering, sorting, and projections helps shape list fields and provider-backed queries when the resolver return type supports it.

Execution turns the validated operation into a response. The response contains `data` when fields complete, and may include `errors` if validation or execution reports a problem. Nullability rules determine how far an execution error affects the surrounding response shape.

```text
schema contract -> client operation -> validation -> resolvers and data -> data plus errors response
```

Refer to this flow when you encounter unexpected behavior. Consider whether the issue belongs to schema design, operation shape, resolver logic, data middleware, nullability, or error handling.

# Design the schema around client tasks

Begin schema design with the client task and the domain language. Avoid exposing every C# type, table, controller action, or service method by default.

Before adding a field, review it using this checklist:

| Review point | Question to answer | Read next |
| --- | --- | --- |
| Purpose | What client task does this field support? | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |
| Contract review | Does the field belong in the client-facing schema? | [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) |
| Name | Does the name make sense without reading C# source code? | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Arguments | Which values should the client control, and which values belong in server policy? | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) |
| Identity and variants | Do clients need stable IDs, shared interfaces, or heterogeneous results? | [Polymorphism and identity](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/polymorphism-and-identity/) |
| Nullability | Which values are guaranteed, optional, or allowed to fail independently? | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) |
| Lists | Can the list grow, and does it need pagination from the start? | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |
| Errors | Should domain failures appear in the payload, the `errors` array, or both? | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) |
| Authorization | Which callers can see or execute this part of the graph? | Security and API boundaries (planned) |
| Evolution | Can you change this field later without breaking known clients? | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |

For public APIs, treat schema design as a long-lived contract. For first-party APIs, you may have tighter control over clients, but the schema still becomes the language your teams use to coordinate change.

# Understand runtime behavior for effective debugging

Understanding runtime behavior helps you debug issues and make better design decisions.

| Symptom | Likely area | Recovery route |
| --- | --- | --- |
| Nitro reports an unknown field | The operation does not match the current schema, or naming differs from the C# member you expected | Read [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) and then try [Add a field](/docs/hotchocolate/v16/learn/1-quick-start/add-a-field/) |
| The response has `data` and `errors` | A resolver reported an error, or nullability changed how the result completed | Read [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) |
| A nested field causes repeated data access | Field execution is following the selection set, and the data layer needs batching or caching | Read [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Paging, filtering, sorting, or projections do not change the data | The resolver return shape, provider support, or data middleware setup may not match the field | Read [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) |
| A client works in Nitro but not in an app | Variables, endpoint URL, request format, CORS, authentication, or production operation policy may differ | Read [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |

For detailed pipeline information, see the [Execution engine](/docs/hotchocolate/v16/execution-engine/) reference.

# Connect operations, clients, and production contracts

A GraphQL operation is not the same as a client library. The same operation can be sent from Nitro, `curl`, a browser app, a mobile app, a test, or a generated client. The server always receives an operation document, variables, and request metadata.

Keep this distinction in mind when planning client work:

| Need | Use | Read next |
| --- | --- | --- |
| Explore the schema and inspect response shapes | [Nitro](/docs/nitro/) | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Understand operation text, variables, names, and selection sets | GraphQL operation concepts | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |
| Send requests over HTTP | GraphQL over HTTP | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| Coordinate known client operations in production | Trusted documents and registry workflows | [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/), [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/), and [Nitro client registry](/docs/nitro/apis/client-registry/) |
| Stream events to clients | Subscription fields, topics, transports, and providers | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) |
| Monitor and evolve a contract with real consumers | Schema registry, client registry, tests, and deprecation | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |

For production, decide whether your API will accept ad-hoc operations from unknown consumers or only known operations from clients you control. This choice affects introspection, trusted documents, cost analysis, caching, monitoring, and schema evolution.

# Prepare for data, performance, testing, and rollout

Plan for these decisions before your schema is widely adopted.

| Readiness question | Why it matters | Route |
| --- | --- | --- |
| Which data source owns each field? | GraphQL lets clients select fields independently, so your data boundaries must handle nested selections | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) |
| Where can N+1 work appear? | Nested selections can repeat relationship lookups unless you batch or translate them well | [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| Which operation limits do you need? | Large selections, deep nesting, or expensive list fields need guardrails | [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) |
| Which operations should clients be allowed to run? | Public, first-party, and internal APIs need different trust, introspection, and authorization boundaries | Security and API boundaries (planned) |
| Which caching layer owns each optimization? | Client stores, operation caches, response caches, persisted documents, and DataLoader solve different problems | [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/) |
| Do clients need realtime updates? | Event streams add topic design, transport, scale-out, and operational concerns | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) |
| How will you verify the contract? | Schema changes and operation behavior need repeatable checks | [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) and [Testing guide](/docs/hotchocolate/v16/guides/testing/) |
| How will clients survive schema change? | Removing or changing fields can break consumers | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Schema evolution guide](/docs/hotchocolate/v16/guides/schema-evolution/) |
| How will you observe the API? | Production GraphQL needs visibility into requests, errors, latency, and field behavior | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) |

Treat these as design inputs, not last-minute fixes. A small internal graph can start with lighter guardrails. A public API or a first-party API with many deployed clients requires stronger contract, operation, and rollout planning.

Routes marked as planned are included in the section map even if their pages are still being written. Use the linked reference documentation beside them until those pages are available.

# When you need a task recipe

Thinking pages help you choose the right approach. Other documentation shows you what to type or which API to use.

| I need to | Go to |
| --- | --- |
| Build a server from start to finish | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) |
| Make a small verified edit | [Quick Start](/docs/hotchocolate/v16/learn/1-quick-start/) |
| Install packages, configure hosting, or map endpoints | [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/) |
| Add Hot Chocolate to an existing ASP.NET Core app | [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) |
| Translate REST, OData, Apollo Server, GraphQL.NET, or earlier Hot Chocolate habits | [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) |
| Look up exact attributes, descriptors, options, or provider APIs | [Building a schema](/docs/hotchocolate/v16/building-a-schema/), [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/), or [Server](/docs/hotchocolate/v16/server/) |
| Recover from a tutorial issue | [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) |
| Explore, register, or monitor APIs with ChilliCream tooling | [Nitro](/docs/nitro/) |

# Next steps

Choose your next page based on your current decision:

1. Read [Why GraphQL on .NET](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/why-graphql-on-dotnet/) if you are still evaluating the architectural fit.
2. Read [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) before a larger schema area becomes a client contract.
3. Read [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) before you define the schema in Hot Chocolate.
4. Read [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) when a response or resolver behavior is unexpected.
5. Return to the [full tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) when you want to apply the concept in a working server.
