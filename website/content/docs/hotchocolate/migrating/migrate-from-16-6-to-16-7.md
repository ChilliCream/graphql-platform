---
title: Migrate Hot Chocolate from 16.6 to 16.7
metaTitle: "Hot Chocolate 16.7 Migration Guide"
description: "Migration guide for Hot Chocolate v16.6 to v16.7: update cost analysis, implement the new WebSocket connection initialization diagnostic event, replace raw condition masks with ConditionFlags, and configure wide operation limits."
---

Update every `HotChocolate.*` package in the application to version 16.7 before applying these changes.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## IServerDiagnosticEvents gained a WebSocket connection initialization event

`IServerDiagnosticEvents` has a new `WebSocketConnectionInitialized` member. It is raised once per WebSocket session, for both the `graphql-transport-ws` and the legacy `graphql-ws` protocol, after the client's connection initialization message has been accepted.

Listeners that derive from `ServerDiagnosticEventListener` need no change, because the base class provides a virtual no-op. Types that implement `IServerDiagnosticEvents` directly have to implement the new member:

```csharp
public void WebSocketConnectionInitialized(
    ISocketSession session,
    IOperationMessagePayload connectionInitMessage)
{
}
```

The payload of `connectionInitMessage` is only valid for the duration of the call. Read out any value that is needed later inside the callback, for example onto `ISocketConnection.Features`.

## Cost variable multipliers retired

Cost analysis now evaluates coerced variable values directly. The filter and sort variable multipliers are inert in 16.7 and marked `[Obsolete(error: true)]`, so code that accesses them fails to compile. They will be removed in a later release.

The positional `RequestCostOptions` constructors and `Deconstruct` overload that expose the filter variable multiplier are also marked `[Obsolete(error: true)]`. The replacement positional shape includes `skipAnalyzer` and replaces the multiplier with `maxResponseSize`. Record `with` expressions that do not access an obsolete member continue to compile.

| Deprecated 16.6 member                                              | 16.7 replacement                                                       |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| `FilterCostOptions.VariableMultiplier`                              | Remove the setting. Coerced filter values are priced directly.         |
| `SortCostOptions.VariableMultiplier`                                | Remove the setting. Coerced sort values are priced directly.           |
| `RequestCostOptions.FilterVariableMultiplier`                       | Remove the setting.                                                    |
| `RequestCostOptions(double, double, bool, int?)`                    | `RequestCostOptions(double, double, bool, bool, double?)`              |
| `RequestCostOptions(double, double, bool, bool, int?)`              | `RequestCostOptions(double, double, bool, bool, double?)`              |
| `Deconstruct(out double, out double, out bool, out bool, out int?)` | `Deconstruct(out double, out double, out bool, out bool, out double?)` |

Update positional construction as follows:

```diff
 var requestOptions = new RequestCostOptions(
     maxFieldCost: 1_000,
     maxTypeCost: 1_000,
     enforceCostLimits: true,
-    filterVariableMultiplier: 5);
+    skipAnalyzer: false,
+    maxResponseSize: null);
```

`ICostMetricsCache` and `DefaultCostMetricsCache` have been removed. Compiled cost plans are cached internally, so no replacement cache service registration is required.

## Document normalization and cost analyzer placement changed

Document normalization, i.e. flattening the selected operation's fragments into its own selection set, is no longer a pipeline stage. It is a lazy `IOperationDocumentNormalizer` service instead: a middleware that needs the normalized document calls `context.GetNormalizedDocument()`, which normalizes once per request on first access and stores the result on the document info. It is resolved by the first stage that needs it, which is now variable coercion, and cached by operation id in the normalized-document cache, so a later stage or a later request for the same operation never rewrites the document a second time. The default, persisted-operation, and automatic-persisted-operation pipelines use this order:

```text
DocumentValidation -> OperationVariableCoercion -> CostAnalyzer -> OperationCache -> OperationCompiler -> SkipWarmupExecution
```

The `CostAnalyzer` stage is present once `AddCostAnalyzer()` has run, which `AddGraphQLServer()` does.

`OperationResolverMiddleware`/`UseOperationResolver()` are renamed to `OperationCompilerMiddleware`/`UseOperationCompiler()`; the compiler now asks the normalizer service for the normalized document itself on an operation cache miss, instead of relying on a prior pipeline stage. The old names remain available as `[Obsolete]` forwarders that resolve to the same middleware key, so an existing `before:`/`after:` insertion that references `WellKnownRequestMiddleware.OperationResolverMiddleware` keeps working.

