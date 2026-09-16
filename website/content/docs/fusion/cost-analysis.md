---
title: Cost Analysis
description: "Configure Fusion cost analysis, limits, reporting, and pre-plan enforcement."
---

Fusion estimates an operation's field cost and type cost before operation planning. When maximum response size is enabled, it also estimates the response-field count. The analyzer compiles and caches a cost plan, then evaluates that plan against the request's coerced variables.

Cost metadata comes from the public `@cost` and `@listSize` directives in the execution schema. Fusion composition derives these directives from source-schema metadata as described in [Cost Metadata Derivation](./composition.md#cost-metadata-derivation).

# Default Enforcement

`AddGraphQLGatewayServer()` enables cost enforcement in every hosting environment with these limits:

- Maximum field cost: `1,000`
- Maximum type cost: `1,000`

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

Summing the costs prevents a client from splitting an expensive workload among variable sets that each stay under the limit. Request batching is an array of independent requests in one HTTP request. Cost limits currently apply separately to each independent request in a request batch. Summing costs across an entire request batch is planned, with no target version.

# Pipeline Placement

The predefined Fusion pipelines run these stages in order:

1. Document Cache
2. Document Parser
3. Document Validation
4. Document Normalization
5. Operation Variable Coercion
6. Operation Plan Cache
7. Cost Analysis
8. Operation Plan
9. Skip Warmup Execution
10. Concurrency Gate
11. Operation Execution

Cost enforcement runs before operation planning. A rejected request does not create an operation-plan cache entry. `DocumentNormalization` expands fragments and removes statically excluded selections before variable coercion and cost analysis.

A custom pipeline must include `UseDocumentNormalization()`, place `UseOperationVariableCoercion()` before `UseCostAnalysis()`, and place `UseCostAnalysis()` after `UseOperationPlanCache()` and before `UseOperationPlan()`.

# Options Reference

Configure `FusionCostOptions` with `ModifyCostOptions`:

| Option                       | Type                          | Default | Contract                                                                                                                                       |
| ---------------------------- | ----------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `MaxFieldCost`               | `double`                      | `1,000` | Maximum allowed field cost. Valid range: non-negative finite values or `Infinity`.                                                             |
| `MaxTypeCost`                | `double`                      | `1,000` | Maximum allowed type cost. Valid range: non-negative finite values or `Infinity`.                                                              |
| `EnforceCostLimits`          | `bool`                        | `true`  | Enforces the field, type, and response-size limits.                                                                                            |
| `SkipAnalyzer`               | `bool`                        | `false` | Skips analysis, enforcement, and reporting.                                                                                                    |
| `MaxResponseSize`            | `double?`                     | `null`  | Maximum estimated response-field count. `null` disables this check and metric. Valid range: `null`, non-negative finite values, or `Infinity`. |
| `CostPlanCacheSize`          | `int`                         | `256`   | Maximum compiled cost plans cached per schema.                                                                                                 |
| `CaseBudget`                 | `int?`                        | `null`  | Exact cases compiled per operation before `CaseBudgetExceededBehavior` decides the fallback. `null` uses the default (510).                    |
| `CaseBudgetExceededBehavior` | `CaseBudgetExceededBehavior?` | `null`  | Behavior once compiling one operation exhausts `CaseBudget`. `null` uses the default (`EvaluatePerRequest`).                                   |

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

> **Note:** The assumed size for a list without applicable `@listSize` metadata is not a gateway runtime option. It is a composition setting configured on the composer (`SourceSchemaMergerOptions.DefaultListSize`) and carried into the execution schema by the `@fusion__cost_options(defaultListSize:)` directive; see [Composition](./composition.md) for details. Without this setting, an unannotated list is unbounded, and a list whose element type has a positive weight exceeds any finite type-cost limit. Annotate the field with `@listSize(assumedSize:)` in its source schema or configure a finite `DefaultListSize` at composition time.

# Accessing the Analysis Result

`RequestContext.TryGetCostAnalysisResult` provides the compiled `CostPlan`, every estimate for the request, and whether the estimates are the assumed bound (warmup requests). Read the result after the cost middleware has completed:

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
                bool isAssumedBound = result.IsAssumedBound;
            }
        },
        key: "ReadCostAnalysisResult",
        before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

Cost analysis and reporting return `HC0048` when required operation or document state is missing, when a non-warmup request reaches the analyzer with zero coerced variable sets (an explicit empty variable batch, `variables: []`), or when metrics cannot be attached to the execution-result state. This applies to `execute`, `report`, and `validate` mode alike.

# Next Steps

- [Request Limits](./request-limits.md)
- [Composition](./composition.md#cost-metadata-derivation)
- [Directive Reference](./directives-reference.md)
