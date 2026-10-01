---
title: Cost Analysis
description: "Guard a public Hot Chocolate API with cost analysis: the @cost and @listSize directives assign weights so overly expensive queries are rejected before execution."
---

If you expose a GraphQL API to the public internet, you cannot predict what queries clients will send. A single deeply nested query requesting thousands of nodes can bring your server to its knees. Cost analysis prevents this by calculating the cost of an operation before executing it and rejecting operations that exceed your budget.

Hot Chocolate implements the draft [IBM Cost Analysis specification](https://ibm.github.io/graphql-specs/cost-spec.html). It assigns weights to fields and estimates list sizes, then computes two metrics: **field cost** (execution impact) and **type cost** (data impact). Operations that exceed either limit are rejected before any resolver runs. The analyzer compiles an operation into a cost plan, caches that plan, and evaluates it against each request's coerced variables.

# Why This Matters for Public APIs

With REST, each endpoint has a predictable cost. You know that `GET /users` returns a page of users and takes a roughly constant amount of server time. With GraphQL, a client can construct a query that fans out across relationships:

```graphql
query {
  users(first: 50) {
    edges {
      node {
        orders(first: 50) {
          edges {
            node {
              items(first: 50) {
                edges {
                  node {
                    product {
                      reviews(first: 50) {
                        edges {
                          node {
                            author {
                              name
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

This query requests up to 50 x 50 x 50 x 50 = 6,250,000 review nodes. Without cost analysis, the server would attempt to resolve all of them.

Cost analysis evaluates the operation before execution and rejects it before it consumes resources.

# How Cost Is Calculated

The analyzer produces these metrics:

- **Field cost** represents resolver and input-processing work.
- **Type cost** represents the weighted objects in the response.
- **Maximum response size** represents the maximum number of response-object fields when `MaxResponseSize` is enabled.

The default cost weights are:

| Schema element                                      | Default weight |
| --------------------------------------------------- | -------------- |
| Scalar and enum types                               | `0`            |
| Object, interface, and union types                  | `1`            |
| Fields returning a scalar or enum                   | `0`            |
| Fields returning an object, interface, or union     | `1`            |
| Output fields returning a list of scalars or enums  | `0`            |
| Arguments and input fields with leaf values         | `0`            |
| Arguments and input fields with input-object values | `1`            |
| Fields without a pure resolver                      | `10`           |

The resolver weight is written as an explicit `@cost` directive when `ApplyCostDefaults` is enabled. A field with no explicit `@cost` directive weighs `1` when its return type is an object, interface, or union, and `0` otherwise. A field without a pure resolver instead receives an explicit `@cost` directive worth `DefaultResolverCost`; its return type keeps its own weight, which counts only toward type cost.

## Field Cost Example

Walk a query for a `book` field without a pure resolver, returning a `title` and an `author`:

```graphql
{
  book {
    # 10 (field without a pure resolver)
    title # 0 (scalar)
    author {
      # 1 (field returning an object)
      name # 0 (scalar)
    }
  }
}
# Field cost: 10 + 0 + 1 + 0 = 11
```

Add pagination and the weight of every field below the list multiplies by the list size. This `books` field returns a `BooksConnection` generated with the defaults, evaluated with `first: 50`:

```graphql
{
  books(first: 50) {
    # 10 (field without a pure resolver)
    edges {
      # 1 (field returning an object, paid once)
      node {
        # 1 x 50 (field returning an object, once per edge)
        title # 0 x 50 (scalar)
        author {
          # 1 x 50 (field returning an object, once per node)
          name # 0 x 50 (scalar)
        }
      }
    }
  }
}
# Field cost: 10 + 1 + 1 x 50 + 0 x 50 + 1 x 50 + 0 x 50 = 111
```

The same selection without `first` evaluates at `DefaultPageSize`, and a list field with no inherited or explicit `@listSize` annotation evaluates at `DefaultListSize`, which defaults to `50`.

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

Compare these totals against `MaxFieldCost` (`1_000` by default) and `MaxTypeCost` (`10_000` by default): an operation whose evaluated field cost or type cost exceeds its limit is rejected with error code `HC0047`, shown under [Rejections and HTTP Status](#rejections-and-http-status).

## Variable-Aware Evaluation

The evaluated cost uses coerced values for slicing arguments, `@skip` and `@include` conditions, and input objects. Fields with the same response name are collected before their weights are applied.

For a paginated `books` field generated with the defaults, the schema includes this metadata:

```graphql
books(first: Int, last: Int): BooksConnection
  @listSize(
    assumedSize: 50
    slicingArguments: ["first", "last"]
    slicingArgumentDefaultValue: 10
    sizedFields: ["edges", "nodes"]
    requireOneSlicingArgument: false
  )
  @cost(weight: "10")
```

This operation is evaluated with `first = 3`:

```graphql
query GetBooks($first: Int) {
  books(first: $first) {
    nodes {
      title
    }
  }
}
```

The evaluated field cost is `11` and the type cost is `5`. The same selection without `first` uses `DefaultPageSize = 10`, producing field cost `11` and type cost `12`.

`GraphQL-Cost: validate` always coerces variables, matching `execute`/`report`. An optional variable that is not supplied behaves like an absent argument, so the operation above without `$first` reports the same field cost `11` and type cost `12` shown for the argument-less selection. A required variable (`$first: Int!`) that is not supplied fails the request with the ordinary variable-coercion error; the assumed bound is not exposed through the request pipeline.

# List Size

The analyzer selects a list size in this order:

1. An inherited size from a parent `@listSize(sizedFields:)` annotation.
2. The maximum slicing-argument value present after coercion. Negative values become `0`, and `0` remains `0`.
3. `slicingArgumentDefaultValue`, only when no slicing argument is present.
4. `assumedSize`.
5. `CostOptions.DefaultListSize`, which defaults to `50` (`PagingDefaults.MaxPageSize`).

An annotation with `sizedFields` applies its selected size to the named direct child fields. The inherited size takes priority over a child's own `@listSize` annotation.

Hot Chocolate paging writes `MaxPageSize` to `assumedSize` and `DefaultPageSize` to `slicingArgumentDefaultValue`. A supplied paging argument is evaluated at its coerced value, an argument-less request is evaluated at `DefaultPageSize`, and `MaxPageSize` remains the assumed bound for a variable-bound paging argument. An explicit `null` is not a slicing value and suppresses the argument's schema default. An undefined variable behaves as an absent argument, so a schema default can apply before the remaining fallbacks.

An unannotated list falls through to `DefaultListSize`. With the default of `50`, a nested chain of unannotated lists can still exceed a finite type-cost limit, since the assumed size compounds at every nesting level. Annotate the field with `@listSize(assumedSize:)`, or set `DefaultListSize` for the schema with `ModifyCostOptions` to any non-negative finite number or `double.PositiveInfinity`.

## Requiring a Slicing Argument

The `@listSize` definition defaults `requireOneSlicingArgument` to `true`. A schema-first directive usage that omits the argument requires exactly one non-null literal slicing argument. Explicit nulls do not count. Static validation is skipped when every non-null slicing argument is variable-bound because presence is not yet known. Otherwise, zero or multiple non-null slicing arguments return error code `HC0082` with the message `Exactly one slicing argument must be defined.`

Hot Chocolate paging writes `requireOneSlicingArgument: false` by default. Set `RequirePagingBoundaries` to require a boundary:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(options => options.RequirePagingBoundaries = true);
```

# Applying Cost Metadata

## Cost Weights

Set a field weight with the `[Cost]` attribute or the descriptor API:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class BookQueries
{
    [Cost(100)]
    public static async Task<Book?> GetBookAsync(
        int id,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Books.FindAsync([id], cancellationToken);
}
```

</Implementation>
<Code>

```csharp
public class BookQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("book")
            .Resolve(_ => new Book("C# in depth"))
            .Cost(100);
    }
}
```

</Code>
</ExampleTabs>

## List Size Metadata

Set list-size metadata with the `[ListSize]` attribute or the descriptor API:

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class BookQueries
{
    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["first", "last"],
        RequireOneSlicingArgument = false,
        SlicingArgumentDefaultValue = 10)]
    public static IEnumerable<Book> GetBooks(int? first, int? last)
        => [];
}
```