In 16.6, the default order was `OperationCache -> OperationResolver -> SkipWarmupExecution -> OperationVariableCoercion`, with `AddCostAnalyzer()` inserting directly after `DocumentValidation`, ahead of everything else. In 16.7, HotChocolate aligns its stage order with Fusion's: variable coercion now runs directly after document validation, ahead of the operation cache and the operation compiler, and `AddCostAnalyzer()` still inserts after the keyed variable-coercion middleware, so it lands between coercion and the operation cache. Consequence: a request that cost analysis rejects is never compiled and never enters the operation cache, and enforcement still sees coerced variables. Because the operation compiler now runs after coercion, `OperationVariableCoercionMiddleware` reads `context.GetNormalizedDocument()` directly instead of the compiled operation; a custom middleware inserted `before:`/`after:` the coercion stage can no longer rely on `context.TryGetOperation()` succeeding there. A custom pipeline that used `before:`/`after:` relative to any of these stages must account for the new anchors. A custom pipeline applies this delta:

```diff
 builder
     .AddGraphQL()
     .AddCostAnalyzer()
     // ... parsing, validation ...
-    .UseOperationCache()
-    .UseOperationResolver()
-    .UseSkipWarmupExecution()
-    .UseOperationVariableCoercion()
+    .UseOperationVariableCoercion()
+    .UseOperationCache()
+    .UseOperationCompiler()
+    .UseSkipWarmupExecution()
     .UseConcurrencyGate()
     .UseOperationExecution();
```

`OperationVariableCoercion` and `CostAnalyzer` no longer special-case warmup requests: `SkipWarmupExecution` is the only stage that checks whether a request is a warmup request, so a warmup request is coerced and cost-analyzed exactly like any other request before that stage stops it from executing. `GraphQL-Cost: validate` requests always run variable coercion, matching `execute`/`report` (2026-09-14 user ruling); a `validate` request without required variables fails with the ordinary variable-coercion error. The assumed bound is not exposed through the request pipeline.

## Omitted list-size requirement now enforces

A schema-first `@listSize` usage with `slicingArguments` now uses the directive definition's default for `requireOneSlicingArgument`. The built-in definition defaults it to `true`. Static validation counts non-null literal slicing arguments and returns `HC0082` when the count is zero or greater than one. Explicit null does not count. When every non-null slicing argument is variable-bound, validation is deferred because its presence is not known yet.

Write `requireOneSlicingArgument: false` to retain the 16.6 relaxation:

```diff
-@listSize(slicingArguments: ["first", "last"])
+@listSize(
+  slicingArguments: ["first", "last"]
+  requireOneSlicingArgument: false
+)
```

Generated paging annotations already write the setting explicitly. They write `false` by default and `true` when `RequirePagingBoundaries` is enabled.

# Deprecations

## Raw condition masks replaced by ConditionFlags

Hot Chocolate can now compile and execute operations with more than 64 distinct `@skip`/`@include` conditions or `@defer` conditions. `MaxAllowedIncludeConditions` limits the combined `@skip` and `@include` conditions, and `MaxAllowedDeferConditions` limits the `@defer` conditions. Both limits default to **1,024**. An operation that exceeds either limit produces a GraphQL request error during operation compilation.

Configure the limits through `RequestExecutorOptions`:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(options =>
    {
        options.MaxAllowedIncludeConditions = 2_048;
        options.MaxAllowedDeferConditions = 2_048;
    });
