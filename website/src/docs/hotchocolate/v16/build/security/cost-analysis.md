---
title: Cost analysis
---

Cost analysis helps protect a Hot Chocolate server from GraphQL operations that, while valid, may be too resource-intensive to execute safely. Before any resolver runs, the system estimates the cost of an operation, compares it to your configured budgets, and rejects requests that exceed those limits.

Refer to this page if your clients can send dynamic GraphQL documents. If all production clients use pre-registered operations, also review [trusted documents](trusted-documents.md), as allowlisting and cost budgets address different concerns.

# Why cost limits are important

A single GraphQL document can traverse multiple relationships and nested lists in one request. Even a small-looking document can require the server to resolve a large object graph.

```graphql
query ProductReviewFanOut {
  products(first: 50) {
    nodes {
      name
      reviews(first: 50) {
        nodes {
          rating
          author {
            displayName
          }
        }
      }
    }
  }
}
```

This operation could request up to `50 x 50 = 2,500` reviews, in addition to products, authors, connection objects, and resolver work. Cost analysis detects this operation shape before execution. With default limits, such an operation is often rejected before any resolver is invoked.

Cost is a static estimate. It does not represent elapsed time, database rows, memory usage, or resolver duration. Think of it as a budget for the operation's shape, working alongside paging, depth limits, timeouts, authorization, and trusted documents.

# Enabling and tuning the analyzer

The standard ASP.NET Core registration path enables default security, which includes the cost analyzer.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
    });

var app = builder.Build();

app.MapGraphQL();

await app.RunAsync();
```

Default cost budgets provide a conservative starting point:

| Option              | Default | Effect                                                                             |
| ------------------- | ------- | ---------------------------------------------------------------------------------- |
| `MaxFieldCost`      | `1_000` | Maximum allowed field cost, the static estimate of resolver and nested field work. |
| `MaxTypeCost`       | `1_000` | Maximum allowed type cost, the static estimate of response object fan-out.         |
| `EnforceCostLimits` | `true`  | Reject operations when either budget is exceeded.                                  |

If default security is disabled, register the analyzer explicitly.

```csharp
builder
    .AddGraphQL(disableDefaultSecurity: true)
    .AddCostAnalyzer()
    .AddQueryType<Query>()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
    });
```

An accepted operation continues through the request pipeline and executes normally. An operation over budget returns a validation-style error and resolvers do not run.

# How Hot Chocolate estimates cost

Hot Chocolate cost analysis is based on static schema metadata and the operation document. The analyzer runs after document validation and before execution.

```text
parse
  -> validate
  -> analyze cost
  -> reject or execute
  -> optionally report metrics
```

The analyzer calculates two metrics.

| Metric     | What it represents                                                                    | Typical tuning signal                                                    |
| ---------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Field cost | Resolver work, field weights, argument weights, input weights, and nested selections. | Raise or lower `MaxFieldCost`, add `[Cost]`, review expensive fields.    |
| Type cost  | Response object fan-out from composite types and lists.                               | Raise or lower `MaxTypeCost`, reduce page sizes, add list-size metadata. |

## Default weights

When `ApplyCostDefaults` is `true`, Hot Chocolate adds default metadata during schema setup.

| Schema item                   | Default weight or size | Notes                                                     |
| ----------------------------- | ---------------------- | --------------------------------------------------------- |
| Scalar or enum field          | `0`                    | Leaf selections are cheap by default.                     |
| Composite field or list field | `1`                    | Object and list selections add cost.                      |
| Composite type                | `1`                    | Used for type cost.                                       |
| Leaf type                     | `0`                    | Scalar and enum types do not add type cost by default.    |
| Async resolver pipeline field | `10.0`                 | Controlled by `DefaultResolverCost`.                      |
| List without `@listSize`      | `1`                    | Add paging or list-size metadata for lists that can grow. |

Explicit `@cost` or `[Cost]` metadata overrides the default weight for that schema element. If a field-cost sum becomes negative, Hot Chocolate floors that field cost at zero.

## Lists multiply nested cost

List-size metadata tells the analyzer how many items to assume. The list size multiplies the nested field and type cost below the list.

```graphql
query ProductsAndReviews {
  products(first: 20) {
    nodes {
      reviews(first: 10) {
        nodes {
          rating
        }
      }
    }
  }
}
```

The nested `reviews` selection is evaluated for every selected product. With `20` products and `10` reviews per product, nested review fields are estimated across up to `200` review nodes.

The analyzer also handles details that matter for large schemas:

- Interfaces and unions use the highest possible object type cost for an abstract selection.
- The same response name inside a selection set is counted once.
- Argument and input field costs contribute to field cost.
- A filter variable can use the configured filter variable multiplier.

# Configure paging for predictable costs

Pagination is the first control for list fan-out. Every list that can grow should have a server-side boundary.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyPagingOptions(options =>
    {
        options.MaxPageSize = 50;
        options.DefaultPageSize = 10;
        options.RequirePagingBoundaries = true;
    });
```

