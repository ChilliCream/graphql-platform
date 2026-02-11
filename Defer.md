# Defer and Stream PR 1110

## Reference Locations
- **GraphQL Spec**: `/Users/michael/local/graphql-spec/public/draft/index.html`
- **Reference Implementation**: `/Users/michael/local/graphql-js`
- **Key Reference File**: `/Users/michael/local/graphql-js/src/execution/incremental/buildExecutionPlan.ts`

## Overview
We're integrating GraphQL `@defer` directive support into Hot Chocolate's execution engine. The defer directive allows incremental delivery of GraphQL responses - the initial response returns immediately with non-deferred fields, and deferred fragments arrive in subsequent payloads.

## Key Concepts

### DeferUsage
- Represents a `@defer` directive occurrence in a query
- Forms a parent chain for nested defer scopes
- Has properties: `Label`, `Parent`, `DeferConditionIndex`
- The `DeferConditionIndex` maps to a bit position in runtime defer flags (ulong bitmask)

### Defer Flags
- Runtime bitmask (ulong) indicating which defer conditions are active
- Each bit corresponds to a defer condition (supports up to 64 defer directives)
- Variables like `@defer(if: $var)` are evaluated at runtime, not compile-time

### Branch IDs
- Execution branches represent different execution contexts
- Branch IDs are **scheduler-issued** via `WorkScheduler.NextBranchId()` (atomic counter)
- Each operation's main branch gets a unique ID at initialization (no more constant `MainBranchId`)
- Each deferred fragment gets its own unique branch ID
- `SystemBranchId = -1` is reserved for orchestrator tasks (DeferTask) — **not tracked** for completion
- This ensures uniqueness across variable-batched operations sharing a scheduler
- Branch task counts are tracked in `WorkScheduler._branchTaskCount` (Dictionary<int, int>)
- `DeferExecutionCoordinator` tracks parent-child branch relationships and defer usage mappings

### Primary Defer Usage
- When a selection has multiple defer usages (nested defers), we need to find the "primary" one
- The primary defer usage is the **outermost** active defer that isn't covered by a parent
- This determines which execution branch the selection belongs to

## Completed Work

### 1. WorkQueue Priority System
**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/WorkQueue.cs`

Changed from single `Stack<IExecutionTask>` to two stacks for priority-based execution:
```csharp
private readonly Stack<IExecutionTask> _immediateStack = new();
private readonly Stack<IExecutionTask> _deferredStack = new();
```

**Why two stacks instead of PriorityQueue?**
- Binary priority levels (immediate vs deferred) don't need heap overhead
- Stack<T> is O(1) for push/pop vs O(log n) for PriorityQueue
- Better cache locality with Stack<T>
- Zero-allocation when pooled

**Logic**:
- `Push()` routes tasks to immediate or deferred stack based on `IsDeferred`
- `TryTake()` always tries immediate stack first, then deferred stack
- Ensures initial response completes as fast as possible

### 2. IExecutionTask.IsDeferred Property
**File**: `src/HotChocolate/Core/src/Abstractions/Execution/Tasks/IExecutionTask.cs`

Added property:
```csharp
bool IsDeferred { get; }
```

**Note**: Named "IsDeferred" for general execution engine concept of deprioritized tasks, not GraphQL-specific

### 3. ResolverTask Branch Tracking
**Files**:
- `src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTask.cs`

Added properties:
```csharp
internal int BranchId { get; private set; }
internal DeferUsage? DeferUsage { get; private set; }

public bool IsDeferred => DeferUsage is not null;
```

**Why both BranchId and DeferUsage?**
- `BranchId` is used by coordinator to track which branch this task belongs to
- `DeferUsage` is used when creating child tasks to determine if they need a new branch
- When child's primary defer usage differs from parent's, a new branch is created

### 4. Selection.GetPrimaryDeferUsage() Method

**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/Selection.cs`

Finds the primary (outermost) active defer usage for a selection at runtime. Handles conditional defers by walking up the parent chain when a defer is inactive.

**Algorithm**:

