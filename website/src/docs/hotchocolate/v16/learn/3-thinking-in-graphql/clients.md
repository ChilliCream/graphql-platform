---
title: "Clients"
description: "Learn what GraphQL clients own, when to use raw HTTP or a client library, and how operations, variables, fragments, caches, errors, and persisted documents shape client workflows with Hot Chocolate v16."
---

A GraphQL client sends operation documents to a single GraphQL endpoint and receives a response shaped by its request. The client is responsible for constructing the operation, managing variables, naming the operation, setting headers, handling authentication, processing the response, and updating application state. Hot Chocolate executes the schema and returns the GraphQL response envelope.

```json
{
  "query": "query GetBook($id: ID!) { bookById(id: $id) { id title } }",
  "operationName": "GetBook",
  "variables": {
    "id": "Qm9vazox"
  }
}
```

The flow looks like this:

```
schema -> operation document -> request envelope -> Hot Chocolate -> response envelope -> app state
```

Most production applications use a client library, as client-side needs often extend beyond a single HTTP request. Libraries can generate types, validate documents, execute requests, manage subscriptions, normalize data, update local stores, retry failed requests, and help publish operation contracts.

For details on operation syntax, variables, fragments, directives, and operation names, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/). This page focuses on the responsibilities of the client around those operations.

# Understanding Client Responsibilities

Start by asking: what does my application need to do to communicate with a Hot Chocolate API?

At its core, the application must build a GraphQL request:

```http
POST /graphql
Content-Type: application/json
Accept: application/graphql-response+json
Authorization: Bearer eyJ...
GraphQL-Client-Id: catalog-web
GraphQL-Client-Version: 2024.12.0

{
  "query": "query GetBook($id: ID!) { bookById(id: $id) { id title } }",
  "operationName": "GetBook",
  "variables": {
    "id": "Qm9vazox"
  }
}
```

The response matches the selection set:

```json
{
  "data": {
    "bookById": {
      "id": "Qm9vazox",
      "title": "GraphQL in Practice"
    }
  }
}
```

Each part of the request and response has a clear owner:

| Part | Owner | Why it matters |
| --- | --- | --- |
| Schema | Server team | Defines types, fields, arguments, nullability, and descriptions available to clients. |
| Operation document | Client team | Specifies the exact fields needed for a task. |
| Variables | Client team | Allows dynamic input without changing the operation text. |
| Request headers | Client and platform teams | Carry content negotiation, authorization, tenant context, and client identity. |
| Response envelope | Server produces, client interprets | Contains `data`, `errors`, and optional `extensions`. |
| Application state | Client team | Updates UI, services, normalized stores, or local caches from the response. |

In production, include a stable client name or ID and version in your requests if your observability or registry workflow supports it. Nitro operation monitoring uses `GraphQL-Client-Id` and `GraphQL-Client-Version` to group requests, aiding support, rollout coordination, deprecation planning, and operation reporting.

# When Raw HTTP Is Not Enough

Raw HTTP is helpful for inspecting requests, running smoke tests, writing scripts, server-to-server integrations, or debugging transport issues. However, as your application grows to include multiple screens, shared fragments, authentication, retries, subscriptions, generated types, or data reused in several places, raw HTTP becomes harder to manage. For example, if a mutation updates a book title, both a list and detail page should reflect the change consistently.

Use this table to help choose your client approach:

| Client shape | Use when | What you manage | Trade-off |
| --- | --- | --- | --- |
| Raw HTTP | Scripts, smoke tests, diagnostics, small integrations, first steps | JSON envelope, headers, variables, response parsing, retries, error handling | Transparent, but lacks schema support and managed store. |
| Generated typed client | .NET apps that benefit from schema-aware APIs | Checked-in operations, schema refresh, code generation, DI, result handling | Strong boundaries, but requires up-to-date build workflow. |
| Normalized UI client | Browser/mobile apps with shared entities | Fragment design, cache policies, entity identity, mutation/subscription updates | Excellent state model, but cache behavior must be learned and reviewed. |
| Lightweight client | Small JS, mobile, or server clients needing request helpers | Operation documents, transport config, response policy | Lower setup cost, but fewer built-in guardrails. |

[Nitro](/docs/nitro/) is ideal for interactive workflows: schema exploration, operation authoring, variables, auth headers, environments, response inspection, and sharing documents before code generation or runtime integration.

