---
title: Migrate Hot Chocolate from 16.6 to 16.7
metaTitle: "Hot Chocolate 16.7 Migration Guide"
description: "Migration guide for Hot Chocolate v16.6 to v16.7: replace raw condition masks with ConditionFlags and configure wide operation limits."
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

The deprecated evaluation overloads continue to work for operations with at most 64 conditions. On a wider operation, they throw `InvalidOperationException` when a conditional or deferrable selection requires flags beyond the first 64. The deprecated `SelectionEnumerator` and projection overloads throw for wider include operations. The deprecated `IncludeFlags` properties expose only the first 64 flags. Releases before 16.7 rejected operations with more than 64 conditions during compilation.