1. For each entry in `_deferUsage`, walk up the parent chain to find the **nearest active** defer (bit set in `deferFlags`).
2. If any entry resolves to no active defer at all (walked to root) → return `null`. The field has a non-deferred occurrence and belongs in the initial response.
3. Among all resolved effective defers, keep the **outermost** (the one that is an ancestor of others).

**Fast path**: Single defer usage (most common) — just walks up the parent chain and returns the first active one.

**Example scenarios** with `_deferUsage = [B]` where B.parent = A:

| A (conditional) | B | Result | Why |
|---|---|---|---|
| active | active | B | B is nearest active |
| inactive | active | B | A disabled, B still defers |
| active | inactive | A | B disabled, folds into A's scope |
| inactive | inactive | null | No active defer in chain |

## Key Decisions & Rationale

### 1. Walk parent chain on inactive defer (not immediate null)

Previously returned null if ANY defer usage was inactive. Now walks up the parent chain to find the nearest active ancestor. This handles conditional outer defers: when `@defer(if: $var)` is disabled, the content folds into its parent scope, but a parent `@defer` may still be active.

**Return null** only when walking the full chain finds no active defer — meaning the field truly has a non-deferred occurrence.

### 2. Use reference equality for DeferUsage identity

DeferUsage is a sealed record and instances are interned during compilation. Reference equality (`==`) correctly identifies the same defer directive across parent chains and array entries.

### 3. Outermost wins among multiple effective defers

When `_deferUsage` has multiple entries that resolve to different active defers, the outermost (ancestor) is kept as primary. If two are unrelated (different branches), the first is kept (single-return API limitation — reference impl returns a set).

## Reference Implementation Notes

From `graphql-js/src/execution/incremental/buildExecutionPlan.ts`:

### `getFilteredDeferUsageSet()` (lines 51-75)

1. Collects all defer usages from field details
2. **If ANY field has `undefined` deferUsage**, clears the set and returns empty
3. For remaining defer usages, removes children whose parents are also in the set (walks full ancestor chain)
4. What remains are the outermost (primary) defer usages

This matches our approach:
- ✅ Check all defer usages are active (our lines 318-322)
- ✅ Check if parent is in array using reference equality (our lines 330-339)
- ✅ Return first uncovered defer usage (our line 344)

### `buildExecutionPlan()` (lines 17-49) — Branching Logic

Takes a `groupedFieldSet` and `parentDeferUsages` (the defer context of the current branch).
For each field:

1. Computes `filteredDeferUsageSet` via `getFilteredDeferUsageSet()`
2. If `filteredDeferUsageSet === parentDeferUsages` → field stays in current branch (no new branch)
3. If different → field goes into a **new branch** keyed by its defer usage set

**Branch creation rule**: A new branch is created at the **boundary** where a field's primary defer usage differs from the parent task's defer usage. Fields inside the same defer scope inherit the parent's branch.

Example:

```graphql
{
  ... @defer {        # A
    bar {             # primary = A, parent branch has no defer → new branch A
      age             # primary = A, parent branch = A → stays in branch A
      ... @defer {    # B (parent = A)
        baz           # primary = B, parent branch = A → new branch B
      }
    }
  }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────┐
│ GraphQL Query with @defer directives               │
└─────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│ Operation Compiler                                  │
│ - Builds Selection objects                          │
│ - Assigns DeferUsage[] to each selection            │
│ - Assigns defer flags (ulong bitmask)               │
└─────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│ Execution Engine creates ResolverTasks             │
│ - Calls GetPrimaryDeferUsage(deferFlags)            │
│ - Determines BranchId based on primary defer usage  │
│ - Sets task.BranchId and task.DeferUsage            │
└─────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│ WorkQueue.Push(task)                                │
│ - Checks task.IsDeferred                            │
│ - Routes to _immediateStack or _deferredStack       │
└─────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│ WorkQueue.TryTake()                                 │
│ - Tries _immediateStack first                       │
│ - Then _deferredStack                               │
│ - Ensures initial response completes first          │
└─────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│ DeferExecutionCoordinator                           │
│ - Tracks branches and their relationships           │
│ - Composes incremental results                      │
│ - Delivers responses in correct order               │
└─────────────────────────────────────────────────────┘
```