Paging options affect generated cost metadata.

| Paging option             | Default | Cost analysis effect                                                                                             |
| ------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------- |
| `MaxPageSize`             | `50`    | Used as `assumedSize` for paged fields. Higher values increase estimated cost.                                   |
| `DefaultPageSize`         | `10`    | Used as `slicingArgumentDefaultValue` when `ApplySlicingArgumentDefaultValue` is enabled.                        |
| `RequirePagingBoundaries` | `false` | Requires clients to send a boundary such as `first` or `last`, and drives generated `requireOneSlicingArgument`. |

For cursor connections, Hot Chocolate can generate metadata like this:

```graphql
products(first: Int, after: String, last: Int, before: String): ProductsConnection
  @listSize(
    assumedSize: 50
    slicingArguments: ["first", "last"]
    slicingArgumentDefaultValue: 10
    sizedFields: ["edges", "nodes"]
    requireOneSlicingArgument: false
  )
  @cost(weight: "10")
```

For offset paging, generated list-size metadata uses `take` as the slicing argument and `items` as the sized field.

If a client omits a slicing argument, the analyzer can use `slicingArgumentDefaultValue`. If a client sends a variable and the static analyzer cannot know the runtime value, the analyzer can fall back to `assumedSize`. If several slicing arguments have usable values and exactly-one slicing is not required, the analyzer uses the maximum value.

# Add cost metadata where defaults are not enough

Cost metadata belongs in the schema. Limits belong in cost options. Add metadata when the default model does not match your resolver or list behavior.

## Mark expensive fields

Use `[Cost]` for expensive fields, arguments, input fields, and advanced type-wide rules.

```csharp
using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

[QueryType]
public static partial class ReportQueries
{
    [Cost(75)]
    public static async Task<SalesReport> GetSalesReport(
        DateOnly from,
        DateOnly to,
        ReportService reports,
        CancellationToken cancellationToken)
    {
        return await reports.Generate(from, to, cancellationToken);
    }
}
```

Expected SDL shape:

```graphql
type Query {
  salesReport(from: Date!, to: Date!): SalesReport! @cost(weight: "75")
}
```

Use higher weights for remote service calls, aggregate fields, report generation, computed relationships, or search arguments that add backend work. Use the [Cost attribute](../attributes/cost.md) page for full attribute details.

Descriptor-based schemas can apply the same metadata.

```csharp
using HotChocolate.Types;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("salesReport")
            .Resolve(context => context.Service<ReportService>().Generate())
            .Cost(75);
    }
}
```

## Describe custom list sizes

Use `[ListSize]` for list fields that do not use standard Hot Chocolate paging, or when generated paging metadata needs an intentional override.

```csharp
using HotChocolate;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

[QueryType]
public static partial class CatalogQueries
{
    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["limit"],
        SlicingArgumentDefaultValue = 20,
        RequireOneSlicingArgument = false)]
    public static async Task<IReadOnlyList<Product>> GetTopProducts(
        int? limit,
        ProductService products,
        CancellationToken cancellationToken)
    {
        return await products.GetTopProducts(limit ?? 20, cancellationToken);
    }
}
```

Expected SDL shape:

```graphql
type Query {
  topProducts(limit: Int): [Product!]!
    @listSize(
      assumedSize: 100
      slicingArguments: ["limit"]
      slicingArgumentDefaultValue: 20
      requireOneSlicingArgument: false
    )
}
```

Use `SizedFields` when the size applies to child list fields instead of the field itself, for example `edges` and `nodes` on a custom connection-like type.

```csharp
descriptor
    .Field("topProducts")
    .Argument("limit", argument => argument.Type<IntType>())
    .Resolve(context => context.Service<ProductService>().GetTopProducts())
    .ListSize(
        assumedSize: 100,
        slicingArguments: ["limit"],
        requireOneSlicingArgument: false,
        slicingArgumentDefaultValue: 20);
```

