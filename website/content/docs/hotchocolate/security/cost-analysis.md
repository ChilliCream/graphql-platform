---
title: Cost Analysis
description: "Guard a public Hot Chocolate API with cost analysis: the @cost and @listSize directives assign weights so overly expensive queries are rejected before execution."
---

Cost analysis evaluates an operation before execution and rejects it when its configured field-cost, type-cost, or response-size limit is exceeded.

Hot Chocolate implements the draft [IBM Cost Analysis specification](https://ibm.github.io/graphql-specs/cost-spec.html). The analyzer compiles an operation into a cost plan, caches that plan, and evaluates it against each request's coerced variables.

# Cost Metrics

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
| Output fields returning a list of scalars or enums  | `1`            |
| Arguments and input fields with leaf values         | `0`            |
| Arguments and input fields with input-object values | `1`            |
| Fields without a pure resolver                      | `10`           |

The list-of-scalars output weight and the resolver weight are written as explicit `@cost` directives when `ApplyCostDefaults` is enabled.

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

`GraphQL-Cost: validate` without variables reports the static bound. For the variable-bound operation above, the static bound uses `assumedSize = 50`, producing field cost `11` and type cost `52`.

# List Size

The analyzer selects a list size in this order:

1. An inherited size from a parent `@listSize(sizedFields:)` annotation.
2. The maximum slicing-argument value present after coercion. Negative values become `0`, and `0` remains `0`.
3. `slicingArgumentDefaultValue`, only when no slicing argument is present.
4. `assumedSize`.
5. `CostOptions.DefaultListSize`, which defaults to `Infinity`.

An annotation with `sizedFields` applies its selected size to the named direct child fields. The inherited size takes priority over a child's own `@listSize` annotation.

Hot Chocolate paging writes `MaxPageSize` to `assumedSize` and `DefaultPageSize` to `slicingArgumentDefaultValue`. A supplied paging argument is evaluated at its coerced value, an argument-less request is evaluated at `DefaultPageSize`, and `MaxPageSize` remains the static bound for a variable-bound paging argument. An explicit `null` is not a slicing value and suppresses the argument's schema default. An undefined variable behaves as an absent argument, so a schema default can apply before the remaining fallbacks.

An unannotated list falls through to `DefaultListSize`. With the default `Infinity`, a list whose element type has a non-zero weight exceeds any finite type-cost limit. Annotate the field with `@listSize(assumedSize:)` or set a finite `DefaultListSize` for the schema.

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

| Header value | Execution | Reported value                                                                |
| ------------ | --------- | ----------------------------------------------------------------------------- |
| `report`     | Yes       | Evaluated cost for the supplied variables.                                    |
| `validate`   | No        | Evaluated cost with variables, or the static bound when variables are absent. |

`validate` returns an extensions-only response with HTTP status `200`, including when the reported values exceed configured limits. A variable batch reports the cost for each variable set on its corresponding result.

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
                bool isStaticBound = result.IsStaticBound;
            }
        },
        key: "ReadCostAnalysisResult",
        before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

`GetCostMetrics()` returns the first evaluated set. `TryGetCostAnalysisResult` exposes the compiled `CostPlan`, every estimate in a variable batch, and whether the estimates are a static bound.

# Rejections and HTTP Status

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

In `report` mode, a rejected response also contains `extensions.operationCost` with the evaluated values.

# Options Reference

## Cost Options

| Option                             | Default    | Contract                                                                        |
| ---------------------------------- | ---------- | ------------------------------------------------------------------------------- |
| `MaxFieldCost`                     | `1_000`    | Maximum allowed field cost.                                                     |
| `MaxTypeCost`                      | `1_000`    | Maximum allowed type cost.                                                      |
| `EnforceCostLimits`                | `true`     | Reject operations that exceed a configured limit.                               |
| `SkipAnalyzer`                     | `false`    | Bypass cost analysis and reporting.                                             |
| `ApplyCostDefaults`                | `true`     | Apply Hot Chocolate cost metadata to the schema.                                |
| `ApplySlicingArgumentDefaultValue` | `true`     | Apply the paging default to argument-less evaluated requests.                   |
| `DefaultResolverCost`              | `10.0`     | Weight applied to fields without a pure resolver. `null` disables the default.  |
| `DefaultListSize`                  | `Infinity` | Size used for list fields without applicable `@listSize` metadata.              |
| `CostPlanCacheSize`                | `256`      | Maximum compiled cost plans cached per schema.                                  |
| `MaxResponseSize`                  | `null`     | Maximum response-object-field count. `null` disables the check and metric.      |
| `CaseBudget`                       | `null`     | Exact cases evaluated per operation. `null` uses the engine default.            |

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

# Disabling Cost Enforcement

Set `EnforceCostLimits` to `false` to keep analysis and reporting without rejecting operations:

```csharp
builder
    .AddGraphQL()
    .ModifyCostOptions(options => options.EnforceCostLimits = false);
```

Set `SkipAnalyzer` to `true` only when cost analysis and cost reporting must both be bypassed.

# Next Steps

- [Pagination](../fetching-data/pagination.md)
- [Request Limits](./request-limits.md)
- [Trusted Documents](../performance/trusted-documents.md)
- [Security Overview](./index.md)
