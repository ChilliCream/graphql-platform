---
title: Migrate Hot Chocolate Fusion from 16.6 to 16.7
description: "Migration guide for Hot Chocolate Fusion v16.6 to v16.7: account for default cost enforcement, implement the new WebSocket connection initialization diagnostic event, replace raw condition masks with ConditionFlags, and configure wide operation limits."
---

Update every Hot Chocolate Fusion package in the application to version 16.7 before applying these changes.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## IServerDiagnosticEvents gained a WebSocket connection initialization event

`IServerDiagnosticEvents` has a new `WebSocketConnectionInitialized` member. The gateway raises it once per WebSocket session, for both the `graphql-transport-ws` and the legacy `graphql-ws` protocol, after the client's connection initialization message has been accepted.

Listeners that derive from `ServerDiagnosticEventListener` need no change, because the base class provides a virtual no-op. Types that implement `IServerDiagnosticEvents` directly have to implement the new member:

```csharp
public void WebSocketConnectionInitialized(
    ISocketSession session,
    IOperationMessagePayload connectionInitMessage)
{
}
```

The payload of `connectionInitMessage` is only valid for the duration of the call. Read out any value that is needed later inside the callback, for example onto `ISocketConnection.Features`.

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

`DocumentNormalization` is new. `OperationVariableCoercion` now runs before `OperationPlanCache`, and `CostAnalysis` runs before `OperationPlan`. Coercion errors therefore precede planning errors. Only warmup requests skip coercion and use the assumed-bound path; `GraphQL-Cost: validate` requests coerce variables exactly like `execute` and `report`, and fail with the ordinary coercion error when required variables are missing.

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

## Default list size is Infinity, and is configured at composition time

The assumed size for a list field that carries no applicable `@listSize` information defaults to unbounded (`Infinity`). With default enforcement enabled, an unannotated, non-paginated composite list is rejected with `HC0047` and a `typeCost` of `"Infinity"`.

This default is a composition setting, not a gateway runtime option: `FusionCostOptions.DefaultListSize` no longer exists. Annotate the source field with `@listSize(assumedSize:)` so composition carries the bound into the composite directive, or set a finite default on the composer's `SourceSchemaMergerOptions.DefaultListSize`. When set, composition writes it onto the execution schema with a schema-level `@fusion__cost_options(defaultListSize:)` directive, and the gateway reads it from there:

```diff
 var options = new SchemaComposerOptions
 {
     Merger =
     {
+        DefaultListSize = 100
     }
 };
```