Use the [ListSize attribute](../attributes/listsize.md) page for a detailed reference. `AssumedSize` and `SlicingArgumentDefaultValue` must be non-negative. `[ListSize]` does not limit resolver results. Enforce result sizes with paging, validation, or service logic.

# Measure operation cost

Measure representative operations before changing budgets. Hot Chocolate supports HTTP headers and programmatic request builder helpers.

| Mode     | How to request it        | Behavior                                                                                                  |
| -------- | ------------------------ | --------------------------------------------------------------------------------------------------------- |
| Normal   | No `GraphQL-Cost` header | Analyze, enforce configured limits, execute accepted operations, and omit cost metrics from the response. |
| Report   | `GraphQL-Cost: report`   | Analyze, enforce configured limits, execute accepted operations, and add cost metrics.                    |
| Validate | `GraphQL-Cost: validate` | Analyze and return cost metrics without executing resolvers.                                              |
| Skip     | `SkipAnalyzer = true`    | Do not analyze, report, or enforce cost. Use only for a deliberate opt-out.                               |

HTTP report example:

```bash
curl http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -H "GraphQL-Cost: report" \
  -d '{"query":"query { products(first: 5) { nodes { name } } }"}'
```

Expected response shape:

```json
{
  "data": {
    "products": {
      "nodes": [{ "name": "Coffee" }]
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 18,
      "typeCost": 12
    }
  }
}
```

The numbers depend on your schema metadata and selected fields. Treat the shape of `extensions.operationCost.fieldCost` and `extensions.operationCost.typeCost` as the stable contract.

HTTP validate example:

```bash
curl http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -H "GraphQL-Cost: validate" \
  -d '{"query":"query { products(first: 5) { nodes { name } } }"}'
```

Expected response shape:

```json
{
  "extensions": {
    "operationCost": {
      "fieldCost": 18,
      "typeCost": 12
    }
  }
}
```

Programmatic requests can use the same modes.

```csharp
using HotChocolate.Execution;

var reportRequest = OperationRequestBuilder.New()
    .SetDocument("query { products(first: 5) { nodes { name } } }")
    .ReportCost()
    .Build();

var validateRequest = OperationRequestBuilder.New()
    .SetDocument("query { products(first: 5) { nodes { name } } }")
    .ValidateCost()
    .Build();
```

# Reject expensive operations

When enforcement is enabled, the analyzer checks field cost first and type cost second. An over-budget operation returns an error with code `HC0047`.

Field-cost rejection shape:

```json
{
  "errors": [
    {
      "message": "The maximum allowed field cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "fieldCost": 5611,
        "maxFieldCost": 1000
      }
    }
  ]
}
```

Type-cost rejection shape:

```json
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "typeCost": 7601,
        "maxTypeCost": 1000
      }
    }
  ]
}
```

If the request also uses `GraphQL-Cost: report`, rejected results can include top-level `extensions.operationCost` metrics in addition to the error extension values.

# Tune budgets safely

Use this workflow for public or partner APIs:

1. Add paging to every list that can grow.
2. Set `MaxPageSize`, `DefaultPageSize`, and `RequirePagingBoundaries` intentionally.
3. Add `[Cost]` to fields that do more work than their schema shape suggests.
4. Add `[ListSize]` to non-standard list fields.
5. Measure common and largest expected operations with `GraphQL-Cost: report` or `GraphQL-Cost: validate`.
6. Set `MaxFieldCost` and `MaxTypeCost` with a margin above measured legitimate operations.
7. Monitor rejected operations and cost percentiles after release.

Tune the two budgets separately.

| Symptom                                   | Likely cause                                                     | First action                                                              |
| ----------------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Field cost is high, type cost is moderate | Expensive resolver weights, filter arguments, nested field work. | Review `[Cost]`, filters, sorting, and resolver fields.                   |
| Type cost is high, field cost is moderate | Large list fan-out.                                              | Lower page sizes or add list-size metadata.                               |
| Expected admin operation is rejected      | Legitimate trusted caller has a larger budget.                   | Use a request-scoped override for that caller.                            |
| Public operation needs very high limits   | Operation shape is too broad for a public endpoint.              | Reduce nesting, split the client operation, or require trusted documents. |

Do not make inaccurate metadata smaller to let an operation pass. Prefer more precise list-size metadata, smaller page sizes, split operations, or a measured budget change.

# Override cost options for trusted requests

Request-scoped options are useful for trusted internal callers, controlled admin operations, or a temporary measurement period. Apply them with a delegate HTTP request interceptor.

