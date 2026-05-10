---
title: "Testing GraphQL APIs"
description: "Choose the right test layers for Hot Chocolate v16 APIs, protect the schema contract with CookieCrumble snapshots, and verify operations, authorization, and production guardrails."
---

When a teammate renames a field, changes `String` to `String!`, or moves an authorization rule, the server may still start and the `/graphql` endpoint may still respond. However, these changes can break client expectations. Testing GraphQL APIs is about more than checking endpoint availability. It is about protecting the contract your clients rely on.

GraphQL exposes a single HTTP endpoint, but clients interact with it through many distinct operations. The schema defines the durable contract, while each operation represents an executable contract. The safety boundary is enforced by rules that reject unsafe or unauthorized requests before they reach sensitive logic.

# Map GraphQL Risks to Test Layers

Begin by identifying the risk you want to catch, then select the appropriate test layer.

| Layer | What it answers | Failure signal | Preferred assertion style |
| --- | --- | --- | --- |
| Schema contract test | Has the public GraphQL contract changed? | SDL diff shows renamed fields, removed types, changed nullability, changed arguments, or directive changes. | CookieCrumble schema snapshot. |
| Operation test | Do key queries and mutations return the expected `data` and `errors`? | GraphQL result differs, contains unexpected `errors`, or returns unexpected partial data. | CookieCrumble result snapshot for stable responses, targeted assertions for volatile values. |
| Resolver, service, and data test | Does business or data logic behave as intended? | Branch, mapping, batching, repository, or domain rule fails. | Targeted assertions close to the logic. |
| Transport test | Does the ASP.NET Core host handle requests as clients expect? | Wrong route, status code, content type, headers, cookies, authentication, or WebSocket behavior. | Host integration test with `WebApplicationFactory` or the transport fixture your app uses. |
| Guardrail test | Are unsafe or unauthorized operations blocked? | Unauthorized caller gets protected data, or an overly deep, broad, costly, or untrusted operation reaches resolver work. | Operation or host test that asserts the GraphQL error envelope and side effects. |

A breaking nullability change should fail the schema snapshot. An incorrect resolver result should fail an operation, resolver, or service test. A missing authentication header should fail at the host layer, since HTTP middleware participates in the behavior.

# Start with a CookieCrumble Schema Snapshot

```csharp
using CookieCrumble.HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class SchemaTests
{
    [Fact]
    public async Task Schema_Should_Not_Change_Unexpectedly()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.MatchSnapshot();
    }
}
```

Every maintained GraphQL API should include a schema snapshot. This records the schema SDL and fails the test if the public contract changes.

The snapshot will catch changes such as:

- Renamed fields or types
- Removed fields
- Changed nullability
- Changed argument types or defaults
- Added or removed directives
- Generated shape changes from paging, filtering, projections, mutation conventions, or authorization metadata

The first run records the SDL. Later runs show a diff when the schema changes. Treat this diff as an API review prompt: was this contract change intended?

If the change is intentional, update the snapshot after review. If not, fix the schema registration or resolver binding that caused the change.

When building an `ISchemaDefinition` directly, use the Hot Chocolate CookieCrumble extension to capture the schema SDL:

```csharp
schema.MatchSnapshot();
```

For setup steps, required packages, and snapshot update workflow, see the [Testing guide](/docs/hotchocolate/v16/guides/testing/).

# Test Real Operations That Represent User Journeys

```csharp
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ProductOperationTests
{
    [Fact]
    public async Task Product_By_Id_Should_Return_Product()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddSingleton<ICatalogService>(new FakeCatalogService())
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query ProductById($id: ID!) {
                  productById(id: $id) {
                    id
                    name
                  }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?> { ["id"] = "1" })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.MatchSnapshot();
    }
}
```

Operation tests complement schema snapshots by proving behavior through Hot Chocolate execution. They cover parsing, validation, variable coercion, middleware, dependency injection, DataLoader usage, resolver execution, null completion, and result formatting.

Choose operations based on user journeys, not schema coverage percentage.

| Operation to test | Why it matters |
| --- | --- |
| The query that loads the most important screen | Protects the read path clients use most |
| The mutation that changes critical state | Protects input coercion, domain rules, payload shape, and side effects |
| A query using fragments, variables, filters, paging, or sorting | Protects the operation shapes real clients send |
| A DataLoader-heavy query | Protects resolver wiring and batching behavior |
| A known production operation from reporting or support | Prevents regressions in behavior clients depend on |

Assert the GraphQL response envelope. A successful operation should return the expected `data` shape and no unexpected `errors`. When testing for expected failures, assert the error shape, including `path` and stable values in `extensions` if your API defines them.