## Operation Compiler — DeferUsage Construction

**File**: `src/HotChocolate/Core/src/Types/Execution/Processing/OperationCompiler.cs`

### CollectFields (line 212)

Recursively walks the selection set. When an inline fragment has `@defer`:

1. Creates a `DeferCondition` and registers it in `DeferConditionCollection`
2. Creates a new `DeferUsage(label, parentDeferUsage, deferIndex)` — parent chain is correct
3. Recurses into the fragment's selections with the new DeferUsage as `parentDeferUsage`
4. Fields get the current scope's DeferUsage via `FieldSelectionNode(fieldNode, pathIncludeFlags, parentDeferUsage)`

### BuildSelectionSet (line 277) — Compile-Time Filtering

For each field (by response name), collects all `FieldSelectionNode` entries:

1. **Non-deferred check**: If ANY node has `DeferUsage == null`, the field has `hasNonDeferredNode = true` and is not deferred (lines 301, 342-344). Matches the reference impl's "clear set if any field detail has undefined deferUsage".
2. **Ancestor filtering** (lines 373-386): Walks the **full ancestor chain** (not just direct parent). If any ancestor is also in the list, removes the child. Leaves only outermost defer usages. Matches the reference impl exactly.
3. Produces `finalDeferUsage` array and `deferMask` bitmask, stored on the `Selection`.

### Compile-Time vs Runtime Filtering

The compile-time ancestor filtering in `BuildSelectionSet` (lines 373-386) is only safe for **constant** defers (unconditional `@defer` or `@defer(if: true)`). When a variable is involved (`@defer(if: $var)`), the child must be preserved in the `_deferUsage` array so `GetPrimaryDeferUsage` can evaluate it at runtime.

**Safe to filter at compile time** (both constant):

```graphql
... @defer {            # A (constant)
  ... @defer {          # B (constant, parent = A)
    field               # → [A] at compile time, correct
  }
}
```

**Must defer to runtime** (variable involved):

```graphql
... @defer(if: $a) {   # A (conditional)
  ... @defer {          # B (unconditional, parent = A)
    field               # → must keep [A, B], filter at runtime
  }
}
```

If A is disabled at runtime ($a = false), A's content executes immediately but B should still defer `field`. If B is discarded at compile time, `GetPrimaryDeferUsage` sees only [A], A's bit is off, returns null — incorrectly putting the field in the initial response.

**TODO**: Update `BuildSelectionSet` ancestor filtering to only remove a child when both the child and its covering ancestor are constant (no variable condition). Variable-dependent usages must stay in the array for runtime evaluation by `GetPrimaryDeferUsage`.

## Task List

### Done

- [x] `IExecutionTask.IsDeferred` property
- [x] `WorkQueue` dual-stack priority system (immediate/deferred)
- [x] `DeferUsage`, `DeferCondition`, `DeferConditionCollection` metadata types
- [x] `DeferUsageEnumerator` zero-allocation enumerator
- [x] `DeferExecutionCoordinator` branch tracking, result composition, streaming
- [x] `ResolverTask` BranchId + DeferUsage properties
- [x] `OperationContext.DeferFlags` (ulong bitmask)
- [x] `SelectionSet.HasDeferredSelections` flag
- [x] `Selection.GetPrimaryDeferUsage()` — runtime algorithm with parent chain walk
- [x] Operation compiler: `CollectFields` builds DeferUsage parent chain
- [x] Operation compiler: `BuildSelectionSet` compile-time ancestor filtering
- [x] `ResultDocument` per-defer-group constructor — scoped to selections matching a specific `DeferUsage`

### Execution Engine Integration

- [x] `ResolverTaskFactory.EnqueueRootResolverTasks()` — defer branch grouping, DeferTask creation, ArrayPool pattern
- [x] `ResolverTaskFactory.EnqueueOrInlineResolverTasks()` — defer-aware branching with `parentBranchId` from `ValueCompletionContext`
- [x] `IExecutionTask.BranchId` — added to interface, abstract on `ExecutionTask` base class
- [x] `ValueCompletionContext.ParentBranchId` — threads parent ResolverTask's BranchId through value completion
- [x] `OperationContext.DeferExecutionCoordinator` — per-operation (not shared via `_current*` pattern), returns `_deferExecutionCoordinator` directly
- [ ] Pool `DeferTask` — `OperationContext.CreateDeferTask()` currently does `new DeferTask()`, needs a pooled factory like `ResolverTask` has

