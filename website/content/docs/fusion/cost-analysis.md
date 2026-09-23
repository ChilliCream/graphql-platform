---
title: Cost Analysis
description: "Configure Fusion cost analysis, limits, reporting, and pre-plan enforcement."
---

Fusion estimates an operation's field cost and type cost before operation planning. When maximum response size is enabled, it also estimates the response-field count. The analyzer compiles and caches a cost plan, then evaluates that plan against the request's coerced variables.

Cost metadata comes from the public `@cost` and `@listSize` directives in the execution schema. Fusion composition derives these directives from source-schema metadata as described in [Cost Metadata Derivation](./composition.md#cost-metadata-derivation).

# How Cost Is Calculated

The analyzer produces these metrics:

- **Field cost** represents resolver and input-processing work.
- **Type cost** represents the weighted objects in the response.
- **Maximum response size** represents the maximum number of response-object fields when `MaxResponseSize` is enabled.

Weights come from the public `@cost` directive that composition derives for each coordinate. An unannotated coordinate falls back to its default weight; see [Cost Metadata Derivation](./composition.md#cost-metadata-derivation) for the default per coordinate kind.

## Field Cost Example

Walk a query for a `book` field weighted `10`, returning a `title` and an `author`:

```graphql
{
  book {
    # 10 (weight: "10")
    title # 0 (scalar, unannotated)
    author {
      # 1 (object field, unannotated)
      name # 0 (scalar, unannotated)
    }
  }
}
# Field cost: 10 + 0 + 1 + 0 = 11
```

Add pagination and the weight of every field below a sized field multiplies by the list size. This `books` field is weighted `10` and carries `@listSize(assumedSize: 50, slicingArguments: ["first", "last"], slicingArgumentDefaultValue: 10, sizedFields: ["edges", "nodes"])`, evaluated with `first: 50`:

```graphql
{
  books(first: 50) {
    # 10 (weight: "10")
    edges {
      # 1 (object field, paid once)
      node {
        # 1 x 50 (object field, once per edge)
        title # 0 x 50 (scalar)
        author {
          # 1 x 50 (object field, once per node)
          name # 0 x 50 (scalar)
        }
      }
    }
  }
}
# Field cost: 10 + 1 + 1 x 50 + 0 x 50 + 1 x 50 + 0 x 50 = 111
```

## Type Cost Example

Type cost counts the weighted objects the response instantiates. The same paginated query instantiates one `BooksConnection`, fifty `BooksEdge` objects, fifty `Book` objects, and fifty `Author` objects, on top of the root `Query` object:

```graphql
{
  # 1 Query
  books(first: 50) {
    # 1 BooksConnection
    edges {
      # 50 BooksEdges
      node {
        # 50 Books
        title
        author {
          # 50 Authors
          name
        }
      }
    }
  }
}
# Type cost: 1 + 1 + 50 + 50 + 50 = 152
```

The examples use this source schema:

```graphql
directive @cost(
  weight: String!
) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
directive @listSize(
  assumedSize: Int
  slicingArguments: [String!]
  sizedFields: [String!]
  requireOneSlicingArgument: Boolean = true
  slicingArgumentDefaultValue: Int
) on FIELD_DEFINITION

type Query {
  book: Book @cost(weight: "10")
  books(first: Int, last: Int): BooksConnection
    @cost(weight: "10")
    @listSize(
      assumedSize: 50
      slicingArguments: ["first", "last"]
      slicingArgumentDefaultValue: 10
      sizedFields: ["edges", "nodes"]
    )
}

type Book {
  title: String
  author: Author
}

type Author {
  name: String
}

type BooksConnection {
  edges: [BooksEdge]
  nodes: [Book]
}

type BooksEdge {
  node: Book
}
```

## Pricing Rules

- Complementary `@include` and `@skip` branches are mutually exclusive.
- Fields are collected by response name before signed weights are applied, and clamping to zero happens after the field-call sum and the per-instance type sum are calculated.
- An interface or union return weight is the signed maximum of its member object-type weights.
- A field selected through an interface is priced through each possible object type's field metadata.
- Costs on arguments of directives used in the operation contribute to field cost.
- Negative slicing-argument values clamp to `0`. A slicing value of `0` remains `0`, and the field-call cost is still paid once.

# List Size

The analyzer selects the assumed size for a list field in this order:

1. An inherited size from a parent `@listSize(sizedFields:)` annotation. The inherited size takes priority over the child field's own `@listSize`.
2. The maximum slicing-argument value present after coercion, including a schema argument default.
3. `slicingArgumentDefaultValue`, only when no slicing argument is present.
4. `assumedSize`.
5. The default list size configured at composition time. Without a composed default, the list is unbounded.

An explicit `null` slicing argument is not a slicing value and suppresses that argument's schema default. An undefined slicing variable behaves as an absent argument, so a schema default can apply before the remaining fallbacks. See [Default List Size](./composition.md#default-list-size) for configuring the composed default.

# Default Enforcement

`AddGraphQLGatewayServer()` enables cost enforcement in every hosting environment with these limits:

- Maximum field cost: `1,000`
- Maximum type cost: `10,000`

This environment-independent behavior differs from the default introspection rule, which permits introspection in Development.

Passing `disableDefaultSecurity: true` disables cost enforcement. Cost analysis and the `GraphQL-Cost` reporting modes remain available.

```csharp
builder.Services.AddGraphQLGatewayServer(
    disableDefaultSecurity: true);
```

Set `EnforceCostLimits` to `false` when only cost enforcement should be disabled:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyCostOptions(options => options.EnforceCostLimits = false);
```

Set `SkipAnalyzer` to `true` to bypass analysis, enforcement, and reporting.

# Reporting and Validation

Send the `GraphQL-Cost` HTTP request header to obtain cost metrics:

| Header value | Execution | Reported value                             |
| ------------ | --------- | ------------------------------------------ |
| `report`     | Yes       | Evaluated cost for the supplied variables. |
| `validate`   | No        | Evaluated cost for the supplied variables. |

`validate` requires the variables the operation declares, exactly like `execute` and `report`. Without them, the request fails with the ordinary variable coercion error; the assumed bound is not exposed through the request pipeline. With variables, `validate` returns an extensions-only response with HTTP status `200`, including when the reported values exceed configured limits. A variable batch returns one extensions-only result per variable set, with that set's `operationCost`. A successful variable batch in `report` mode also includes one `operationCost` per result.

```json
{
  "extensions": {
    "operationCost": {
      "fieldCost": 11,
      "typeCost": 5
    }
  }
}
```

When `MaxResponseSize` is enabled, `operationCost` also contains `maxResponseSize`. Positive infinite values are serialized as the string `"Infinity"`.

# Rejections and HTTP Status

Cost-limit failures use error code `HC0047`. The error extensions identify the failed limit:

| Limit                 | Error extension keys                        |
| --------------------- | ------------------------------------------- |
| Field cost            | `fieldCost`, `maxFieldCost`                 |
| Type cost             | `typeCost`, `maxTypeCost`                   |
| Maximum response size | `maxResponseSize`, `maxAllowedResponseSize` |

```json
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "typeCost": 2,
        "maxTypeCost": 1
      }
    }
  ]
}
```

The HTTP status depends on the accepted response media type:

| `Accept` media type                 | Rejection status |
| ----------------------------------- | ---------------- |
| `application/graphql-response+json` | `400`            |
| `application/json`                  | `200`            |

In `report` mode, a rejected response also contains `extensions.operationCost`. For a rejected variable batch, the single rejection result contains one `operationCost`: its `fieldCost` and `typeCost` are the sums across all variable sets, and it contains `maxResponseSize` only for a response-size rejection, using the first violating set's value.

One request is one invocation of the request pipeline, and a variable batch is one request. Cost enforcement sums the field cost and type cost across all variable sets and compares those sums with `MaxFieldCost` and `MaxTypeCost`. If either sum exceeds its limit, the whole request is rejected before any variable set executes, with one `HC0047` result and the HTTP status shown above. Maximum response size remains enforced per variable set. `MaxResponseSize` is checked per variable set, not summed. If any set exceeds `MaxResponseSize`, the whole request is likewise rejected before any set executes, with one `HC0047` result. When multiple sets violate this limit, the first violating set determines the reported `maxResponseSize`.

Request batching is an array of independent requests in one HTTP request. Cost limits currently apply separately to each independent request in a request batch. Summing costs across an entire request batch is planned, with no target version.

# Pipeline Placement

The predefined Fusion pipelines run these stages in order:

1. Document Cache
2. Document Parser
3. Document Validation
4. Operation Variable Coercion
5. Cost Analysis
6. Operation Plan Cache
7. Operation Plan
8. Skip Warmup Execution
9. Concurrency Gate
10. Operation Execution

Cost enforcement runs before the operation-plan cache and before operation planning. A rejected request never enters the planner's single-flight coalescing and does not create an operation-plan cache entry or an in-flight planning entry. Document normalization, i.e. expanding fragments and removing statically excluded selections, is not a pipeline stage; it is resolved once per request by the first stage that needs it, which is variable coercion, cached by operation id and rewritten only on a cache miss, and cost analysis and operation planning then read it from the operation document info instead of re-normalizing it. Cost enforcement is per request and reflects the coerced variable values; limits on operation structure, such as maximum depth, node count, and parser limits, remain the protection against operations that are expensive to plan regardless of their variables.

A custom pipeline must place `UseOperationVariableCoercion()` before `UseCostAnalysis()`, and place `UseCostAnalysis()` before `UseOperationPlanCache()`.

# Options Reference

Configure `FusionCostOptions` with `ModifyCostOptions`:

| Option                       | Type                          | Default  | Contract                                                                                                                                       |
| ---------------------------- | ----------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxFieldCost`               | `double`                      | `1,000`  | Maximum allowed field cost. Valid range: non-negative finite values or `Infinity`.                                                             |
| `MaxTypeCost`                | `double`                      | `10,000` | Maximum allowed type cost. Valid range: non-negative finite values or `Infinity`.                                                              |
| `EnforceCostLimits`          | `bool`                        | `true`   | Enforces the field, type, and response-size limits.                                                                                            |
| `SkipAnalyzer`               | `bool`                        | `false`  | Skips analysis, enforcement, and reporting.                                                                                                    |
| `MaxResponseSize`            | `double?`                     | `null`   | Maximum estimated response-field count. `null` disables this check and metric. Valid range: `null`, non-negative finite values, or `Infinity`. |
| `CostPlanCacheSize`          | `int`                         | `256`    | Maximum compiled cost plans cached per schema.                                                                                                 |
| `CaseBudget`                 | `int?`                        | `null`   | Exact cases compiled per operation before `CaseBudgetExceededBehavior` decides the fallback. `null` uses the default (510).                    |
| `CaseBudgetExceededBehavior` | `CaseBudgetExceededBehavior?` | `null`   | Behavior once compiling one operation exhausts `CaseBudget`. `null` uses the default (`EvaluatePerRequest`).                                   |

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.MaxResponseSize = 10_000;
        options.CostPlanCacheSize = 512;
    });