Refer to the [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) for assertion structure. For more on documents, operation names, variables, and fragments, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/). For error handling, see [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

## Choose Executor Tests or Host Tests

| If the behavior depends on... | Use... |
| --- | --- |
| GraphQL schema execution, middleware, variables, DataLoader, filtering, paging, projections, or resolver wiring | An in-process `IRequestExecutor` test |
| ASP.NET Core routing, request headers, cookies, authentication middleware, content negotiation, response status codes, or JSON transport | A host integration test with `WebApplicationFactory` |
| WebSocket subscription transport behavior | A host or transport-level test that exercises the subscription protocol |
| A service, repository, mapper, or domain branch without GraphQL behavior | A focused unit or integration test at that layer |

Executor tests are fast and avoid the web host. Host tests are more expensive but validate the same boundary clients use. For more, see Microsoft's [`WebApplicationFactory` integration testing guidance](https://learn.microsoft.com/aspnet/core/test/integration-tests).

# Test Subscriptions at the Right Layer

Subscriptions require tests at multiple boundaries, since resolver, event provider, and transport behaviors can fail in different ways.

Use executor tests for the GraphQL part of a subscription. Execute the subscription operation, publish controlled events, and assert the payload shape, filtering, authorization, and error handling. This keeps resolver and payload regressions close to the schema execution pipeline.

Add provider-level tests if your API depends on a specific subscription provider. Cover topic naming, payload serialization, filtering rules, retries, and delivery guarantees that belong to the provider integration.

Use host or transport tests for the WebSocket boundary. Cover the protocol your clients use, authentication during connection, connection initialization payloads, start and stop messages, event delivery order for a controlled stream, error messages, and connection close behavior. Focus these tests on message flow and host configuration, leaving resolver payload details to executor tests.

# When Resolver and Data Tests Add Value

Direct resolver tests are helpful when the resolver contains meaningful logic, such as:

- Branching by input
- Mapping between domain models and GraphQL payloads
- Normalizing arguments
- Choosing a domain outcome
- Coordinating a service call where the decision belongs in the resolver

Service and repository tests are often better for data rules than GraphQL-level tests. If a resolver forwards arguments to a service, an operation test plus service tests usually provide stronger coverage than many direct resolver tests.

Test DataLoader or batching behavior where batching is observable. If you need to prove a query avoids repeated loads, use an operation test with a fake data service that records requested keys, or test the DataLoader class directly. For more, see [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/).

Direct resolver tests do not prove schema binding, field middleware, nullability, filtering, paging, projections, authorization, or result serialization. Use operation tests through `IRequestExecutor` for those features.

# Prove Protected Operations Fail for the Wrong Caller

Authorization is executable behavior. Schema metadata alone is not enough.

For each protected field or mutation, test the caller states clients may encounter:

| Caller | Expected result |
| --- | --- |
| Allowed caller | Receives the expected `data` |
| Unauthenticated caller | Receives no protected data and an expected GraphQL error or transport challenge, depending on where authentication fails |
| Authenticated but forbidden caller | Receives no protected data and the expected GraphQL error shape |

Use an executor test when you can build the authorization context in process and the behavior belongs to GraphQL authorization. Use a host integration test when claims, authentication schemes, headers, cookies, or ASP.NET Core middleware determine the caller.

Do not assume a GraphQL authorization failure behaves like a REST `403` response. A GraphQL request may reach execution and return a response envelope with `data`, `errors`, and an HTTP status that reflects transport success. Transport failures, content negotiation, and malformed requests are covered in [HTTP transport](/docs/hotchocolate/v16/server/http-transport/).

For setup, policies, packages, and middleware order, see [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) and [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/).

# Add Guardrail Tests for Unsafe Operations

GraphQL gives clients flexibility in shaping requests. This power requires production guardrails, and those guardrails should be tested.

Add tests for the controls you configure:

| Guardrail | Test idea | Expected behavior |
| --- | --- | --- |
| Parser limits | Send a document that exceeds configured field, token, directive, or recursion limits | Request is rejected before validation or execution work continues |
| Execution depth | Execute an operation deeper than the configured maximum | Validation fails and resolvers for the operation do not run |
| Cost analysis | Execute a document that exceeds field or type cost limits | Validation rejects the request before expensive resolver work |
| Trusted or persisted operations | Send an unknown operation when the server only accepts trusted documents | Request is rejected and no side effect occurs |
| Authorization | Run a protected query or mutation as the wrong caller | Protected data is absent and the expected error is returned |

Keep these tests close to production registration. A guardrail test is only valuable if it uses the same limits as the server.

See [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) for parser and validation limits, [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) for cost configuration, and [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) for trusted document deployment.

# Add Nitro checks for team and CI confidence

Local tests are the fast baseline. Keep the CookieCrumble schema snapshot, representative operation tests, authorization tests, and guardrail tests in the repository.

Use Nitro when the question crosses repository, client, or deployment boundaries.

| Testing task | Nitro capability |
| --- | --- |
| Review schema compatibility outside one branch or one machine. | [Schema registry](/docs/nitro/apis/schema-registry/) and [schema CLI commands](/docs/nitro/cli-commands/schema/). |
| Prove registered client operations still validate against the proposed schema. | [Client registry](/docs/nitro/apis/client-registry/) and [client CLI commands](/docs/nitro/cli-commands/client/). |
| Enforce release policy around schema and client compatibility. | [Deployment approvals](/docs/nitro/apis/deployments/). |
| Choose high-value regression tests from real traffic. | [Operation reporting](/docs/nitro/apis/operation-reporting/). |
| Share reproducible manual checks with variables and endpoint settings. | [Operations](/docs/nitro/documents/operations/), [connection settings](/docs/nitro/documents/connection-settings/), and [environments](/docs/nitro/environments/). |

A typical CI flow looks like this:

1. Run local tests with CookieCrumble schema snapshots and representative operation tests.
2. Publish or check the proposed schema with Nitro.
3. Validate registered client operations against the schema.
4. Apply deployment policy.
5. Use operation reporting to decide which production operations should become local regression tests next.

Nitro does not replace unit, executor, host integration, or snapshot tests. It adds shared contract and release confidence around them.

# Use Snapshots as Review Tools

Snapshots make changes visible, but do not remove the need to understand the diff.

Use CookieCrumble snapshots for:

- Schema SDL contract review
- Stable nested GraphQL results
- Small inline expectations that stay readable beside the test

Use targeted assertions for values that change across runs:

- Generated IDs
- Timestamps
- Unordered lists
- Localized text
- Environment-specific data
- Fields that depend on current identity or external services

For a small stable response, inline snapshots keep the expectation beside the test:

```csharp
result.MatchInlineSnapshot(
    """
    {
      "data": {
        "productById": {
          "id": "1",
          "name": "Chai"
        }
      }
    }
    """);
```

For large schema SDL and larger operation results, file snapshots are easier to review. Commit snapshot files with the tests. When a snapshot fails in a pull request, read the diff, explain the contract or response change, and update the snapshot only when the new output is correct.

# Build your first GraphQL API test plan

Start with this plan for a small API:

1. Add one CookieCrumble schema snapshot. Treat it as required.
2. Add operation tests for the most important query and mutation.
3. Add focused resolver, service, repository, or DataLoader tests where real logic lives.
4. Add authorization tests for protected operations.
5. Add guardrail tests for configured limits and trusted operation behavior.
6. Add Nitro schema and client registry checks when the API is shared across teams or deployment environments.

Use these review questions:

- What schema changes should require human review?
- Which operations are business-critical?
- Which fields or mutations require authorization proof?
- Which limits protect production from deep, broad, costly, or untrusted operations?
- Which schema or client compatibility checks need a registry or deployment policy?

Grow the suite as the schema and production risk grow. A small internal API may start with a schema snapshot, two operation tests, and a service test. A public API should add host-level authentication tests, guardrail tests, client operation validation, and release checks.

# Troubleshoot Common Testing Failures

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Schema snapshot fails after adding, renaming, or removing a field | Intentional contract change or accidental schema drift | Review the SDL diff. Update the snapshot only when the public API change is intended. |
| Operation test returns `errors` with null `data` | Validation error, resolver exception, authorization failure, or non-null propagation | Inspect error code, path, and message. Use [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) to decide the expected shape. |
| Authorization test passes with an executor but fails through HTTP | Claims, headers, cookies, authentication middleware, or host configuration differs from the in-process setup | Move the case to a host integration test or align the executor context with production behavior. |
| Snapshot diffs change on every run | Timestamps, generated IDs, unordered data, or environment-specific values are captured | Stabilize the test data and ordering, or replace that part of the snapshot with targeted assertions. |
| Guardrail test executes resolver work instead of rejecting the request | The limit is not configured in the test host, the operation does not exceed the threshold, or the wrong guardrail is under test | Mirror production registration and craft an operation that exceeds the configured rule. |
| CI tests pass, but a client breaks after a schema change | Local tests did not include that client's operation or compatibility policy | Add Nitro client registry validation, then decide whether the operation should also become a local regression test. |
| Direct resolver test passes, but the operation test fails | Schema binding, middleware, dependency injection, filtering, paging, projections, nullability, or authorization participates only during GraphQL execution | Keep direct tests focused on plain logic and cover GraphQL behavior through the executor or host path. |

# Go Deeper When You Need Implementation Details

- For step-by-step test setup, see the [Testing guide](/docs/hotchocolate/v16/guides/testing/).
- For executor tests and CookieCrumble APIs, see the [Testing guide](/docs/hotchocolate/v16/guides/testing/).
- For schema change review, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/).
- For null propagation and partial data, see [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).
- For connecting tests to operation documents, clients, caches, and trusted documents, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/), [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/), and [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/).
- For production limits, see [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/).
- For host boundary testing, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and Microsoft's [`WebApplicationFactory` integration testing guidance](https://learn.microsoft.com/aspnet/core/test/integration-tests).
- For team-level contract checks, use Nitro [schema registry](/docs/nitro/apis/schema-registry/), [client registry](/docs/nitro/apis/client-registry/), [deployment approvals](/docs/nitro/apis/deployments/), and [operation reporting](/docs/nitro/apis/operation-reporting/).
