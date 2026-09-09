---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: account for default cost enforcement and replace raw condition masks with ConditionFlags."
---

Update every Hot Chocolate Fusion package in the application to version 16.7 before applying these changes.

# Deprecations

## Raw condition masks replaced by ConditionFlags

Fusion can now compile and execute operations with more than 64 distinct `@skip`/`@include` conditions or `@defer` conditions. `MaxAllowedIncludeConditions` limits the combined `@skip` and `@include` conditions, and `MaxAllowedDeferConditions` limits the `@defer` conditions. Both limits default to **1,024**. An operation that exceeds either limit produces a GraphQL request error during operation compilation.

Configure the limits through `FusionRequestOptions` on the gateway:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyRequestOptions(options =>
    {
        options.MaxAllowedIncludeConditions = 2_048;
        options.MaxAllowedDeferConditions = 2_048;
    });
```

`ConditionFlags` contains the first 64 evaluated conditions and any remaining conditions. Pass the condition carriers from `OperationPlanContext` to the Fusion `Selection` overloads:

```diff
- bool included = selection.IsIncluded(context.IncludeFlags);
- bool deferred = selection.IsDeferred(context.DeferFlags);
+ bool included = selection.IsIncluded(context.IncludeConditionFlags);
+ bool deferred = selection.IsDeferred(context.DeferConditionFlags);
```

Replace every deprecated Fusion `Selection` overload as follows:

| Deprecated 16.6 member                                   | 16.7 replacement                                                  |
| -------------------------------------------------------- | ----------------------------------------------------------------- |
| `Selection.IsIncluded(ulong)`                            | `Selection.IsIncluded(ConditionFlags)`                            |
| `Selection.IsDeferred(ulong)`                            | `Selection.IsDeferred(ConditionFlags)`                            |
| `Selection.GetActiveDeliveryGroups(ulong)`               | `Selection.GetActiveDeliveryGroups(ConditionFlags)`               |
| `Selection.HasActiveDeliveryGroup(ulong, DeliveryGroup)` | `Selection.HasActiveDeliveryGroup(ConditionFlags, DeliveryGroup)` |

The deprecated raw overloads continue to work for operations with at most 64 conditions. When an operation has more than 64 conditions of the corresponding kind, the deprecated raw inclusion overloads throw `InvalidOperationException` for every conditional selection and the deprecated raw defer overloads throw for every deferrable selection, including selections whose own conditions are all among the first 64; raw inclusion evaluation does not throw for an unconditional selection, and raw defer evaluation does not throw for a non-deferrable selection. Releases before 16.7 rejected operations with more than 64 conditions during compilation.

# Breaking changes

## Custom request pipelines must add the cost stages

All three predefined Fusion pipelines now normalize the document, coerce variables, check cost, and only then plan the operation. Update custom pipelines to use the same relative order:

```diff
 builder
     // ... document cache and parser ...
     .UseDocumentValidation()
+    .UseDocumentNormalization()
+    .UseOperationVariableCoercion()
     .UseOperationPlanCache()
+    .UseCostAnalysis()
     .UseOperationPlan()
     .UseSkipWarmupExecution()
-    .UseOperationVariableCoercion()
     .UseConcurrencyGate()
     .UseOperationExecution();
```

`DocumentNormalization` is new. `OperationVariableCoercion` now runs before `OperationPlanCache`, and `CostAnalysis` runs before `OperationPlan`. Coercion errors therefore precede planning errors. Warmup requests and `GraphQL-Cost: validate` requests without variables use the static-bound path without coercion.

## Fusion diagnostic event interface expanded

Direct implementations of `IFusionExecutionDiagnosticEvents` or `IFusionExecutionDiagnosticEventListener` must implement these members:

```diff
 public interface IFusionExecutionDiagnosticEvents
 {
+    IDisposable AnalyzeOperationCost(RequestContext context);
+    void OperationCost(
+        RequestContext context,
+        double fieldCost,
+        double typeCost);
 }
```

`AnalyzeOperationCost` scopes cost analysis. `OperationCost` reports each evaluated field-cost and type-cost pair inside that scope.

`FusionExecutionDiagnosticEventListener` supplies implementations for both members, so subclasses do not require changes. `FusionActivityScopes.AnalyzeComplexity` now enables the cost-analysis activity span. It is included in `FusionActivityScopes.All`, but not in `FusionActivityScopes.Default`.

## Cost enforcement is enabled by default

Fusion now enforces a maximum field cost of `1,000` and a maximum type cost of `1,000` in every hosting environment. A request that exceeds either limit returns error code `HC0047` before operation planning.

Passing `disableDefaultSecurity: true` disables cost enforcement as part of disabling the gateway's default security. Cost analysis and `GraphQL-Cost` reporting remain available:

```diff
-services.AddGraphQLGatewayServer();
+services.AddGraphQLGatewayServer(disableDefaultSecurity: true);
```

## Default list size is Infinity

`FusionCostOptions.DefaultListSize` is a `double` and defaults to `double.PositiveInfinity`. With default enforcement enabled, an unannotated, non-paginated composite list is rejected with `HC0047` and a `typeCost` of `"Infinity"`.

Annotate the source field with `@listSize(assumedSize:)` so composition carries the bound into the composite directive, or set a finite gateway default:

```diff
 builder.Services
     .AddGraphQLGatewayServer()
+    .ModifyCostOptions(options => options.DefaultListSize = 100)
     // ... gateway configuration ...