```csharp
using HotChocolate.Execution;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor(
        (context, requestExecutor, requestBuilder, cancellationToken) =>
        {
            if (context.User.IsInRole("Admin"))
            {
                var costOptions = requestExecutor.GetCostOptions();
                requestBuilder.SetCostOptions(
                    costOptions with
                    {
                        MaxFieldCost = 20_000,
                        MaxTypeCost = 20_000
                    });
            }

            return ValueTask.CompletedTask;
        });
```

`EnforceCostLimits = false` keeps analysis available but stops rejection. `SkipAnalyzer = true` disables analysis, reporting, and enforcement. Setting either option affects the other option so that skipped analysis is not also enforced.

Read [Interceptors](../server-configuration/interceptors.md) for the transport hook details.

# Use cost analysis with trusted documents

Persisted operations, also called trusted documents, let a server execute only pre-registered operation documents. That blocks arbitrary operation text, but it does not make every registered operation cheap.

Recommended workflow for trusted documents:

1. Register or publish the operation document through your trusted-document process.
2. Measure its cost during review or CI.
3. Keep enforcement enabled when the registered operations fit a clear budget.
4. Use `EnforceCostLimits = false` for reporting-only mode if the allowlist is your primary operation-shape control.
5. Avoid `SkipAnalyzer = true` unless you also accept losing cost metrics.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(options =>
    {
        options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    })
    .ModifyCostOptions(options =>
    {
        options.EnforceCostLimits = false;
    });