```

`ConditionFlags` contains the first 64 evaluated conditions and any remaining conditions. Replace `IResolverContext.IncludeFlags` and the raw `IsIncluded` overload with their `ConditionFlags` equivalents:

```diff
- ulong includeFlags = context.IncludeFlags;
- bool included = selection.IsIncluded(includeFlags);
+ ConditionFlags includeFlags = context.IncludeConditionFlags;
+ bool included = selection.IsIncluded(includeFlags);
```

`ISelectionVisitorContext.IncludeFlags` is also replaced by `IncludeConditionFlags`:

```diff
- ulong includeFlags = visitorContext.IncludeFlags;
+ ConditionFlags includeFlags = visitorContext.IncludeConditionFlags;
```

Pass the same `ConditionFlags` value to `SelectionEnumerator` and `AsSelector<T>`:

```diff
- var enumerator = new SelectionEnumerator(selectionSet, context.IncludeFlags);
- var selector = selection.AsSelector<Product>(context.IncludeFlags);
+ var enumerator = new SelectionEnumerator(selectionSet, context.IncludeConditionFlags);
+ var selector = selection.AsSelector<Product>(context.IncludeConditionFlags);
```

The complete Core selection migration is:

| Deprecated 16.6 member                             | 16.7 replacement                                            |
| -------------------------------------------------- | ----------------------------------------------------------- |
| `ISelection.IsIncluded(ulong)`                     | `ISelection.IsIncluded(ConditionFlags)`                     |
| `ISelection.IsDeferred(ulong)`                     | `ISelection.IsDeferred(ConditionFlags)`                     |
| `Selection.IsSkipped(ulong)`                       | `Selection.IsSkipped(ConditionFlags)`                       |
| `Selection.IsIncluded(ulong)`                      | `Selection.IsIncluded(ConditionFlags)`                      |
| `Selection.IsDeferred(ulong)`                      | `Selection.IsDeferred(ConditionFlags)`                      |
| `Selection.IsDeferred(ulong, DeferUsage?)`         | `Selection.IsDeferred(ConditionFlags, DeferUsage?)`         |
| `Selection.GetPrimaryDeferUsage(ulong)`            | `Selection.GetPrimaryDeferUsage(ConditionFlags)`            |
| `Selection.GetActiveDeferUsages(ulong)`            | `Selection.GetActiveDeferUsages(ConditionFlags)`            |
| `Selection.HasActiveDeferUsage(ulong, DeferUsage)` | `Selection.HasActiveDeferUsage(ConditionFlags, DeferUsage)` |
| `IResolverContext.IncludeFlags`                    | `IResolverContext.IncludeConditionFlags`                    |
| `SelectionEnumerator(SelectionSet, ulong)`         | `SelectionEnumerator(SelectionSet, ConditionFlags)`         |
| `AsSelector<TValue>(this ISelection, ulong)`       | `AsSelector<TValue>(this ISelection, ConditionFlags)`       |
| `AsSelector<TValue>(this Selection, ulong)`        | `AsSelector<TValue>(this Selection, ConditionFlags)`        |

The Data projection APIs accept the same `ConditionFlags` value. Replace every raw `Select` sink as follows:

| Deprecated 16.6 extension signature                                             | 16.7 replacement                                                                         |
| ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `Select<TKey, TValue>(this IDataLoader<TKey, TValue>, ISelection, ulong)`       | `Select<TKey, TValue>(this IDataLoader<TKey, TValue>, ISelection, ConditionFlags)`       |
| `Select<TKey, TValue>(this IDataLoader<TKey, TValue[]>, ISelection, ulong)`     | `Select<TKey, TValue>(this IDataLoader<TKey, TValue[]>, ISelection, ConditionFlags)`     |
| `Select<TKey, TValue>(this IDataLoader<TKey, List<TValue>>, ISelection, ulong)` | `Select<TKey, TValue>(this IDataLoader<TKey, List<TValue>>, ISelection, ConditionFlags)` |
| `Select<TKey, TValue>(this IDataLoader<TKey, Page<TValue>>, ISelection, ulong)` | `Select<TKey, TValue>(this IDataLoader<TKey, Page<TValue>>, ISelection, ConditionFlags)` |
| `Select<T>(this IQueryable<T>, Selection, ulong)`                               | `Select<T>(this IQueryable<T>, Selection, ConditionFlags)`                               |

At call sites, pass `IncludeConditionFlags` instead of `IncludeFlags` to each overload:

```diff
- valueLoader.Select(selection, context.IncludeFlags);
- arrayLoader.Select(selection, context.IncludeFlags);
- listLoader.Select(selection, context.IncludeFlags);
- pageLoader.Select(selection, context.IncludeFlags);
- queryable.Select(selection, context.IncludeFlags);
+ valueLoader.Select(selection, context.IncludeConditionFlags);
+ arrayLoader.Select(selection, context.IncludeConditionFlags);
+ listLoader.Select(selection, context.IncludeConditionFlags);
+ pageLoader.Select(selection, context.IncludeConditionFlags);
+ queryable.Select(selection, context.IncludeConditionFlags);
```

The deprecated evaluation overloads continue to work for operations with at most 64 conditions. When an operation has more than 64 conditions of the corresponding kind, the deprecated raw inclusion overloads throw `InvalidOperationException` for every conditional selection and the deprecated raw defer overloads throw for every deferrable selection, including selections whose own conditions are all among the first 64; raw inclusion evaluation does not throw for an unconditional selection, and raw defer evaluation does not throw for a non-deferrable selection. The deprecated `SelectionEnumerator` and projection overloads throw for wider include operations. The deprecated `IncludeFlags` properties expose only the first 64 flags. Releases before 16.7 rejected operations with more than 64 conditions during compilation.

# Behavioral breaking changes

## Cost estimates use coerced request values

Cost plans are compiled once and evaluated for each request. Supplied slicing variables, Boolean `@include` and `@skip` conditions, and variable-supplied input objects now affect the estimate that is reported and enforced.

In 16.7, one request is one invocation of the request pipeline, and a variable batch is one request. Cost enforcement sums the field cost and type cost across all variable sets and compares those sums with `MaxFieldCost` and `MaxTypeCost`. If either sum exceeds its limit, the whole request is rejected before any variable set executes, with one `HC0047` result. This prevents a client from splitting an expensive workload among variable sets that each stay under the limit. Maximum response size remains enforced per variable set. `MaxResponseSize` is checked per variable set, not summed. If any set exceeds `MaxResponseSize`, the whole request is likewise rejected before any set executes, with one `HC0047` result. When multiple sets violate this limit, the first violating set determines the reported `maxResponseSize`.

The following calculation rules also change:

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
5. `CostOptions.DefaultListSize`.

An explicit null slicing argument is not an integer value and suppresses that argument's schema default. An undefined slicing variable behaves as an absent argument. When no schema argument default applies, both cases fall through to `slicingArgumentDefaultValue`.

## Default list size is 50

On 16.6, an unannotated, non-paginated list assumed a size of 1. `CostOptions.DefaultListSize` is a `double` and on 16.7 defaults to `PagingDefaults.MaxPageSize` (`50`), so that same unannotated list now assumes 50 elements. A deeply nested unannotated selection that passed on 16.6 can now exceed `MaxTypeCost`, because the assumed size compounds across nesting levels. Annotate the field with `@listSize(assumedSize:)` or set `DefaultListSize` for the schema, to any non-negative finite number or `double.PositiveInfinity`:

```diff
 builder
     .AddGraphQLServer()