</Implementation>
<Code>

```csharp
public class BookQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("books")
            .Argument("first", argument => argument.Type<IntType>())
            .Argument("last", argument => argument.Type<IntType>())
            .Resolve(_ => Array.Empty<Book>())
            .ListSize(
                assumedSize: 100,
                slicingArguments: ["first", "last"],
                requireOneSlicingArgument: false,
                slicingArgumentDefaultValue: 10);
    }
}
```

</Code>
</ExampleTabs>

# Reporting and Validation

Send the `GraphQL-Cost` HTTP request header to inspect operation cost:

| Header value | Execution | Reported value                                                        |
| ------------ | --------- | --------------------------------------------------------------------- |
| `report`     | Yes       | Evaluated cost for the supplied variables.                            |
| `validate`   | No        | Evaluated cost for the coerced variables, same coercion as `execute`. |

`validate` returns an extensions-only response with HTTP status `200`, including when the reported values exceed configured limits. A variable batch returns one extensions-only result per variable set, with that set's `operationCost`. A successful variable batch in `report` mode also includes one `operationCost` per result.

The response contains the values used by enforcement:

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

## Accessing Costs in Code

The cost accessors are extensions on `RequestContext`. Read them after the cost middleware has completed:

```csharp
builder
    .AddGraphQL()
    .UseRequest(
        next => async context =>
        {
            await next(context);

            CostMetrics firstEstimate = context.GetCostMetrics();

            if (context.TryGetCostAnalysisResult(out var result))
            {
                CostPlan plan = result.Plan;
                IReadOnlyList<CostEstimate> estimates = result.Estimates;
            }
        },
        key: "ReadCostAnalysisResult",
        before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

`GetCostMetrics()` returns the first evaluated set. `TryGetCostAnalysisResult` exposes the compiled `CostPlan` and every estimate in a variable batch.

# Rejections and HTTP Status

Cost analysis runs after variable coercion and before the operation compiler, so a rejected request is never compiled and never enters the operation cache. Enforcement is per request and reflects the coerced variable values. Limits on operation structure, such as maximum depth, node count, and parser limits, remain the protection against operations that are expensive to compile regardless of their variables; see [Request Limits](./request-limits.md).

Cost-limit failures use error code `HC0047`. The error extensions identify the failed limit:

| Limit                 | Error extension keys                        |
| --------------------- | ------------------------------------------- |
| Field cost            | `fieldCost`, `maxFieldCost`                 |
| Type cost             | `typeCost`, `maxTypeCost`                   |
| Maximum response size | `maxResponseSize`, `maxAllowedResponseSize` |

For example, a type-cost rejection has this body:

```json
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 1,
        "typeCost": 2
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

