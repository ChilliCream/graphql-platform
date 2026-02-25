# Defer Bug: Incomplete Stream with `hasNext: true`

## Problem

A query with 3 `@defer` fragment spreads on the same object type produces an incomplete
incremental delivery stream. The stream terminates with `hasNext: true` and some pending
IDs are never completed. The stream runs for ~1 second then stops producing payloads.

Specifically, 3 of 7 edge node deliveries (ids 17, 19, 21) are announced as pending but
never completed/delivered.

## Status: FIXED

All three root causes have been identified and fixed. All tests pass:

- **6/6 DeferSiblingTests** (new integration tests)
- **14/14 DeferTests** (existing tests — no regressions)
- **49/49 OperationCompilerTests** (existing tests — no regressions)

## Root Causes and Fixes

### Root Cause 1: Single-primary defer assignment forced sibling defers into parent-child relationships

**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/Selection.cs`

When a field belonged to multiple sibling `@defer` scopes (e.g., `metrics` in both FragmentA
and FragmentC), `GetPrimaryDeferUsage()` picked ONE winner. This forced sibling defers into
a parent-child relationship at runtime, creating per-list-item nested branches (~30 pending
IDs instead of 3).

**Fix**: Changed from single-primary to set-based approach matching graphql-js spec:

1. **`IsDeferred(ulong deferFlags)`**: Changed from AND semantics (`== _deferMask`) to ANY
   semantics (`!= 0`). A field with `_deferMask = bit_a | bit_c` is now correctly considered
   deferred when only A is active.

2. **`IsDeferred(ulong deferFlags, DeferUsage? parentDeferUsage)`**: Changed from single
   primary comparison to set membership check via `HasActiveDeferUsage()`.

3. **Added `GetActiveDeferUsages(ulong deferFlags)`**: Returns all active DeferUsages with
   parent-child pruning (matches graphql-js `getFilteredDeferUsageSet`).

4. **Added `HasActiveDeferUsage(ulong deferFlags, DeferUsage target)`**: Checks if target
   is in the active DeferUsage set.

### Root Cause 2: deferUsage not propagated through resolver task chain

**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTaskFactory.cs`

In `EnqueueOrInlineResolverTasks`, when creating resolver tasks for non-deferred fields,
`parentDeferUsage` and `parentBranchId` were NOT passed to `CreateResolverTask`. This caused
child resolver tasks to have `deferUsage = null`, so at deeper nesting levels
`IsDescendantOf(anyDefer, null)` returned `true`, creating spurious per-list-item branches.

**Fix**: Pass `context.ParentBranchId` and `parentDeferUsage` in both the `HasIncrementalParts`
and non-`HasIncrementalParts` code paths. Also added `IsDescendantOf` helper to skip sibling
defers (only create branches for descendant DeferUsages).

### Root Cause 3: Race condition in DeferExecutionCoordinator.ReadResultsAsync

**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/DeferExecutionCoordinator.cs`

`_isComplete` was read OUTSIDE the lock after the result snapshot was taken. A final delivery
could set `_isComplete = true` and add its result to `_results` between the snapshot read and
the completion check. The reader would see `_isComplete = true` but miss the last result
(the one with `hasNext: false`).

Diagnostic output confirmed: 3 pending IDs announced (a=2, b=3, c=4), but only 2 completed
(a=2, c=4). Branch b (id=3) was never delivered due to the race.

**Fix**: Read `_isComplete` INSIDE the same lock as the snapshot:

```csharp
bool isComplete;
lock (_sync)
{
    snapshot ??= [];
    snapshot.Clear();
    snapshot.AddRange(_results);
    _results.Clear();
    isComplete = _isComplete;
}
```

### Root Cause 4: ResultDocument filtered by single primary instead of set membership

**File**: `src/HotChocolate/Core/src/Types/Text/Json/ResultDocument.cs`

The deferred `CreateObject` overload used `selection.GetPrimaryDeferUsage(deferFlags) == deferUsage`
to filter fields for a DeferTask. This meant C's DeferTask couldn't see `metrics` because A
was the "primary".

**Fix**: Changed to `selection.HasActiveDeferUsage(deferFlags, deferUsage)` so that any
DeferTask whose DeferUsage is in the field's active set can see the field.

## Files Changed

| File                                                                                | Change                                                                             |
| ----------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `src/HotChocolate/Core/src/Types/Execution/Processing/Selection.cs`                 | Set-based defer, `GetActiveDeferUsages`, `HasActiveDeferUsage`, fixed `IsDeferred` |
| `src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTaskFactory.cs` | Propagate deferUsage/branchId, `IsDescendantOf`, skip sibling defers               |
| `src/HotChocolate/Core/src/Types/Execution/Processing/DeferExecutionCoordinator.cs` | Fix race condition: read `_isComplete` inside lock                                 |
| `src/HotChocolate/Core/src/Types/Text/Json/ResultDocument.cs`                       | Set membership filter for deferred fields                                          |
| `src/HotChocolate/Core/test/Execution.Tests/DeferSiblingTests.cs`                   | 6 new integration tests                                                            |

## Integration Tests

`src/HotChocolate/Core/test/Execution.Tests/DeferSiblingTests.cs`:

1. **Three_Sibling_Defers_With_Overlapping_Paths** — 3 sibling defers where A and C overlap
   on `metrics.subgraphs.insights.edges.node`. Verifies exactly 3 pending IDs, all completed,
   `hasNext: false`.

2. **Two_Sibling_Defers_Same_Root_Field_Different_Subselections** — Simplest overlap case.
   Verifies exactly 2 pending IDs, all completed.

3. **Three_Sibling_Defers_All_Overlapping_On_Same_Field** — All 3 defers overlap on same
   deeply nested path. Verifies exactly 3 pending IDs, all completed.

4. **Sibling_Defers_Over_List_Field_Do_Not_Explode_Pending_IDs** — Verifies the fix doesn't
   create per-list-item branches (core of the production bug). Exactly 2 pending IDs, NOT
   N \* listLength.

5. **Sibling_Defer_With_Nested_Defer_Inside** — Nested @defer inside a sibling @defer.
   Verifies nested defers still work correctly while sibling defers are skipped.

6. **Single_Defer_Still_Works** — Basic regression test for single defers.

## Spec Alignment

The fix aligns HC with the graphql-js reference implementation:

| Aspect             | Before (broken)                            | After (fixed)                                 |
| ------------------ | ------------------------------------------ | --------------------------------------------- |
| Per-field tracking | Single primary `DeferUsage`                | Set of all active DeferUsages                 |
| Sibling handling   | First one wins, other becomes nested child | Both stay in set, independent DeferTasks      |
| IsDeferred check   | ALL bits must match (AND)                  | ANY bit matches                               |
| Pending IDs        | One per branch discovery (per list item)   | One per `@defer` directive                    |
| Branch creation    | Sibling defers create nested branches      | Only descendant defers create nested branches |
| Result filtering   | Single primary match                       | Set membership                                |