-    .AddQueryType<Query>();
+    .AddQueryType<Query>()
+    .ModifyCostOptions(options => options.DefaultListSize = 100);
```

# Noteworthy changes

## New cost options

| Option                       | Type                          | Default | Contract                                                                                                                 |
| ---------------------------- | ----------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------ |
| `DefaultListSize`            | `double`                      | `50`    | Size for a list without applicable `@listSize` metadata; sourced from `PagingDefaults.MaxPageSize`.                      |
| `MaxResponseSize`            | `double?`                     | `null`  | Maximum response-object-field count. `null` disables this check and metric.                                              |
| `CostPlanCacheSize`          | `int`                         | `256`   | Maximum compiled cost plans cached per schema.                                                                           |
| `CaseBudget`                 | `int?`                        | `null`  | Exact cases evaluated per operation before falling back per `CaseBudgetExceededBehavior`. `null` uses the default (510). |
| `CaseBudgetExceededBehavior` | `CaseBudgetExceededBehavior?` | `null`  | Behavior once compiling one operation exhausts `CaseBudget`. `null` uses the default (`EvaluatePerRequest`).             |

When `MaxResponseSize` is enabled, `extensions.operationCost` includes `maxResponseSize`. A rejection includes `{ maxResponseSize, maxAllowedResponseSize }` in the error extensions and uses error code `HC0047`.

## Reporting and result access

`GraphQL-Cost: validate` always coerces variables and reports the evaluated cost, exactly like `execute`/`report` (2026-09-14 user ruling; the value changes from an earlier 16.7 preview, where `validate` without variables reported the assumed bound instead of coercing). A required variable that is not supplied fails the request with the ordinary variable-coercion error. Coercion succeeding, `validate` does not execute the operation, returns no `data`, and remains HTTP 200 even when the reported value exceeds a configured limit. A variable batch in validate mode returns one extensions-only result per variable set, with that set's `operationCost`. A successful variable batch in report mode also includes one `operationCost` per result.

Positive infinite values in `extensions.operationCost` and cost error extensions are serialized as the JSON string `"Infinity"`. A `GraphQL-Cost: report` rejection includes `operationCost` alongside the error. For a rejected variable batch, the single rejection result contains one `operationCost`: its `fieldCost` and `typeCost` are the sums across all variable sets, and it contains `maxResponseSize` only for a response-size rejection, using the first violating set's value.

Cost rejections, including the single result for a rejected variable batch, return HTTP 400 when the response media type is `application/graphql-response+json`; legacy `application/json` responses remain HTTP 200.

Request batching is an array of independent requests in one HTTP request. Cost limits currently apply separately to each independent request in a request batch. Summing costs across an entire request batch is planned, with no target version.

Use `RequestContext.TryGetCostAnalysisResult(out var result)` to access the compiled `CostPlan` and all estimates for the request. Cost analysis and reporting return `HC0048` when required operation or document state is missing, when a request reaches the analyzer with zero coerced variable sets (an explicit empty variable batch, `variables: []`), or when metrics cannot be attached to the execution-result state.