```

## Composition derives public cost directives

Composition now folds source `@cost` and `@listSize` usages into public directives and records the declaring source values as `@fusion__cost` and `@fusion__listSize` provenance entries. The gateway enforces the folded public values.

The public `@cost(weight:)` is the maximum effective weight among every serving source. A source's effective weight is its declared weight or the default for that coordinate: composite types and output fields returning composites use `1`; leaf types and output fields returning leaves use `0`; arguments and input fields use `1` when input-object-typed and `0` otherwise. For example, a composite field weighted `-7` in one source and unannotated in another now folds to `1`.

Public `@listSize` arguments are folded as follows:

| Argument                      | Fold over source entries                                                                                                                                                                 |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `assumedSize`                 | Maximum of present values.                                                                                                                                                               |
| `slicingArguments`            | Plain union in first-seen order.                                                                                                                                                         |
| `sizedFields`                 | Plain union in first-seen order.                                                                                                                                                         |
| `requireOneSlicingArgument`   | `true` if any value is `true`; `false` when at least one value is `false` and none is `true`; omitted when every value is `null`. Source definition defaults are applied before folding. |
| `slicingArgumentDefaultValue` | Maximum of present values; omitted when no source provides it.                                                                                                                           |

Composition accepts compatible IBM-spec `@cost` and `@listSize` definitions that omit canonical arguments. The directive name and repeatability must match, every declared argument must use the canonical type, and source locations must be a subset of the canonical locations. Defaults and descriptions do not affect compatibility. `slicingArgumentDefaultValue` remains optional, and usages without a local definition receive the canonical definition during composition.

# Behavioral breaking changes

## Cost estimates use coerced request values

Cost plans are compiled once and evaluated for each request. Supplied slicing variables, Boolean `@include` and `@skip` conditions, and variable-supplied input objects now affect the estimate that is reported and enforced. An accepted variable batch reports each variable set's cost on its corresponding result in report or validate mode. If any set exceeds a limit, every batch index returns `HC0047` using the first violation's kind and limit, but each index carries its own numeric estimate and, in report mode, its own `operationCost`. The request does not execute.

Fusion uses the same cost rules as Hot Chocolate:

- Complementary `@include` and `@skip` branches are treated as mutually exclusive.
- Fields are collected by response name before signed weights are applied. Clamping happens after the complete field-call sum and per-instance type sum are calculated.
- An interface or union return weight is the signed maximum of its member object-type weights.
- A field selected through an interface is priced through each possible object type's field metadata.
- Explicit `@cost(weight: "1")` metadata on output lists of scalars is preserved in SDL.
- Costs on arguments of directives used in the query contribute to field cost.
- An inherited size from a parent's `@listSize(sizedFields:)` takes precedence over the child field's own `@listSize`.
- Negative slicing values clamp to `0`. A slicing value of `0` remains `0`, while the field-call cost is still paid once.

For list fields, the first applicable source in this order supplies the size:

1. An inherited size from a parent `sizedFields` annotation.
2. The maximum slicing argument present after coercion, including schema argument defaults.
3. `slicingArgumentDefaultValue`, when no slicing argument is present.
4. `assumedSize`.
5. `FusionCostOptions.DefaultListSize`.

An explicit null slicing argument is not an integer value and suppresses that argument's schema default. An undefined slicing variable behaves as an absent argument. When no schema argument default applies, both cases fall through to `slicingArgumentDefaultValue`.

# Noteworthy changes

## Fusion cost options

Configure these values with `ModifyCostOptions`:

| Option              | Type      | Default    | Contract                                                                    |
| ------------------- | --------- | ---------- | --------------------------------------------------------------------------- |
| `MaxFieldCost`      | `double`  | `1,000`    | Maximum field cost.                                                         |
| `MaxTypeCost`       | `double`  | `1,000`    | Maximum type cost.                                                          |
| `EnforceCostLimits` | `bool`    | `true`     | Enforces the field, type, and response-size limits.                         |
| `SkipAnalyzer`      | `bool`    | `false`    | Skips cost analysis and reporting when `true`.                              |
| `DefaultListSize`   | `double`  | `Infinity` | Size for a list without applicable `@listSize` metadata.                    |
| `MaxResponseSize`   | `double?` | `null`     | Maximum response-object-field count. `null` disables this check and metric. |
| `CostPlanCacheSize` | `int`     | `256`      | Maximum compiled cost plans cached per schema.                              |
| `CaseBudget`        | `int?`    | `null`     | Exact cases evaluated per operation. `null` uses the engine default.        |

When `MaxResponseSize` is enabled, `extensions.operationCost` includes `maxResponseSize`. A rejection includes `{ maxResponseSize, maxAllowedResponseSize }` in the error extensions and uses error code `HC0047`.

## Reporting and result access

`GraphQL-Cost: validate` without variables reports the static bound. It does not execute the operation, returns no `data`, and remains HTTP 200 even when the reported value exceeds a configured limit. With variables, it reports the evaluated cost.

Positive infinite values in `extensions.operationCost` and cost error extensions are serialized as the JSON string `"Infinity"`. A `GraphQL-Cost: report` rejection includes `operationCost` alongside the error.

Single-result cost rejections are request errors. They return HTTP 400 when the response media type is `application/graphql-response+json`; legacy `application/json` responses remain HTTP 200. A rejected Fusion variable batch returns an `OperationResultBatch` and remains HTTP 200 for either media type.

Use `RequestContext.TryGetCostAnalysisResult(out var result)` to access the compiled `CostPlan`, all estimates for the request, and whether the result is a static bound.