```

An operation whose exact compile would exceed `CaseBudget` is, by default (`CaseBudgetExceededBehavior.EvaluatePerRequest`), evaluated exactly per request instead of from a compiled plan: slower for that operation, but never over-estimated. `CaseBudgetExceededBehavior.Overestimate` restores the compiled, sound over-estimate for the unaffordable remainder instead.

> **Note:** The default list size from [List Size](#list-size) step 5 is not a gateway runtime option. It is a composition setting (`SourceSchemaMergerOptions.DefaultListSize`) carried into the execution schema by the `@fusion__cost_options(defaultListSize:)` directive, and the gateway reads it from there. See [Composition](./composition.md) for details.

# Per-Request Cost Options

`FusionCostOptions` apply to every request by default, but a gateway can loosen or tighten those limits for a particular request or user by attaching a `FusionRequestCostOptions` to the request. A value set on the request replaces the corresponding gateway value for that request, in either direction: a request can raise a limit above the gateway default or lower it below the gateway default. Attached request options replace all of the gateway's limits for that request, and a `null` `MaxResponseSize` on the request means that request has no response-size limit.

The typical place to do this is an `IHttpRequestInterceptor`. Its `OnCreateAsync` method runs for every HTTP request and has access to the authenticated user, letting it pick request options based on group membership before the operation executes:

```csharp
public class CostOptionsHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.IsInRole("developer"))
        {
            requestBuilder.SetCostOptions(
                new FusionRequestCostOptions(
                    maxFieldCost: 5_000,
                    maxTypeCost: 5_000,
                    enforceCostLimits: true,
                    skipAnalyzer: false,
                    maxResponseSize: 50_000));
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyCostOptions(options => options.MaxResponseSize = 10_000)
    .AddHttpRequestInterceptor<CostOptionsHttpRequestInterceptor>();
```

Here, requests from the `developer` role get a higher `MaxResponseSize` (and higher field/type cost limits) than the gateway's configured limits, while every other request keeps enforcing the gateway's configured limits.

Response-size analysis itself is an opt-in that is only ever enabled per gateway, by setting `FusionCostOptions.MaxResponseSize`. A request cannot turn the analysis on: if the gateway leaves `MaxResponseSize` unset (`null`) and a request nonetheless sets `FusionRequestCostOptions.MaxResponseSize`, the request fails fast with error code `HC0062` and the message:

> The request cost options set MaxResponseSize, but the schema does not enable the response-size analysis.

To enable the check, set `FusionCostOptions.MaxResponseSize` on the gateway first; requests may then raise or lower it as needed.

Setting `FusionRequestCostOptions.SkipAnalyzer` for a request bypasses the cost analyzer entirely for that request. A `MaxResponseSize` set on the same request is then ignored.

# Accessing the Analysis Result

`RequestContext.TryGetCostAnalysisResult` provides the compiled `CostPlan` and every estimate for the request. Read the result after the cost middleware has completed:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .UseRequest(
        next => async context =>
        {
            await next(context);

            if (context.TryGetCostAnalysisResult(out var result))
            {
                CostPlan plan = result.Plan;
                IReadOnlyList<CostEstimate> estimates = result.Estimates;
            }
        },
        key: "ReadCostAnalysisResult",
        before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

Cost analysis and reporting return `HC0048` when required operation or document state is missing, when a request reaches the analyzer with zero coerced variable sets (an explicit empty variable batch, `variables: []`), or when metrics cannot be attached to the execution-result state. This applies to `execute`, `report`, and `validate` mode alike.

# Next Steps

- [Request Limits](./request-limits.md)
- [Composition](./composition.md#cost-metadata-derivation)
- [Directive Reference](./directives-reference.md)