### Scheduler — Branch Tracking

- [x] **Branch ID generation**: `BranchTracker` with `Interlocked.Increment` atomic counter
  - Each operation gets a unique main branch ID at initialization via `_currentBranchTracker.CreateNewBranchId()`
  - Defer branches get unique IDs via `DeferExecutionCoordinator.Branch()` → `_branchTracker.CreateNewBranchId()`
  - `BranchTracker.SystemBranchId = -1` is the only constant — for DeferTask orchestrators, not tracked for completion
  - `_current*` pattern for tracker/scheduler (not coordinator) ensures uniqueness across variable-batched operations
- [x] **Branch task counting**: `Dictionary<int, Branch>` in `WorkScheduler` with nested `Branch` class
  - `Register()`: inside `lock(_sync)`, creates `Branch` on first encounter, calls `branch.RegisterTask()`
  - `Complete()`: inside `lock(_sync)`, calls `branch.CompleteTask()`, removes and signals via `branch.Complete()` when count hits 0
  - `Clear()`: clears `_activeBranches` dictionary
  - `Branch` uses `AsyncManualResetEvent` for single-awaiter async signaling (~56 bytes, resettable)
  - Cancellation via `CancellationToken.Register` with proper `await using` disposal
- [x] **`WaitForCompletionAsync(branchId)`**: returns `ValueTask.CompletedTask` if branch already completed or never registered; otherwise delegates to `branch.WaitForCompletionAsync(operationContext.RequestAborted)`
- [x] **Initial payload signal**: `QueryExecutor.ExecuteIncrementalAsync` awaits `scheduler.WaitForCompletionAsync(branchId)` on main branch, then enqueues result via coordinator
- [x] Remove constant `DeferExecutionCoordinator.MainBranchId` — replaced with instance `_mainBranchId` set via `Initialize()`
- [x] Update `DeferExecutionCoordinator.Branch()` to use `_branchTracker.CreateNewBranchId()`
- [x] Update `OperationContext` to store assigned main branch ID (`_branchId` / `ExecutionBranchId`)

### Compiler

- [ ] Update `BuildSelectionSet` ancestor filtering to only remove constant defers; preserve variable-dependent usages for runtime evaluation

### Middleware — `OperationExecutionMiddleware` Simplification

**Done**: Removed `ITransactionScopeHandler` and all related types:

- [x] Deleted `ITransactionScopeHandler`, `ITransactionScope`, `DefaultTransactionScopeHandler`, `DefaultTransactionScope`, `NoOpTransactionScopeHandler`, `NoOpTransactionScope`
- [x] Deleted `RequestExecutorBuilderExtensions.TransactionScope.cs` (public DI extensions)
- [x] Removed from middleware: field, constructor param, factory resolution, `using var transactionScope` / `.Complete()` in mutation path
- [x] Removed `TryAddNoOpTransactionScopeHandler()` from `RequestExecutorServiceCollectionExtensions.CreateBuilder()`
- [x] Deleted `TransactionScopeHandlerTests.cs` and 2 snapshot files
- [x] 5-arg `ExecuteQueryOrMutationAsync` now returns `IExecutionResult` (supports `ResponseStream` for defer)

**Done** — method collapse and defer wiring:

