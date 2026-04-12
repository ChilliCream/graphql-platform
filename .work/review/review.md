# Review Verdict: **Request Changes**

The current changes introduce valuable semantic introspection functionality, but there are shipping blockers and a few important consistency risks.

## Critical

### 1. Sync-over-async in Fusion introspection resolvers
- **Files:** `src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Introspection/Query.cs`
- **Evidence:** Calls to `SearchAsync(...).AsTask().GetAwaiter().GetResult()` and `GetPathsToRootAsync(...).AsTask().GetAwaiter().GetResult()`
- **Why it matters:** Blocking async calls inside request execution risks thread-pool starvation/deadlock patterns and increases tail latency under load.
- **Fix:** Move resolver path to async end-to-end (preferred), or provide a truly synchronous provider path. Avoid `GetResult()` in execution flow.

## Major

### 2. Semantic introspection enabled by default changes schema surface
- **File:** `src/HotChocolate/Core/src/Types/SchemaOptions.cs`
- **Evidence:** `EnableSemanticIntrospection` defaults to `true`.
- **Why it matters:** Existing schemas gain new introspection fields/types by default, which can break strict schema-shape checks and downstream tooling expectations.
- **Fix:** Consider defaulting to opt-in (`false`) or gate with explicit versioned rollout/clear upgrade note.

### 3. Inconsistent `ISchemaSearchProvider` registration between Core and Fusion
- **Files:** `src/HotChocolate/Core/src/Types/SchemaBuilder.Setup.cs`, `src/HotChocolate/Fusion/src/Fusion.Execution/Execution/FusionRequestExecutorManager.cs`
- **Evidence:** Core registers provider unconditionally; Fusion registers only when semantic introspection is enabled.
- **Why it matters:** Divergent lifecycle/behavior across stacks makes feature behavior and diagnostics inconsistent.
- **Fix:** Align registration strategy (prefer conditional registration in both paths).

## Minor

### 4. Missing cancellation propagation in Fusion search calls
- **File:** `src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Introspection/Query.cs`
- **Evidence:** `SearchAsync`/`GetPathsToRootAsync` are invoked without a request cancellation token.
- **Why it matters:** Cancelled requests may continue expensive introspection work unnecessarily.
- **Fix:** Thread request cancellation through field context and pass token to provider APIs.

### 5. Duplicate coordinate-resolution logic in Core and Fusion
- **Files:** `src/HotChocolate/Core/src/Types/Types/Introspection/IntrospectionFields.cs`, `src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Introspection/SchemaCoordinateResolver.cs`
- **Why it matters:** Logic drift risk and higher maintenance overhead.
- **Fix:** Extract a shared resolver helper/API and reuse in both implementations.

### 6. First-query latency risk from lazy BM25 index build
- **File:** `src/HotChocolate/Core/src/Types/Types/Introspection/Search/BM25SearchProvider.cs`
- **Why it matters:** Large schemas can pay indexing cost on first `__search` call.
- **Fix:** Add optional prewarm/eager build path or document/startup-warm strategy.

## Notes

- Pattern and validation wiring look largely correct (new fields marked as introspection and recognized by validation logic).
- Snapshot changes are consistent with the new schema surface.