See [Composition](../composition.md#default-list-size) for details.

## Composition derives public cost directives

Composition now folds source `@cost` and `@listSize` usages into public directives and records the declaring source values as `@fusion__cost` and `@fusion__listSize` provenance entries. The gateway enforces the folded public values.

The public `@cost(weight:)` is the maximum effective weight among every serving source. A source's effective weight is its declared weight or the default for that coordinate: composite types and output fields returning composites use `1`; leaf types and output fields returning leaves use `0`; arguments and input fields use `1` when input-object-typed and `0` otherwise. For example, a composite field weighted `-7` in one source and unannotated in another now folds to `1`. A source that provides the field only as partial (for example an Apollo Federation `@external` field returned through `@provides`) is not a serving source: an unannotated partial member contributes neither its default weight to `@cost` nor a gap that widens `@listSize`'s `assumedSize`, though its own declared usage still folds in.

Public `@listSize` arguments are folded as follows:

| Argument                      | Fold over source entries                                                                                                                                                                 |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `assumedSize`                 | Maximum of present values.                                                                                                                                                               |
| `slicingArguments`            | Plain union in first-seen order.                                                                                                                                                         |
| `sizedFields`                 | Plain union in first-seen order.                                                                                                                                                         |
| `requireOneSlicingArgument`   | `true` if any value is `true`; `false` when at least one value is `false` and none is `true`; omitted when every value is `null`. Source definition defaults are applied before folding. |
| `slicingArgumentDefaultValue` | Maximum of present values; omitted when no source provides it.                                                                                                                           |

Composition accepts compatible IBM-spec `@cost` and `@listSize` definitions that omit canonical arguments. The directive name and repeatability must match, every declared argument must use the canonical type, and source locations must be a subset of the canonical locations. Defaults and descriptions do not affect compatibility. `slicingArgumentDefaultValue` remains optional, and usages without a local definition receive the canonical definition during composition.

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

# Behavioral breaking changes

## Cost estimates use coerced request values

Cost plans are compiled once and evaluated for each request. Supplied slicing variables, Boolean `@include` and `@skip` conditions, and variable-supplied input objects now affect the estimate that is reported and enforced.

In 16.7, one request is one invocation of the request pipeline, and a variable batch is one request. Cost enforcement sums the field cost and type cost across all variable sets and compares those sums with `MaxFieldCost` and `MaxTypeCost`. If either sum exceeds its limit, the whole request is rejected before any variable set executes, with one `HC0047` result. This prevents a client from splitting an expensive workload among variable sets that each stay under the limit. Maximum response size remains enforced per variable set. `MaxResponseSize` is checked per variable set, not summed. If any set exceeds `MaxResponseSize`, the whole request is likewise rejected before any set executes, with one `HC0047` result. When multiple sets violate this limit, the first violating set determines the reported `maxResponseSize`.

Fusion uses the same cost rules as Hot Chocolate:

- Complementary `@include` and `@skip` branches are treated as mutually exclusive.
- Fields are collected by response name before signed weights are applied. Clamping happens after the complete field-call sum and per-instance type sum are calculated.
- An interface or union return weight is the signed maximum of its member object-type weights.
- A field selected through an interface is priced through each possible object type's field metadata.
- Output fields returning lists of scalars or enums now default to weight `0`, as the [IBM cost specification](https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight) requires. In 16.x, they cost `1`; add `@cost(weight: "1")` to retain that cost.
- Costs on arguments of directives used in the query contribute to field cost.
- An inherited size from a parent's `@listSize(sizedFields:)` takes precedence over the child field's own `@listSize`.
- Negative slicing values clamp to `0`. A slicing value of `0` remains `0`, while the field-call cost is still paid once.

For list fields, the first applicable source in this order supplies the size:

1. An inherited size from a parent `sizedFields` annotation.
2. The maximum slicing argument present after coercion, including schema argument defaults.
3. `slicingArgumentDefaultValue`, when no slicing argument is present.
4. `assumedSize`.
5. The default list size configured at composition time (absent means unbounded).

An explicit null slicing argument is not an integer value and suppresses that argument's schema default. An undefined slicing variable behaves as an absent argument. When no schema argument default applies, both cases fall through to `slicingArgumentDefaultValue`.

# Noteworthy changes

## Fusion cost options

Configure these values with `ModifyCostOptions`:

| Option              | Type      | Default | Contract                                                                    |
| ------------------- | --------- | ------- | --------------------------------------------------------------------------- |
| `MaxFieldCost`      | `double`  | `1,000` | Maximum field cost.                                                         |
| `MaxTypeCost`       | `double`  | `1,000` | Maximum type cost.                                                          |
| `EnforceCostLimits` | `bool`    | `true`  | Enforces the field, type, and response-size limits.                         |
| `SkipAnalyzer`      | `bool`    | `false` | Skips cost analysis and reporting when `true`.                              |
| `MaxResponseSize`   | `double?` | `null`  | Maximum response-object-field count. `null` disables this check and metric. |
| `CostPlanCacheSize` | `int`     | `256`   | Maximum compiled cost plans cached per schema.                              |
| `CaseBudget`        | `int?`    | `null`  | Exact cases evaluated per operation. `null` uses the default (510).         |

The default list size for an unannotated list is not among these options: it is a composition setting (`SourceSchemaMergerOptions.DefaultListSize`) carried into the execution schema by `@fusion__cost_options(defaultListSize:)`; see the section above.

When `MaxResponseSize` is enabled, `extensions.operationCost` includes `maxResponseSize`. A rejection includes `{ maxResponseSize, maxAllowedResponseSize }` in the error extensions and uses error code `HC0047`.

## Reporting and result access

`GraphQL-Cost: validate` requires the variables the operation declares, exactly like `execute` and `report`; without them, the request fails with the ordinary variable coercion error. With variables, it does not execute the operation, returns no `data`, reports the evaluated cost, and remains HTTP 200 even when the reported value exceeds a configured limit. A variable batch in validate mode returns one extensions-only result per variable set, with that set's `operationCost`. A successful variable batch in report mode also includes one `operationCost` per result.

Positive infinite values in `extensions.operationCost` and cost error extensions are serialized as the JSON string `"Infinity"`. A `GraphQL-Cost: report` rejection includes `operationCost` alongside the error. For a rejected variable batch, the single rejection result contains one `operationCost`: its `fieldCost` and `typeCost` are the sums across all variable sets, and it contains `maxResponseSize` only for a response-size rejection, using the first violating set's value.

Cost rejections, including the single result for a rejected variable batch, return HTTP 400 when the response media type is `application/graphql-response+json`; legacy `application/json` responses remain HTTP 200.

Request batching is an array of independent requests in one HTTP request. Cost limits currently apply separately to each independent request in a request batch. Summing costs across an entire request batch is planned, with no target version.

Use `RequestContext.TryGetCostAnalysisResult(out var result)` to access the compiled `CostPlan`, all estimates for the request, and whether the result is the assumed bound (warmup requests). Cost analysis and reporting return `HC0048` when required operation or document state is missing, when a non-warmup request reaches the analyzer with zero coerced variable sets (an explicit empty variable batch, `variables: []`), or when metrics cannot be attached to the execution-result state.
