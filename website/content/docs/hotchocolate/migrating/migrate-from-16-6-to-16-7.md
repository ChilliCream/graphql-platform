---
title: Migrate Hot Chocolate from 16.6 to 16.7
metaTitle: "Hot Chocolate 16.7 Migration Guide"
description: "Migration guide for Hot Chocolate v16.6 to v16.7: update cost analysis and replace raw condition masks with ConditionFlags."
---

Update every `HotChocolate.*` package in the application to version 16.7 before applying these changes.

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

# Breaking changes

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

## Cost analyzer pipeline placement changed

`CostAnalyzerMiddleware` now runs after `OperationVariableCoercionMiddleware`. When `AddCostAnalyzer()` is registered, the default, persisted-operation, and automatic-persisted-operation pipelines use this order.

`AddCostAnalyzer()` inserts the analyzer after the keyed variable-coercion middleware. A custom pipeline must contain that middleware, and variable coercion must precede the remaining execution stages:

```diff
 builder
     .AddGraphQL()
     .AddCostAnalyzer()
     // ... parsing, validation, operation cache ...
     .UseOperationResolver()
-    .UseSkipWarmupExecution()
     .UseOperationVariableCoercion()
+    .UseSkipWarmupExecution()
     .UseConcurrencyGate()
     .UseOperationExecution();
```

Warmup requests and `GraphQL-Cost: validate` requests without variables use the static-bound path without variable coercion.

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

# Behavioral breaking changes

## Cost estimates use coerced request values

Cost plans are compiled once and evaluated for each request. Supplied slicing variables, Boolean `@include` and `@skip` conditions, and variable-supplied input objects now affect the estimate that is reported and enforced. Supported variable batches in report or validate mode produce one result per variable set, with each result carrying that set's `operationCost`. When an executing variable batch enforces cost limits, only offending indices return `HC0047`; accepted query indices still execute and return their data.

The following calculation rules also change:

- Complementary `@include` and `@skip` branches are treated as mutually exclusive.
- Fields are collected by response name before signed weights are applied. Clamping happens after the complete field-call sum and per-instance type sum are calculated.
- An interface or union return weight is the signed maximum of its member object-type weights.
- A field selected through an interface is priced through each possible object type's field metadata.
- Output fields that return lists of scalars retain Hot Chocolate's weight `1`, now as an explicit `@cost(weight: "1")` in printed SDL.
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

## Default list size is Infinity

`CostOptions.DefaultListSize` is a `double` and now defaults to `double.PositiveInfinity`. An unannotated, non-paginated list whose element type has a non-zero weight exceeds every finite type-cost limit. Annotate the field with `@listSize(assumedSize:)` or set `DefaultListSize` for the schema:

```diff
 builder
     .AddGraphQLServer()
-    .AddQueryType<Query>();
+    .AddQueryType<Query>()
+    .ModifyCostOptions(options => options.DefaultListSize = 100);
```

# Noteworthy changes

## New cost options

| Option              | Type      | Default    | Contract                                                                    |
| ------------------- | --------- | ---------- | --------------------------------------------------------------------------- |
| `DefaultListSize`   | `double`  | `Infinity` | Size for a list without applicable `@listSize` metadata.                    |
| `MaxResponseSize`   | `double?` | `null`     | Maximum response-object-field count. `null` disables this check and metric. |
| `CostPlanCacheSize` | `int`     | `256`      | Maximum compiled cost plans cached per schema.                              |
| `CaseBudget`        | `int?`    | `null`     | Exact cases evaluated per operation. `null` uses the engine default.        |

When `MaxResponseSize` is enabled, `extensions.operationCost` includes `maxResponseSize`. A rejection includes `{ maxResponseSize, maxAllowedResponseSize }` in the error extensions and uses error code `HC0047`.

## Reporting and result access

`GraphQL-Cost: validate` without variables reports the static bound. It does not execute the operation, returns no `data`, and remains HTTP 200 even when the reported value exceeds a configured limit. With variables, it reports the evaluated cost.

Positive infinite values in `extensions.operationCost` and cost error extensions are serialized as the JSON string `"Infinity"`. A `GraphQL-Cost: report` rejection includes `operationCost` alongside the error.

Single-result cost rejections are request errors. They return HTTP 400 when the response media type is `application/graphql-response+json`; legacy `application/json` responses remain HTTP 200. A variable batch with rejected indices returns an `OperationResultBatch` and remains HTTP 200 for either media type.

Use `RequestContext.TryGetCostAnalysisResult(out var result)` to access the compiled `CostPlan`, all estimates for the request, and whether the result is a static bound. Cost analysis and reporting return `HC0048` when required operation or document state is missing, or when metrics cannot be attached to the execution-result state.