In `report` mode, a rejected response also contains `extensions.operationCost` with the evaluated values. For a rejected variable batch, the single rejection result contains one `operationCost`: its `fieldCost` and `typeCost` are the sums across all variable sets, and it contains `maxResponseSize` only for a response-size rejection, using the first violating set's value.

One request is one invocation of the request pipeline, and a variable batch is one request. Cost enforcement sums the field cost and type cost across all variable sets and compares those sums with `MaxFieldCost` and `MaxTypeCost`. If either sum exceeds its limit, the whole request is rejected before any variable set executes, with one `HC0047` result and the HTTP status shown above. `MaxResponseSize` is checked per variable set, not summed. If any set exceeds `MaxResponseSize`, the whole request is likewise rejected before any set executes, with one `HC0047` result. When multiple sets violate this limit, the first violating set determines the reported `maxResponseSize`.

Summing the costs prevents a client from splitting an expensive workload among variable sets that each stay under the limit. Request batching is an array of independent requests in one HTTP request. Cost limits currently apply separately to each independent request in a request batch. Summing costs across an entire request batch is planned, with no target version.

# Tuning Guide

## Start with Defaults

The defaults (`MaxFieldCost = 1_000`, `MaxTypeCost = 10_000`) work for many schemas. Deploy with defaults first and observe which operations are rejected.

## Measure Real Operations

Use the `GraphQL-Cost: report` header to measure the cost of your actual client operations, or `GraphQL-Cost: validate` to measure without executing them. Send representative operations from your client applications and review their costs before changing limits.

## Adjust MaxFieldCost and MaxTypeCost

Increase the limits if legitimate operations are rejected. Decrease them if you want tighter protection. The right values depend on your infrastructure and acceptable load.

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
    });
