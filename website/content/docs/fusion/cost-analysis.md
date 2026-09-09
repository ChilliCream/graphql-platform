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

| Header value | Execution | Reported value                                                                |
| ------------ | --------- | ----------------------------------------------------------------------------- |
| `report`     | Yes       | Evaluated cost for the supplied variables.                                    |
| `validate`   | No        | Evaluated cost with variables, or the static bound when variables are absent. |

`validate` returns an extensions-only response with HTTP status `200`, including when the reported values exceed configured limits. A variable batch reports one cost for each variable set.

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

For a single operation, the HTTP status depends on the accepted response media type:

| `Accept` media type                 | Rejection status |
| ----------------------------------- | ---------------- |
| `application/graphql-response+json` | `400`            |
| `application/json`                  | `200`            |

In `report` mode, a rejected response also contains `extensions.operationCost`. If any variable set in a variable batch exceeds a limit, the whole batch is rejected and each result contains its evaluated cost. A rejected variable batch remains HTTP `200` for both media types.

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

| Option              | Type      | Default    | Contract                                                                       |
| ------------------- | --------- | ---------- | ------------------------------------------------------------------------------ |
| `MaxFieldCost`      | `double`  | `1,000`    | Maximum allowed field cost.                                                    |
| `MaxTypeCost`       | `double`  | `1,000`    | Maximum allowed type cost.                                                     |
| `EnforceCostLimits` | `bool`    | `true`     | Enforces the field, type, and response-size limits.                            |
| `SkipAnalyzer`      | `bool`    | `false`    | Skips analysis, enforcement, and reporting.                                    |
| `MaxResponseSize`   | `double?` | `null`     | Maximum estimated response-field count. `null` disables this check and metric. |
| `DefaultListSize`   | `double`  | `Infinity` | Size for a list without applicable `@listSize` metadata.                       |
| `CostPlanCacheSize` | `int`     | `256`      | Maximum compiled cost plans cached per schema.                                 |
| `CaseBudget`        | `int?`    | `null`     | Exact cases evaluated per operation. `null` uses the engine default.           |

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.DefaultListSize = 100;
        options.MaxResponseSize = 10_000;
        options.CostPlanCacheSize = 512;
    });
```

An unannotated list falls through to `DefaultListSize`. With the default `Infinity`, a list whose element type has a positive weight exceeds any finite type-cost limit. Annotate the field with `@listSize(assumedSize:)` in its source schema or set a finite `DefaultListSize` on the gateway.

# Accessing the Analysis Result

`RequestContext.TryGetCostAnalysisResult` provides the compiled `CostPlan`, every estimate for the request, and whether the estimates are a static bound. Read the result after the cost middleware has completed:

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
                bool isStaticBound = result.IsStaticBound;
            }
        },
        key: "ReadCostAnalysisResult",
        before: WellKnownRequestMiddleware.CostAnalyzerMiddleware);
```

# Next Steps

- [Request Limits](./request-limits.md)
- [Composition](./composition.md#cost-metadata-derivation)
- [Directive Reference](./directives-reference.md)