# Choosing a Client Library

Hot Chocolate interoperates through GraphQL and [GraphQL over HTTP](https://graphql.github.io/graphql-over-http/). The application platform usually determines the client library.

| Client family | Platform | Typing model | Cache/store model | Fragment model | Best fit |
| --- | --- | --- | --- | --- | --- |
| [Strawberry Shake](/docs/strawberryshake/v16) | .NET | Generated C# APIs from GraphQL documents | Store and reactive APIs, with options to reduce state management for server scenarios | GraphQL files participate in generation | .NET desktop, mobile, service, and Blazor-style clients seeking generated operation APIs. |
| [Relay](https://relay.dev/docs/introduction-to-relay/) | React | Compiler-generated artifacts and fragment references | Normalized store with strong data ownership rules | Fragment-driven components with data masking | React apps using compiler validation, colocated fragments, and strict store semantics. |
| [Apollo Client](https://www.apollographql.com/docs/react/) | JS/TS | Typed workflows via ecosystem tooling | Normalized cache with configurable policies | Supports fragments and cache-aware composition | JS/TS apps wanting a broad ecosystem and flexible cache policies. |
| [urql](https://nearform.com/open-source/urql/docs/) | JS/TS | Typed workflows via ecosystem tooling | Exchange-based client, optional normalized Graphcache | Supports fragments, behavior driven by tooling/exchanges | Apps needing a modular client pipeline and optional normalized caching. |
| Raw HTTP or platform clients | Any | Handwritten types or dynamic JSON | Whatever you build | Whatever you build | Small integrations, tests, jobs, or clients not needing a managed cache or generated surface. |

Nitro is not a runtime client. It is a workflow, debugging, collaboration, and client-registry tool that supports clients like Strawberry Shake, Relay, Apollo Client, urql, or raw HTTP.

# Nitro for Workflow, Debugging, and Collaboration

Use Nitro to discover, author, debug, share, validate, publish, or observe client operations.

| Task | Use Nitro to | Related docs |
| --- | --- | --- |
| Explore | Browse a schema and draft operations before adding to source control. | [Documents](/docs/nitro/documents/) [Operation pane](/docs/nitro/documents/operations/) |
| Configure request context | Switch endpoint URLs, headers, tokens, and environment values while keeping the operation stable. | [Authentication](/docs/nitro/documents/authentication/) [Environments](/docs/nitro/environments/) |
| Inspect responses | Compare raw `data`, `errors`, `extensions`, timing, status, traces, and subscription output with your runtime client. | [Response pane](/docs/nitro/documents/response/) |
| Collaborate | Share documents and focus team review on operation names, variables, fragments, and response expectations. | [Workspaces](/docs/nitro/workspaces/) |
| Govern releases | Validate client versions, publish known operation sets, and support trusted-document rollouts. | [Client registry](/docs/nitro/apis/client-registry/) [Nitro client CLI](/docs/nitro/cli-commands/client/) |
| Observe usage | Track active clients, operation traffic, error rates, latency, and field usage before removing fields. | [Operation reporting](/docs/nitro/apis/operation-reporting/) [Operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/) |

A typical workflow:

1. Explore the schema and draft `GetBook`.
2. Add variables and environment-specific authentication.
3. Inspect the response envelope and any errors.
4. Share the document with your team.
5. Move the checked-in operation into the runtime client.
6. Validate or publish the operation under a client version for a governed release.

Your application will still use Strawberry Shake, Relay, Apollo Client, urql, raw HTTP, or another runtime client to execute operations in production.

# Fragments: Making Data Requirements Explicit

A fragment is a reusable selection set on a GraphQL type. Client teams use fragments to make module and component data requirements visible.

```graphql
query GetBookPage($id: ID!) {
  bookById(id: $id) {
    id
    ...BookCard
  }
}

fragment BookCard on Book {
  title
  author {
    name
  }
}
```

In this document, the page operation owns the route-level lookup and includes the `BookCard` fragment. The `BookCard` component owns the fields it reads: `title` and `author.name`.

Fragments help because they:

- reduce duplicated field selections,
- make data dependencies easier to review,
- let a screen operation include child component requirements,
- keep checked-in GraphQL documents stable for code generation, analytics, trusted documents, and operation review.

Prefer checked-in GraphQL documents and fragments over opaque runtime query builders when operations participate in code generation, review, trusted documents, or analytics.

Relay popularized fragment-driven component data requirements. Its [fragments guide](https://relay.dev/docs/guided-tour/rendering/fragments/) is a strong conceptual reference even when you use another client library.

# Data Masking: Protecting Component Boundaries

Data masking ensures a component or module can access only the fields declared by its fragment, even if the parent operation fetched more data.

Without masking, a child component might read a field that the parent happened to fetch:

```
BookCard reads book.isbn, but BookCard did not declare isbn.
Another page reuses BookCard without isbn.
The component now fails or renders incomplete data.
```

With masking, the component reads only through its fragment contract:

```graphql
fragment BookCard on Book {
  title
  author {
    name
  }
}
```

If `BookCard` needs `isbn`, the fragment changes. Reviewers can see the new data requirement, generated types can update, and persisted-operation hashes can change through the normal workflow.

Masking prevents hidden dependencies on parent queries and makes refactoring safer. Removing a field from one component does not silently break another component that was reading undeclared data.

Relay's [data masking guide](https://relay.dev/docs/principles-and-architecture/thinking-in-relay/#data-masking) explains the strict version of this model. Some typed workflows and code generators offer similar fragment masking patterns. Check your client library's behavior before relying on it.

# Entities, Identity, and Client Stores

A client store keeps records from GraphQL responses so multiple screens can reuse and update the same entity. Normalized stores usually identify records by type name and stable ID, or by a client-specific key policy.

Consider a response that returns the same book in two places:

```json
{
  "data": {
    "featured": {
      "__typename": "Book",
      "id": "Qm9vazox",
      "title": "GraphQL in Practice"
    },
    "viewer": {
      "readingList": [
        {
          "__typename": "Book",
          "id": "Qm9vazox",
          "title": "GraphQL in Practice"
        }
      ]
    }
  }
}
```

A normalized store can treat both objects as one `Book:Qm9vazox` record. Query results then point to that record, not to unrelated JSON copies. When a mutation or subscription updates the book title, the store can update every screen that reads the same record.

Client stores differ by library. Consider:

| Concept | What to consider |
| --- | --- |
| Entity identity | Does the schema expose stable IDs on object types for clients? |
| Result references | Does the client keep response trees as JSON, normalized records, or both? |
| Mutation updates | Does the mutation payload include the changed object, affected IDs, or enough data to update lists? |
| Subscription updates | Does each event identify the record to update, insert, remove, or invalidate? |
| Nullability | Can generated result types express when fields may be absent or null after errors? |
| Pagination | Do connection edges and page info give the store enough structure to merge or refetch list pages? |

Relay, Apollo Client, urql with Graphcache, and Strawberry Shake all have store or cache concepts, but their APIs and defaults differ. Schema design affects all of them. Stable IDs, object types, nullability, connection edges, and mutation payloads make client updates easier to reason about. For schema design, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) and [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/).

# Building the Request Envelope

Most client libraries construct the request envelope for you, but you should recognize it in browser dev tools, logs, proxies, and integration tests.

For a standard HTTP `POST`, send JSON with `query`, optional `variables`, optional `operationName`, and optional `extensions`:

```json
{
  "query": "query GetBook($id: ID!) { bookById(id: $id) { id title } }",
  "operationName": "GetBook",
  "variables": {
    "id": "Qm9vazox"
  },
  "extensions": {
    "client": {
      "name": "catalog-web",
      "version": "2024.12.0"
    }
  }
}
```

Use variables instead of string interpolation. Variables keep user input out of the GraphQL source text and keep operation documents stable for validation, caching, generated clients, and persisted-operation workflows.

Headers are as important as the body:

| Header | Why send it |
| --- | --- |
| `Content-Type: application/json` | Tells Hot Chocolate to read a JSON request body. |
| `Accept: application/graphql-response+json` | Requests the GraphQL over HTTP response media type. |
| `Authorization` | Carries bearer tokens, basic credentials, or another auth scheme. |
| Tenant or locale headers | Carry application-specific context when your API uses it. |
| `GraphQL-Client-Id` | Identifies the registered client for Nitro monitoring. |
| `GraphQL-Client-Version` | Identifies the deployed client version for Nitro monitoring. |

HTTP `GET` requests are for query operations only. They are useful when the operation and variables fit your deployment and you want HTTP caching. The same request properties move into query parameters. For details on method, status, content negotiation, streaming, batching, multipart, and uploads, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/).

Persisted-operation requests change the envelope. The client sends an operation hash or identifier plus variables, often through `extensions` or a persisted-operation route, instead of the full operation text. The server resolves the document from storage or follows an automatic persisted operation negotiation flow.

For .NET generated client auth headers, see [Strawberry Shake authentication](/docs/strawberryshake/v16/networking/authentication/). For environment-specific URLs and headers during testing, see [Nitro environments](/docs/nitro/environments/) and [Nitro authentication](/docs/nitro/documents/authentication/).

# Reading the Response Envelope

GraphQL responses have top-level fields:

| Field | Meaning |
| --- | --- |
| `data` | The result of executing the operation when execution produced data. |
| `errors` | Request or execution errors reported by the server. |
| `extensions` | Optional metadata such as tracing, persisted-operation information, or server-specific details. |

A successful response contains only `data`:

```json
{
  "data": {
    "bookById": {
      "id": "Qm9vazox",
      "title": "GraphQL in Practice"
    }
  }
}
```

Partial data may appear with errors:

```json
{
  "data": {
    "bookById": {
      "id": "Qm9vazox",
      "title": null
    }
  },
  "errors": [
    {
      "message": "The title could not be loaded.",
      "path": ["bookById", "title"]
    }
  ]
}
```

A request-level error may have no `data`:

```json
{
  "errors": [
    {
      "message": "The variable `id` is required."
    }
  ]
}
```

Do not treat every `errors` array as a total network failure. Inspect both `data` and `errors`. Render usable data when possible, show field-level failures where they belong, and log diagnostics with the operation name, variables, client identity, and error path.

HTTP status and GraphQL error semantics are related but not identical. With `Accept: application/graphql-response+json`, clients can use the GraphQL over HTTP response media type and status rules. With legacy `Accept: application/json`, Hot Chocolate may return legacy-style status behavior. Use the response envelope to determine GraphQL success, partial success, or failure, and use HTTP status for transport-level handling according to the negotiated media type.

Nullability determines how far a field error affects the response shape. For more on server-side execution, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/), [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

# Variables, Operation Names, and Persisted Documents as Contracts

Variables, operation names, and persisted documents turn client behavior into something you can validate, observe, and release.

| Contract piece | What it does | Why it matters |
| --- | --- | --- |
| Variables | Separate runtime input from operation text. | Keeps documents stable and avoids rebuilding GraphQL strings. |
| Operation names | Name the work the client asks the server to perform. | Improves logs, traces, generated APIs, registry views, support, and review. |
| Document hashes | Identify a specific operation document. | Enables persisted operations, APQ, operation caches, and known-operation policies. |
| Trusted documents | Register allowed operation documents before deployment. | Lets the server reject unknown operations in controlled environments. |
| Client versions | Group operation sets by deployed client release. | Supports compatibility checks, rollout planning, and deprecation work. |

Generated clients often turn operation names into methods, classes, or result types. Choose names that describe user intent, such as `GetBookForCheckout`, rather than names that only describe a UI component.

Persisted operations and trusted documents are maturity steps after the client can send normal operations. Two common flows:

| Flow | What happens |
| --- | --- |
| Known operation ID | The client sends an operation ID and variables. Hot Chocolate finds the document in operation storage, executes it, and returns a result. |
| Unknown operation ID | A trusted-document setup rejects the request, while an automatic persisted operation setup can ask the client to upload the full document for storage. |

Use this checklist when operations become production contracts:

| Step | Check |
| --- | --- |
| Extract | Keep GraphQL documents and fragments in source control, then extract them from the client build. |
| Validate | Validate operations against the target schema locally or in CI. |
| Register | Publish trusted documents or a client version for the target stage. |
| Deploy | Deploy the accepting server or gateway before a client that depends on new operation IDs. |
| Monitor | Watch unknown-operation misses, active operation names, client versions, and field usage after rollout. |

For server setup, see [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/). For registry workflows, see [Nitro client registry](/docs/nitro/apis/client-registry/) and [Nitro client CLI](/docs/nitro/cli-commands/client/). For Strawberry Shake persisted operation support, see [Strawberry Shake persisted operations](/docs/strawberryshake/v16/performance/persisted-operations/).

# Planning for Schema Changes and Generated Artifacts

Schema-aware clients should refresh their schema artifact when the server schema changes. Treat this as a normal part of client development.

A safe local workflow:

1. Fetch or update the schema artifact.
2. Validate operations and fragments against the schema.
3. Regenerate client artifacts.
4. Compile the client.
5. Run a representative request against the target endpoint or in Nitro.

Generated clients turn schema changes into build-time feedback, which is valuable. It catches renamed or removed fields, changed argument requirements, nullability changes, and fragment mismatches before runtime failures.

Use CI to validate client operations against the schema before deployment. Use Nitro to validate shared documents or a client registry entry against a changed schema before releasing generated artifacts.

For deprecations, combine schema checks with observed usage:

| Step | Question |
| --- | --- |
| Identify usage | Which clients, versions, and operations select the field or argument? |
| Provide replacement | Is the new field available and documented before clients migrate? |
| Coordinate owners | Which team owns each active operation? |
| Monitor | Have request count, operation count, client count, and error rate dropped enough to remove the old field? |
| Remove | Has the removal been validated against known client versions and trusted documents? |

Static schema diffing shows what changed. Operation reporting and telemetry show who still uses the old contract. For more on rollout, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/).

# Common Client Pitfalls

Use this checklist if a client works in one tool but fails in another:

| Symptom | Likely cause | What to check |
| --- | --- | --- |
| Validation errors before resolver code runs | Operation, fragment, variable type, or stale schema artifact does not match the schema | Refresh schema, validate operations, inspect variables, retry in Nitro. |
| Component cannot access a field the parent fetched | Fragment masking or generated types prevent undeclared reads | Add the field to the component fragment or pass a non-GraphQL prop. |
| Mutation succeeds but another screen shows old data | Store update, cache invalidation, entity identity, or refetch policy is incomplete | Inspect IDs, cache policies, mutation payload shape, and affected lists. |
| Response contains both `data` and `errors` | Field-level error or nullability propagation produced partial data | Handle partial data intentionally and inspect error paths. |
| Request works in Nitro but not in the app | Endpoint, headers, auth, variables, `Accept`, `Content-Type`, or proxy behavior differs | Compare the raw request envelope and headers. |
| Generated types are stale | The schema changed after code generation | Refresh schema and regenerate artifacts. |
| Persisted operation is unknown | Document not published, hash differs, storage is empty, or rollout order is wrong | Compare operation text, hash algorithm, registry state, and deployment order. |
| Cached data appears for the wrong user or tenant | Cache key misses variables, auth context, tenant headers, or user-specific state | Move caching to the correct layer or vary cache keys. |
| Deprecated field still receives traffic | Active client versions or persisted operations still select it | Use operation reporting to find owners and schedule migration. |

# Next Steps

Pick the next page by the task in front of you.

| If you need to | Go to |
| --- | --- |
| Make a first request from another process | [Connect a client](/docs/hotchocolate/v16/get-started/connecting-a-client/) |
| Review operation documents, variables, fragments, directives, and operation names | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |
| Generate a typed .NET client | [Strawberry Shake](/docs/strawberryshake/v16) and [Get started with Strawberry Shake](/docs/strawberryshake/v16/get-started/) |
| Configure auth headers in a .NET GraphQL client | [Strawberry Shake authentication](/docs/strawberryshake/v16/networking/authentication/) |
| Explore the schema, run operations, and inspect responses | [Nitro](/docs/nitro/), [Operation pane](/docs/nitro/documents/operations/), and [Response pane](/docs/nitro/documents/response/) |
| Validate and publish known operation sets | [Nitro client registry](/docs/nitro/apis/client-registry/) and [Nitro client CLI](/docs/nitro/cli-commands/client/) |
| Set up trusted documents or APQ on Hot Chocolate | [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) |
| Handle partial data and errors correctly | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and the [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) |
| Study transport methods, status codes, content negotiation, streaming, uploads, and batching | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) |
| Add realtime client behavior | [Subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime/) and [Strawberry Shake subscriptions](/docs/strawberryshake/v16/subscriptions/) |
| Evolve the schema without surprising clients | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Nitro operation reporting](/docs/nitro/apis/operation-reporting/) |

The goal is for the client to own its operations. When your team manages operation documents, variables, names, response handling, cache behavior, and release contracts, both server and client can evolve together with fewer surprises.