```

## Assign Custom Weights to Expensive Fields

If a resolver calls an external API or runs an expensive query, increase its cost weight:

```csharp
[Cost(50)]
public static async Task<Report> GetReportAsync(/* ... */)
```

## Bound Nested Lists

Cost multiplies at every list nesting level, so the page size of a nested connection has a far larger effect than the page size of the root connection. Lower `MaxPageSize` on nested paginated fields, and annotate unpaginated lists with `@listSize(assumedSize:)` so they do not fall through to `DefaultListSize`.

## Use RequirePagingBoundaries

Force clients to specify `first` or `last` on paginated fields. An argument-less request is otherwise evaluated at `DefaultPageSize`, so requiring a boundary makes the evaluated cost of every paginated request explicit rather than dependent on a schema default:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(options => options.RequirePagingBoundaries = true);
```

# Real-World Example

Consider a product catalog API with this schema. Both `products` and `reviews` are paginated fields without a pure resolver, so each carries `#!sdl @cost(weight: "10")`, and `MaxPageSize` is `50`:

```graphql
type Query {
  products(first: Int, after: String): ProductsConnection
}

type Product {
  name: String
  reviews(first: Int, after: String): ReviewsConnection
}

type Review {
  text: String
  author: User
}

type User {
  name: String
}
```

A client sends this operation:

```graphql
{
  products(first: 50) {
    edges {
      node {
        name
        reviews(first: 50) {
          edges {
            node {
              text
              author {
                name
              }
            }
          }
        }
      }
    }
  }
}
```

The field cost is:

| Field                 | Cost                  |
| --------------------- | --------------------- |
| `products`            | `10`                  |
| `products.edges`      | `1`                   |
| `products.edges.node` | `1 x 50 = 50`         |
| `reviews`             | `10 x 50 = 500`       |
| `reviews.edges`       | `1 x 50 = 50`         |
| `reviews.edges.node`  | `1 x 50 x 50 = 2 500` |
| `author`              | `1 x 50 x 50 = 2 500` |
| **Total**             | **`5 611`**           |

The type cost is one `Query`, one `ProductsConnection`, fifty `ProductsEdge` and fifty `Product` objects, fifty `ReviewsConnection` objects, and 2,500 each of `ReviewsEdge`, `Review`, and `User`, for a total of `7 652`.

With the default limits, the type cost of `7 652` is within `MaxTypeCost`, but the field cost of `5 611` exceeds `MaxFieldCost` and the operation is rejected with `HC0047`. Lowering `MaxPageSize` for the nested `reviews` field is the most effective change, because every field below it multiplies by that size:

```csharp
[UsePaging(MaxPageSize = 10)]
public static IQueryable<Review> GetReviews([Parent] Product product, CatalogContext db)
    => db.Reviews.Where(r => r.ProductId == product.Id);
```

With `#!graphql reviews(first: 10)`, the field cost drops to `1 611` and the type cost to `1 652`. The field cost is still above the default `MaxFieldCost`, so either raise the limit to `2_000` or lower the `products` page size as well. With both connections at `10`, the field cost is `331` and the type cost is `332`, well within the defaults.

# Options Reference

## Cost Options

| Option                             | Default  | Contract                                                                                                                                                             |
| ---------------------------------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxFieldCost`                     | `1_000`  | Maximum allowed field cost. Valid range: non-negative finite values or `Infinity`.                                                                                   |
| `MaxTypeCost`                      | `10_000` | Maximum allowed type cost. Valid range: non-negative finite values or `Infinity`.                                                                                    |
| `EnforceCostLimits`                | `true`   | Reject operations that exceed a configured limit.                                                                                                                    |
| `SkipAnalyzer`                     | `false`  | Bypass cost analysis and reporting.                                                                                                                                  |
| `ApplyCostDefaults`                | `true`   | Apply Hot Chocolate cost metadata to the schema.                                                                                                                     |
| `ApplySlicingArgumentDefaultValue` | `true`   | Apply the paging default to argument-less evaluated requests.                                                                                                        |
| `DefaultResolverCost`              | `10.0`   | Weight applied to fields without a pure resolver. `null` disables the default.                                                                                       |
| `DefaultListSize`                  | `50`     | Size used for list fields without applicable `@listSize` metadata, sourced from `PagingDefaults.MaxPageSize`. Valid range: non-negative finite values or `Infinity`. |
| `CostPlanCacheSize`                | `256`    | Maximum compiled cost plans cached per schema.                                                                                                                       |
| `MaxResponseSize`                  | `null`   | Maximum response-object-field count. `null` disables the check and metric. Valid range: `null`, non-negative finite values, or `Infinity`.                           |
| `CaseBudget`                       | `null`   | Exact cases compiled per operation before `CaseBudgetExceededBehavior` decides the fallback. `null` uses the default (510).                                          |
| `CaseBudgetExceededBehavior`       | `null`   | Behavior once compiling one operation exhausts `CaseBudget`. `null` uses the default (`EvaluatePerRequest`).                                                         |

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.DefaultListSize = 100;
        options.MaxResponseSize = 10_000;
    });
```