```

Read [trusted documents](trusted-documents.md) before choosing an allowlist policy.

# Common false positives and underestimated costs

A false positive means a legitimate operation is rejected because the static estimate is too high. A false negative means an operation looks cheaper than it is.

| Symptom                                          | Likely reason                                                                                                                | Fix                                                                                              |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Paged field cost is higher than expected         | `MaxPageSize` is larger than the selected page, a variable hides the runtime value, or default page size metadata is absent. | Measure with literal values, tune `MaxPageSize`, or review `ApplySlicingArgumentDefaultValue`.   |
| Connection without `first` or `last` is rejected | `RequirePagingBoundaries` generated `requireOneSlicingArgument`.                                                             | Send exactly one boundary or change the paging contract intentionally.                           |
| Abstract selection cost is higher than expected  | Interfaces and unions use the highest possible object type cost.                                                             | Measure concrete selections, split high-cost fields, or use trusted documents for known clients. |
| Raw list looks cheap                             | No paging or `@listSize`, so unknown list size defaults to `1`.                                                              | Add paging, `[ListSize]`, descriptor `.ListSize(...)`, or SDL `@listSize`.                       |
| Expensive resolver looks cheap                   | Default field weight is too low for real backend work.                                                                       | Add `[Cost]` or descriptor `.Cost(...)`.                                                         |
| Filter-heavy operation is rejected               | Filter argument and operation costs, plus variable multiplier, increase field cost.                                          | Review filter options, client filters, and measured cost before changing limits.                 |

# Options reference

## Main cost options

| Option                             | Default | Change when                                                                                      |
| ---------------------------------- | ------- | ------------------------------------------------------------------------------------------------ |
| `MaxFieldCost`                     | `1_000` | Legitimate measured operations need a larger or smaller execution-work budget.                   |
| `MaxTypeCost`                      | `1_000` | Legitimate measured operations need a larger or smaller response fan-out budget.                 |
| `EnforceCostLimits`                | `true`  | You want reporting without rejection for a controlled API or transition period.                  |
| `SkipAnalyzer`                     | `false` | You need to bypass all analysis and metrics for a deliberate, documented reason.                 |
| `ApplyCostDefaults`                | `true`  | You want to opt out of generated cost and list-size defaults and provide metadata yourself.      |
| `ApplySlicingArgumentDefaultValue` | `true`  | You do not want paging `DefaultPageSize` to become list-size fallback metadata.                  |
| `DefaultResolverCost`              | `10.0`  | Async resolver pipeline fields need a different default cost, or `null` to disable that default. |

## Filtering and sorting options

| Option                                          | Default | Notes                                                                                  |
| ----------------------------------------------- | ------- | -------------------------------------------------------------------------------------- |
| `Filtering.DefaultFilterArgumentCost`           | `10.0`  | Default cost for a filter argument.                                                    |
| `Filtering.DefaultFilterOperationCost`          | `10.0`  | Default cost for a filter operation.                                                   |
| `Filtering.DefaultExpensiveFilterOperationCost` | `20.0`  | Default cost for an expensive filter operation.                                        |
| `Filtering.VariableMultiplier`                  | `5`     | Filter variables can multiply filter argument cost.                                    |
| `Sorting.DefaultSortArgumentCost`               | `10.0`  | Default cost option for a sort argument.                                               |
| `Sorting.DefaultSortOperationCost`              | `10.0`  | Default cost option for a sort operation.                                              |
| `Sorting.VariableMultiplier`                    | `5`     | Sorting option value. Measure sort-heavy operations before relying on a budget change. |

# Troubleshooting

## `The maximum allowed field cost was exceeded.`

Meaning: static field work exceeded `MaxFieldCost`.

Try:

- Measure the operation with `GraphQL-Cost: report` or `GraphQL-Cost: validate`.
- Add `[Cost]` to represent expensive or cheap fields accurately.
- Lower page sizes for nested lists.
- Raise `MaxFieldCost` only after confirming the operation is legitimate.

## `The maximum allowed type cost was exceeded.`

Meaning: response fan-out exceeded `MaxTypeCost`.

Try:

- Lower `MaxPageSize` on high fan-out fields.
- Enable `RequirePagingBoundaries` for public clients.
- Reduce nested list selections in client operations.
- Raise `MaxTypeCost` only after measurement.

## `Exactly one slicing argument must be defined.`

Meaning: a field has `requireOneSlicingArgument` and the operation supplied zero or more than one usable slicing argument.

Error shape:

```json
{
  "errors": [
    {
      "message": "Exactly one slicing argument must be defined.",
      "extensions": {
        "code": "HC0082"
      }
    }
  ]
}
```

Try:

- Send exactly one of `first` or `last` for cursor paging.
- For custom list metadata, review `SlicingArguments` and `RequireOneSlicingArgument`.
- If omitted boundaries are part of the API contract, set that option intentionally and document the cost tradeoff.

## Metrics are missing

Check:

- The request used `GraphQL-Cost: report`, `GraphQL-Cost: validate`, `.ReportCost()`, or `.ValidateCost()`.
- `SkipAnalyzer` is not enabled globally or for the request.
- Cost analysis is registered. If default security is disabled, call `.AddCostAnalyzer()`.
- Custom class-based HTTP interceptors preserve the default interceptor behavior so request features and cost switches are available.

## `GraphQL-Cost: validate` did not execute my resolver

That is expected. `validate` computes static cost and returns metrics without execution. Use `report` when you need execution plus metrics.

## Cost settings seem ignored

Check:

- `.ModifyCostOptions(...)` runs on the same GraphQL builder that registers the schema.
- No request interceptor replaces cost options for this request.
- `SkipAnalyzer = true` is not set.
- Generated paging metadata is not being overridden by explicit `[ListSize]` or descriptor metadata.

# Monitor production cost

Cost metrics appear in response extensions when requested:

```json
{
  "extensions": {
    "operationCost": {
      "fieldCost": 120,
      "typeCost": 42
    }
  }
}
```

Diagnostics also include these activity tags:

| Tag                           | Meaning                                  |
| ----------------------------- | ---------------------------------------- |
| `graphql.operation.fieldCost` | Calculated field cost for the operation. |
| `graphql.operation.typeCost`  | Calculated type cost for the operation.  |

Track p95 and p99 field cost, p95 and p99 type cost, rejected operation counts, and top costly operation names or document ids when available.

# When to use another control

Cost analysis is one security layer.

| Use                             | Better fit when                                                                                                                                                       |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Depth limits and request limits | The risk is extreme nesting, parser recursion, fragment visits, large documents, batching, or timeout behavior. Read [request limits](execution-depth-and-limits.md). |
| Trusted documents               | Production clients are controlled and every operation can be registered before deployment. Read [trusted documents](trusted-documents.md).                            |
| Authorization                   | The issue is who can access a field or object, not how expensive the operation is. Read [Authorization](authorization.md).                                            |
| Paging                          | The issue is unbounded list size. Read [Pagination](../pagination/index.md).                                                                                          |

# Next steps

- Review the [security overview](index.md) to place cost analysis in the full security model.
- Use [Cost attribute](../attributes/cost.md) and [ListSize attribute](../attributes/listsize.md) for schema metadata details.
- Tune server behavior from [Server configuration](../server-configuration/index.md).
- Pair cost budgets with [Pagination](../pagination/index.md), [Trusted documents](trusted-documents.md), and [Request limits](execution-depth-and-limits.md).