- [x] Collapsed 4-arg + 5-arg `ExecuteQueryOrMutationAsync` into single method (one fewer async state machine)
- [x] Replaced commented-out defer block with `result.IsStreamResult()` → `result.RegisterForCleanup(operationContextOwner)` ownership transfer
- [x] Uncommented `IsOperationAllowed` — enforces `AllowStreams` flag for operations with `HasDeferredSelections`
- [x] Removed `ExecuteQueryOrMutationNoStreamAsync` — mutation batch now uses `ExecuteQueryOrMutationAsync`
- [x] Removed `ExecuteOperationRequestAsync` — inlined subscription/query/mutation dispatch into `InvokeAsync`
- [x] Unified variable batch path — removed query-only gate, single `ExecuteVariableBatchRequestAsync` handles both queries and mutations with shared scheduler for DataLoader batching
- [x] Relaxed `IsRequestTypeAllowed` — variable batch + defer is allowed (spec only forbids `@defer` on root mutation fields, enforced by validation)

### Result Delivery

- [x] Wire `DeferExecutionCoordinator` into the response stream
- [x] On branch completion: create `OperationResult` from defer group's `ResultDocument`, call `coordinator.EnqueueResult(result, branchId)`
- [ ] Ensure `path` passed to `coordinator.Branch()` is the **full path from query root** (not relative to defer group document)
- [x] Fix `ResultDocument.CreatePath` for defer groups — added `_rootPath` field, defaults to `Path.Root`, defer constructor accepts `path` parameter
- [x] Compose incremental payloads in correct delivery order — coordinator composes pending/incremental/completed with error-bubbling distinction
- [x] Handle `hasNext` flag on initial and subsequent payloads — set in `ComposeAndDeliverUnsafe`

### Serialization

- [x] `IncrementalObjectResult` — write actual data via `Formatter.WriteDataTo` or `null` on error
- [x] `IncrementalObjectResult` — write `subPath` when present
- [x] `CompletedResult` — write `errors` for failed incremental deliveries
- [ ] Investigate `JsonNullIgnoreCondition` impact on incremental result serialization — `JsonResultFormatter` carries this setting but unclear how it interacts with deferred data

### Validation Rules (Spec 5.7.4 & 5.7.5)

- [ ] `@defer` not allowed on root fields of mutation type
- [ ] `@defer` not allowed on root fields of subscription type
- [ ] `@defer` and `@stream` in subscription operations must have an `if` argument that can disable them

### Fusion

- [ ] Implement `HasDeferredSelections` in `Fusion.Execution` — `Fusion-vnext/src/Fusion.Execution/Execution/Nodes/Operation.cs:108` currently `throw new NotImplementedException()`
- [ ] Implement `Selection.IsDeferred(ulong deferFlags)` in `Fusion.Execution` — `Fusion-vnext/src/Fusion.Execution/Execution/Nodes/Selection.cs:161` currently `throw new NotImplementedException()`
- [ ] Implement `SelectionSet.HasDeferredSelections` in `Fusion.Execution` — `Fusion-vnext/src/Fusion.Execution/Execution/Nodes/SelectionSet.cs:64` currently `throw new NotImplementedException()`

### Subscriptions

- [ ] Handle `@defer` inside subscription payloads — `SubscriptionExecutor.Subscription.cs:192` currently calls `result.ExpectOperationResult()` which won't work if the result is a `ResponseStream`

### Testing

- [ ] Simple `@defer` — single deferred fragment
- [ ] Nested `@defer` — defer inside defer
- [ ] Conditional `@defer(if: $var)` — variable true/false
- [ ] Nested conditional/unconditional — `@defer(if: $var) { @defer { field } }`
- [ ] Field in multiple fragments — unrelated defers on same field
- [ ] `@defer` with label — verify label propagation

## Important Files

- `src/HotChocolate/Core/src/Types/Execution/Processing/Selection.cs` - GetPrimaryDeferUsage
- `src/HotChocolate/Core/src/Types/Execution/Processing/WorkQueue.cs` - Priority queue
- `src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTask.cs` - Branch tracking
- `src/HotChocolate/Core/src/Abstractions/Execution/Tasks/IExecutionTask.cs` - IsDeferred property
- `src/HotChocolate/Core/src/Types/Execution/Processing/DeferExecutionCoordinator.cs` - Branch coordinator
- `src/HotChocolate/Core/src/Types/Execution/Processing/DeferUsage.cs` - Defer metadata
- `src/HotChocolate/Core/src/Types/Text/Json/ResultDocument.cs` - Result document (per-operation and per-defer-group)