An operation whose exact compile would exceed `CaseBudget` is, by default (`CaseBudgetExceededBehavior.EvaluatePerRequest`), evaluated exactly per request instead of from a compiled plan: slower for that operation, but never over-estimated. `CaseBudgetExceededBehavior.Overestimate` restores the compiled, sound over-estimate for the unaffordable remainder instead.

## Filtering Cost Options

| Option                                | Default | Contract                                  |
| ------------------------------------- | ------- | ----------------------------------------- |
| `DefaultFilterArgumentCost`           | `10.0`  | Weight for a filter argument.             |
| `DefaultFilterOperationCost`          | `10.0`  | Weight for a filter operation.            |
| `DefaultExpensiveFilterOperationCost` | `20.0`  | Weight for an expensive filter operation. |

## Sorting Cost Options

| Option                     | Default | Contract                     |
| -------------------------- | ------- | ---------------------------- |
| `DefaultSortArgumentCost`  | `10.0`  | Weight for a sort argument.  |
| `DefaultSortOperationCost` | `10.0`  | Weight for a sort operation. |

`FilterCostOptions.VariableMultiplier`, `SortCostOptions.VariableMultiplier`, and `RequestCostOptions.FilterVariableMultiplier` are obsolete compile errors. The analyzer prices coerced variable values directly.

# Per-Request Cost Options

The schema-level `CostOptions` apply to every request by default, but an application can loosen or tighten those limits for a particular request or user by attaching a `RequestCostOptions` to the request. A value set on the request replaces the corresponding schema value for that request, in either direction: a request can raise a limit above the schema default, lower it below the schema default, or leave individual options unset to keep inheriting from the schema.

The typical place to do this is an `IHttpRequestInterceptor`. Its `OnCreateAsync` method runs for every HTTP request and already has access to the authenticated user, so it can pick request options based on group membership before the operation executes:

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
                new RequestCostOptions(
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

Here, requests from the `developer` role get a higher `MaxResponseSize` (and higher field/type cost limits) than the schema default, while every other request keeps enforcing the schema's configured limits.

Response-size analysis itself is an opt-in that is only ever enabled per schema, by setting `CostOptions.MaxResponseSize` on the schema. A request cannot turn the analysis on: if the schema leaves `MaxResponseSize` unset (`null`) and a request nonetheless sets `RequestCostOptions.MaxResponseSize`, the request fails fast with error code `HC0062` and the message:

> The request cost options set MaxResponseSize, but the schema does not enable the response-size analysis.

To enable the check, set `CostOptions.MaxResponseSize` on the schema first; requests may then raise or lower it as needed.

Setting `RequestCostOptions.SkipAnalyzer` for a request bypasses the cost analyzer entirely for that request. In that case, a `MaxResponseSize` set on the same request is ignored silently, because the analyzer never runs. This is not the fail-fast case above.

# Disabling Cost Enforcement

If you protect your API through other means, such as trusted documents, set `EnforceCostLimits` to `false` to keep analysis and reporting without rejecting operations:

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options => options.EnforceCostLimits = false);
```

Set `SkipAnalyzer` to `true` only when cost analysis and cost reporting must both be bypassed.

# Next Steps

- **Need to limit operation depth or size?** See [Request Limits](./request-limits.md).
- **Building a private API?** See [Trusted Documents](../performance/trusted-documents.md).
- **Tuning page sizes?** See [Pagination](../fetching-data/pagination.md).
- **Need an overview of security options?** See [Security Overview](./index.md).
