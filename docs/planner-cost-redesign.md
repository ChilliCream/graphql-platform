# Fusion Query Planner: Cost Estimation Redesign

## The Core Problem

The current heuristic underestimates remaining cost by approximately **7-10x**, causing the
search to degenerate into near-breadth-first exploration for large operations.

### Quantifying the Gap

Consider a `PlanNode` with 2 completed steps and 8 work items remaining in the backlog:

```
Current estimate:
  PathCost    = 2 operations × 10.0  = 20.0
  BacklogCost = 8 items × 1.0        =  8.0
  TotalCost                           = 28.0

Actual remaining cost (typical):
  6 of 8 items need new operations    = 60.0
  2 inline successfully               =  0.0
  Actual remaining                    ≈ 60.0

True total                            ≈ 80.0 vs estimated 28.0
```

The heuristic says 8.0 but reality is ~60.0. This means the priority queue cannot
distinguish between a node that's nearly done and one that has an expensive tail. The search
explores all of them equally, blowing up the search space exponentially.

**For an operation with 50+ unresolvable fields across 5 schemas, the queue can grow into
thousands of nodes that all look equally promising.**

---

## Part 1: Redesigned Cost Function

### The Formula

```
f(n) = g(n) + w × h(n)
```

Where:
- `g(n)` = actual cost of decisions made so far (PathCost)
- `h(n)` = estimated cost of remaining work (BacklogCost) — **this is what changes**
- `w` = weight factor ≥ 1.0 (controls greediness vs optimality)

With `w = 1.0` this is A\* (optimal). With `w > 1.0` this is Weighted A\* — finds a plan
within `w×` optimal cost but explores dramatically fewer nodes. For planning, `w = 1.5`
means "a plan with at most 50% more operations than optimal" — in practice this almost
always finds the optimal plan anyway, because the tighter heuristic already steers correctly.

### h(n): Operation-Aware Backlog Cost

The key insight: **each work item type has a known minimum number of operations it will
produce.** The current cost of 1.0 per item ignores this. A Lookup work item *will* produce
at least one new `OperationPlanStep` (cost 10.0). Assigning it cost 1.0 is a 10x undercount.

```
h(n) = Σ  minOperationCost(workItem)  for each item in backlog
```

#### Per-Item Minimum Operation Cost

| Work Item Type | Current Cost | Proposed Minimum Cost | Rationale |
|---|---|---|---|
| `OperationWorkItem` (Root) | 1.0 | **10.0 + spillover(S, schema)** | Will create ≥1 operation; spillover estimates cascading lookups |
| `OperationWorkItem` (Lookup) | 1.0 | **10.0 + spillover(S, schema)** | Same — guaranteed new operation plus cascade estimate |
| `FieldRequirementWorkItem` (no lookup) | 1.0 | **inlineEstimate(field, steps)** | 0.0 if likely inlinable, 10.0 if not |
| `FieldRequirementWorkItem` (with lookup) | 2.0 | **12.0** | 10.0 for the operation + 2.0 for requirement complexity |
| `NodeFieldWorkItem` | 1.0 | **10.0 + branchCount × 10.0** | Fallback query + 1 operation per type branch |
| `NodeLookupWorkItem` | 1.0 | **10.0** | Will create exactly 1 operation |

This alone changes the heuristic for our example from 8.0 to ~68.0, which is very close to
the actual remaining cost. The search immediately becomes vastly more focused.

### spillover(): The Key Innovation

When a work item carries a `SelectionSet` targeting a specific schema, we can look INTO that
selection set and predict how many fields will spill over to other schemas — each group of
spillover fields to a distinct schema will need at least one additional operation.

```csharp
static double EstimateSpillover(
    SelectionSet selectionSet,
    string targetSchema,
    FusionSchemaDefinition compositeSchema)
{
    // Fast path: if we don't know the target schema yet, use a conservative estimate
    if (targetSchema is null)
        return 0.0;

    var spilloverSchemas = new HashSet<string>();
    var complexType = selectionSet.Type as FusionComplexTypeDefinition;
    if (complexType is null) return 0.0;

    foreach (var selection in selectionSet.Selections)
    {
        if (selection is not FieldNode fieldNode) continue;
        if (fieldNode.Name.Value == "__typename") continue;

        var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

        if (!field.Sources.ContainsSchema(targetSchema))
        {
            // This field will spill over. Which schema(s) can handle it?
            // Track the distinct schemas needed.
            foreach (var schemaName in field.Sources.Schemas)
            {
                spilloverSchemas.Add(schemaName);
                break; // just need one — we're estimating the minimum
            }
        }
        else if (field.Sources.TryGetMember(targetSchema, out var source) && source.Requirements is not null)
        {
            // Field is resolvable but has requirements — might need a lookup for the requirement
            spilloverSchemas.Add("__requirement__"); // counts as potential extra op
        }
    }

    // Each distinct spillover schema = at least 1 additional operation
    return spilloverSchemas.Count * 10.0;
}
```

**Why this works:** The schema topology is fixed. For a selection set with fields
`[name, email, orders, reviews]` targeting SchemaA where `orders` is in SchemaB and
`reviews` is in SchemaC:
- Current estimate: 1.0 (one work item)
- New estimate: 10.0 (this op) + 2 × 10.0 (SchemaB + SchemaC lookups) = 30.0
- Actual cost: 30.0 (this op + lookup to B + lookup to C)

The estimate matches reality perfectly for this common case.

### inlineEstimate(): Predicting Requirement Resolution

For `FieldRequirementWorkItem` without a lookup, the planner attempts to inline the
requirement into an existing step. We can predict whether this will succeed:

```csharp
static double EstimateInlineCost(
    FieldRequirementWorkItem workItem,
    ImmutableList<PlanStep> currentSteps)
{
    var selectionSetId = workItem.Selection.SelectionSetId;

    // Check if any existing step covers this selection set AND is on a schema
    // that can resolve the requirement fields
    for (var i = 0; i < currentSteps.Count; i++)
    {
        if (currentSteps[i] is OperationPlanStep step
            && step.SelectionSets.Contains(selectionSetId)
            && step.Id != workItem.StepId)  // can't inline into the requiring step
        {
            // Found a candidate — inline is likely to succeed
            return 1.0;  // small cost for the inlining work, but no new operation
        }
    }

    // No candidate step found — will likely need a new lookup operation
    return 10.0;
}
```

This turns the FieldRequirement cost from a flat 1.0 into a **situation-aware estimate**:
plans that have already built the right steps get a low estimate (encouraging the search
to continue down that path), while plans missing prerequisite steps get a high estimate
(deprioritizing them).

---

## Part 2: Redesigned Schema Selection Cost (ResolutionCost)

### The Problem with the Current Formula

```csharp
// Current: OperationPlanner.cs:1928
yield return (schemaName, 1.0 / resolvableSelections * 2);
```

| Resolvable Fields | Cost | Delta from Previous |
|---|---|---|
| 1 | 2.000 | — |
| 2 | 1.000 | 1.000 |
| 5 | 0.400 | 0.600 |
| 10 | 0.200 | 0.200 |
| 20 | 0.100 | 0.100 |
| 50 | 0.040 | 0.060 |

The curve flattens out quickly. For ultra-large operations, every schema has a high field count
and the resolution cost provides almost zero discrimination. Schemas with 15 resolvable fields
and schemas with 40 resolvable fields look nearly identical (0.133 vs 0.050).

### Proposed: Spillover-Aware Resolution Cost

Instead of measuring only what a schema CAN resolve, measure **what it will LEAVE behind**:

```csharp
static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
    FusionSchemaDefinition compositeSchema,
    SelectionSet selectionSet)
{
    var totalFields = CountNonTrivialFields(selectionSet);
    var schemaAnalysis = new Dictionary<string, SchemaFit>();

    AnalyzeSchemaFit(compositeSchema, schemaAnalysis, selectionSet.Type, selectionSet.Selections, totalFields);

    foreach (var (schemaName, fit) in schemaAnalysis)
    {
        yield return (schemaName, fit.ComputeCost());
    }
}

struct SchemaFit
{
    public int Resolvable;           // fields this schema can handle
    public int Unresolvable;         // fields that will spill to other schemas
    public int WithRequirements;     // fields resolvable but needing requirements
    public int TotalFields;
    public int SpilloverSchemaCount; // distinct other schemas needed for unresolvable
    public bool HasChildDepth;       // whether resolvable fields have nested selections

    public double ComputeCost()
    {
        var coverageRatio = (double)Resolvable / TotalFields;

        // Base cost: inversely proportional to coverage, but with steeper curve
        // Using (1 - ratio)² makes the cost increase sharply as coverage drops
        var baseCost = (1.0 - coverageRatio) * (1.0 - coverageRatio) * 20.0;

        // Spillover penalty: each distinct schema we'll need to contact later
        // This is the most discriminating signal for ultra-large operations
        var spilloverPenalty = SpilloverSchemaCount * 5.0;

        // Requirement penalty: fields with requirements may need additional lookups
        var requirementPenalty = WithRequirements * 2.0;

        return baseCost + spilloverPenalty + requirementPenalty;
    }
}
```

**Comparison on an ultra-large operation (60 fields, 4 schemas):**

| Schema | Resolvable | Unresolvable | Spillover Schemas | Old Cost | New Cost |
|---|---|---|---|---|---|
| A | 40 | 20 | 3 | 0.050 | 1.39 + 15.0 + 0.0 = 16.4 |
| B | 25 | 35 | 3 | 0.080 | 8.68 + 15.0 + 0.0 = 23.7 |
| C | 15 | 45 | 3 | 0.133 | 14.06 + 15.0 + 2.0 = 31.1 |
| D | 10 | 50 | 3 | 0.200 | 17.36 + 15.0 + 4.0 = 36.4 |

Old cost range: 0.050 to 0.200 (delta = **0.15** — nearly invisible to the priority queue)
New cost range: 16.4 to 36.4 (delta = **20.0** — massive discrimination)

Schema A is clearly preferred: it covers 67% of fields and leaves only 33% for lookups. The
old formula couldn't express this difference meaningfully.

---

## Part 3: Pre-Computed Schema Topology Structures

These structures are computed once at schema composition time (`FusionSchemaDefinition.Seal()`)
and amortized across all planning sessions.

### 3.1 Field-Level Resolution Map

For each composite type, pre-compute which schema can resolve each field and at what cost:

```csharp
class FieldResolutionMap
{
    // For a given type + field, which schemas can resolve it?
    // Key: (typeName, fieldName)
    // Value: sorted array of (schemaName, hasDirectLookup, hasRequirements)
    private readonly Dictionary<(string, string), FieldResolutionInfo[]> _map;

    // For a given type + schema, how many fields are directly resolvable?
    // Key: (typeName, schemaName)
    // Value: count of directly resolvable fields (no requirements)
    private readonly Dictionary<(string, string), int> _directFieldCount;
}
```

This avoids the repeated `field.Sources.ContainsSchema()` / `field.Sources.TryGetMember()`
calls during planning. The partitioner and cost estimator can do O(1) lookups instead.

### 3.2 Schema Transition Matrix

For each type, pre-compute the cheapest transition between every pair of schemas:

```csharp
class SchemaTransitionMatrix
{
    // Key: (typeName, fromSchema, toSchema)
    // Value: (hasDirectLookup, bestLookupArgCount, requiresIntermediary)
    private readonly Dictionary<(string, string, string), TransitionInfo> _transitions;

    // For a given type, which schema pairs have NO transition path?
    // These are dead ends — don't explore them.
    private readonly HashSet<(string, string, string)> _deadTransitions;
}
```

**Dead transition pruning** is extremely valuable: if there's no way to get from SchemaA to
SchemaB for type T, the planner currently discovers this by exhausting all lookup options
(generating many nodes that go nowhere). With the matrix, it can skip the branch entirely.

### 3.3 Type Complexity Score

For each composite type, pre-compute how "scattered" its fields are across schemas:

```csharp
class TypeComplexityScore
{
    // Key: typeName
    // Value: complexity metrics
    private readonly Dictionary<string, TypeComplexity> _scores;
}

struct TypeComplexity
{
    public int TotalFields;
    public int SchemaCount;          // how many schemas have fields of this type
    public int MaxCoverage;          // most fields any single schema can resolve
    public float ScatterRatio;       // 1.0 = every field in different schema, 0.0 = all in one
    public int RequirementChainDepth; // max depth of requirement→field→requirement chains
}
```

The `ScatterRatio` feeds directly into the heuristic: types with high scatter will
generate more operations, so work items involving those types should have higher estimated cost.

```csharp
// In the heuristic:
double EstimateWorkItemCost(OperationWorkItem item)
{
    var typeComplexity = _typeComplexity[item.SelectionSet.Type.Name];
    var baseCost = 10.0; // guaranteed operation

    // Scattered types will generate more lookups
    var scatterPenalty = typeComplexity.ScatterRatio * typeComplexity.SchemaCount * 10.0;

    // Deep requirement chains = sequential operations that can't be parallelized
    var chainPenalty = typeComplexity.RequirementChainDepth * 5.0;

    return baseCost + scatterPenalty + chainPenalty;
}
```

---

## Part 4: Cross-Session Learning (Planning Cost Oracle)

The schema topology is fixed between composition changes, but the planner re-discovers
the cost landscape from scratch for every operation. By recording observed costs, the
planner can build an increasingly accurate heuristic over time.

The oracle is split into two tiers with very different memory profiles:

- **Tier 1 — Heuristic cache:** Stores only learned `double` cost values. Negligible memory.
  Always on.
- **Tier 2 — Sub-plan cache:** Stores the top-N most frequently used complete sub-plans.
  Bounded memory. Opt-in.

### 4.1 Memory Budget Analysis

#### Tier 1: Heuristic Cache (Always On)

All Tier 1 structures store only scalar cost values — no ASTs, no syntax trees,
no plan steps. For a large schema with S=15 schemas, T=300 types:

```
_resolutionCosts:   (typeName, schemaName)                → double
                    Max entries: T × S = 4,500
                    Per entry: ~100 bytes (key tuple + string refs + double + dict overhead)
                    Total: 4,500 × 100 = ~450 KB

_transitionCosts:   (typeName, fromSchema, toSchema)      → double
                    Max entries: T × S × S = 67,500
                    Realistic (only observed transitions): ~5,000
                    Per entry: ~120 bytes
                    Total: 5,000 × 120 = ~600 KB

_deadEndTracker:    (typeName, schemaName, patternHash)   → int (failure count)
                    Realistic: ~2,000 entries
                    Per entry: ~80 bytes
                    Total: 2,000 × 80 = ~160 KB

_spilloverEstimates: (typeName, schemaName, fieldSetHash) → double
                     Realistic: ~3,000 entries
                     Per entry: ~90 bytes
                     Total: 3,000 × 90 = ~270 KB

─────────────────────────────────────────────────────
Tier 1 total:  ~1.5 MB for a 300-type, 15-schema gateway
```

This is the same order of magnitude as the existing `_possibleLookups` and
`_bestDirectLookup` caches already on `FusionSchemaDefinition` (line 23-24).
It scales linearly with schema size and is bounded by the schema topology.

#### Tier 2: Sub-Plan Cache (Bounded, Opt-In)

This tier stores actual plan fragments for the most frequently used selection patterns.
The key design decision: **cap at N entries with frequency gating**.

```
Entry size:
  Key:   SelectionPatternKey = ~40 bytes (typeName ref + 2 × ulong hash)
  Value: PlanFragment with full AST steps ≈ 5-20 KB depending on step count

With N = 200 (top 200 most used patterns):
  200 × 15 KB average = ~3 MB

With N = 500:
  500 × 15 KB average = ~7.5 MB

With N = 1000:
  1000 × 15 KB average = ~15 MB
```

The frequency table itself is a separate lightweight structure that tracks ALL
patterns but stores only a counter — it decides which N patterns earn a cached plan:

```
_frequencyTable:  (SelectionPatternKey → uint hitCount)
                  Even 50,000 distinct patterns = 50,000 × 50 bytes = ~2.5 MB
```

#### Total Memory: Worst Case

```
Tier 1 (heuristics only):            ~1.5 MB   ← always on, ~free
Tier 2 frequency table:              ~2.5 MB   ← always on, tracks hits
Tier 2 cached sub-plans (N=200):     ~3.0 MB   ← bounded by N
Pre-computed topology (Part 3):       ~2.5 MB   ← computed once at seal
──────────────────────────────────────────────
Total:                                ~9.5 MB for a large schema
```

For comparison, the `FusionSchemaDefinition` itself with all its type metadata,
lookup definitions, and field sources for a 300-type schema is easily 5-20 MB already.
The oracle roughly doubles that — a reasonable tradeoff for the planning speedup.

### 4.2 Tier 1: Heuristic Cache Implementation

The heuristic cache stores only learned `double` values. No ASTs, no plan steps.

```csharp
class PlanningCostOracle
{
    // Learned cost values — just doubles, no ASTs
    readonly ConcurrentDictionary<(string Type, string Schema), CostEntry> _resolutionCosts = new();
    readonly ConcurrentDictionary<TransitionKey, CostEntry> _transitionCosts = new();
    readonly ConcurrentDictionary<(string Type, string Schema, ulong PatternHash), int> _deadEnds = new();
    readonly ConcurrentDictionary<(string Type, string Schema, ulong FieldHash), double> _spilloverEstimates = new();

    // Per-pattern hit counter for Tier 2 promotion (lightweight — just a counter)
    readonly ConcurrentDictionary<SelectionPatternKey, uint> _frequencyTable = new();

    // Tier 2: bounded sub-plan cache
    readonly BoundedSubPlanCache _subPlanCache;
}

/// <summary>
/// 16 bytes per entry. That's it. No AST references.
/// </summary>
struct CostEntry
{
    public double TotalCost;     // running sum
    public int ObservationCount; // how many plans contributed

    public double Average => TotalCost / ObservationCount;

    public CostEntry Record(double observedCost)
        => new() { TotalCost = TotalCost + observedCost, ObservationCount = ObservationCount + 1 };
}
```

### 4.3 Recording Outcomes (Tier 1)

After `Plan()` returns, extract cost signals — this is a lightweight post-processing
pass over the completed plan steps, no allocations beyond dictionary updates:

```csharp
void RecordPlanOutcome(PlanNode finalNode)
{
    // 1. Resolution costs: how many operations did each (type, schema) require?
    foreach (var step in finalNode.Steps.OfType<OperationPlanStep>())
    {
        var key = (step.Type.Name, step.SchemaName ?? "");
        _resolutionCosts.AddOrUpdate(key,
            new CostEntry { TotalCost = 1, ObservationCount = 1 },
            (_, entry) => entry.Record(1));
    }

    // 2. Transition costs: for each lookup, what was the total downstream cost?
    foreach (var step in finalNode.Steps.OfType<OperationPlanStep>())
    {
        if (step.Lookup is not { } lookup) continue;

        var downstreamOps = CountDownstreamOps(step, finalNode.Steps);
        var key = new TransitionKey(step.Type.Name, lookup.SchemaName, step.SchemaName!);
        _transitionCosts.AddOrUpdate(key,
            new CostEntry { TotalCost = downstreamOps, ObservationCount = 1 },
            (_, entry) => entry.Record(downstreamOps));
    }

    // 3. Update frequency table for Tier 2 promotion decisions
    RecordSelectionPatterns(finalNode);
}
```

### 4.4 Using Learned Costs in the Heuristic (Tier 1)

The heuristic lookup is O(1) — a single dictionary read returning a `double`:

```csharp
double EstimateWorkItemCost(WorkItem item, PlanNode current, PlanningCostOracle oracle)
{
    switch (item)
    {
        case OperationWorkItem { SelectionSet: var ss }:
        {
            // Check learned cost: just a double lookup, no allocations
            if (oracle.TryGetResolutionCost(ss.Type.Name, current.SchemaName, out var learned))
                return learned * 10.0;

            // Check learned spillover for this field combination
            if (oracle.TryGetSpilloverEstimate(ss, current.SchemaName, out var spillover))
                return 10.0 + spillover;

            // Cold start: fall back to static estimate
            return 10.0 + EstimateSpillover(ss, current.SchemaName);
        }

        case FieldRequirementWorkItem frw:
        {
            if (frw.Lookup is not null)
                return 12.0;

            if (oracle.TryGetTransitionCost(frw, current, out var cost))
                return cost;

            return EstimateInlineCost(frw, current.Steps);
        }

        // ...
    }
}
```

The entire Tier 1 path adds zero allocations to the planning hot loop. It's just
dictionary lookups returning scalar values.

### 4.5 Tier 2: Frequency-Gated Sub-Plan Cache

Only the **top N most frequently seen** selection patterns get their full sub-plans cached.
This is where the AST storage lives, but it's bounded.

```csharp
class BoundedSubPlanCache
{
    readonly int _maxEntries;                     // e.g., 200
    readonly int _promotionThreshold;             // e.g., 3 (seen 3+ times before caching)
    readonly ConcurrentDictionary<SelectionPatternKey, PlanFragment> _cache;

    // Only called after a pattern has been seen _promotionThreshold times
    public void TryPromote(SelectionPatternKey key, PlanFragment fragment)
    {
        if (_cache.Count >= _maxEntries)
        {
            // Evict least-recently-used entry
            EvictLRU();
        }

        _cache.TryAdd(key, fragment);
    }

    public bool TryGet(SelectionPatternKey key, out PlanFragment fragment)
        => _cache.TryGetValue(key, out fragment);
}
```

**Promotion flow:**

```
1st time seeing pattern P:  _frequencyTable[P] = 1           → static heuristic
2nd time seeing pattern P:  _frequencyTable[P] = 2           → static heuristic (improving via Tier 1)
3rd time seeing pattern P:  _frequencyTable[P] = 3           → PROMOTE: cache full sub-plan
4th+ time seeing pattern P: _subPlanCache.TryGet(P) → HIT   → skip search entirely
```

This means:
- One-off queries (exploratory, debugging, introspection): never cached, zero overhead
- Occasional queries (seen 1-2 times): benefit from Tier 1 heuristic learning only
- Hot queries (seen 3+ times): get the full sub-plan cache → near-zero planning time

### 4.6 What the Sub-Plan Cache Actually Stores

For the top-N patterns, we cache the **plan recipe** — the schema assignments and lookup
choices that the search discovered. There are two options:

**Option A: Full plan fragment** (~5-20 KB per entry)

Stores the actual `ImmutableList<PlanStep>` with ASTs. Largest memory footprint but
allows instant replay — no re-planning needed at all.

**Option B: Plan recipe** (~200-500 bytes per entry)

Stores only the decisions, not the built ASTs:

```csharp
record PlanRecipe(
    // For each field group in this pattern: which schema resolves it?
    ImmutableArray<(ulong FieldGroupHash, string SchemaName)> SchemaAssignments,
    // For each lookup needed: which lookup definition to use?
    ImmutableArray<(string TypeName, string FromSchema, string ToSchema, string LookupField)> LookupChoices,
    // The total cost of this plan (for heuristic use)
    double TotalCost);
```

With Option B, the planner doesn't skip the search entirely — but it replays the
decisions deterministically, following a single path with no branching. This is still
orders of magnitude faster than searching, because the branching factor drops to 1.

**Memory with Option B:**

```
N = 200 patterns × 400 bytes = ~80 KB
N = 1000 patterns × 400 bytes = ~400 KB
```

Effectively free. You could cache 10,000 recipes and still be under 4 MB.

### 4.7 Dead-End Avoidance

Part of Tier 1 — stores only `int` counters per (type, schema, pattern):

```csharp
// During planning, when a branch produces no valid successors:
oracle.RecordDeadEnd(current.SchemaName, workItem.SelectionSet.Type.Name, patternHash);

// When branching, check before enqueueing:
if (oracle.IsLikelyDeadEnd(schemaName, type.Name, patternHash))
{
    continue; // skip this branch entirely
}
```

After observing the same dead end 3+ times, the planner stops exploring it.
Memory: ~80 bytes per tracked dead end. Even 10,000 dead ends = 800 KB.

### 4.8 Cache Invalidation

Everything is keyed against the schema topology. On schema change, clear all:

```csharp
// In FusionSchemaDefinition.Seal() or on schema update:
_planningOracle.Clear();
```

The oracle rebuilds naturally as new operations are planned. The warm-up cost is
minimal — Tier 1 converges after ~20-50 operations, Tier 2 after ~100-200 operations
(depending on the promotion threshold and query diversity).

### 4.9 Memory Summary by Configuration

| Configuration | Tier 1 (heuristics) | Frequency table | Tier 2 (sub-plans) | Total |
|---|---|---|---|---|
| Heuristics only (default) | ~1.5 MB | ~2.5 MB | 0 | **~4 MB** |
| + Recipe cache (N=200) | ~1.5 MB | ~2.5 MB | ~80 KB | **~4.1 MB** |
| + Recipe cache (N=1000) | ~1.5 MB | ~2.5 MB | ~400 KB | **~4.4 MB** |
| + Full fragment cache (N=200) | ~1.5 MB | ~2.5 MB | ~3 MB | **~7 MB** |
| + Full fragment cache (N=500) | ~1.5 MB | ~2.5 MB | ~7.5 MB | **~11.5 MB** |

For most deployments, the recipe cache (Option B) at N=1000 gives 95% of the benefit
at ~4.4 MB total. The full fragment cache is only worth it if planning CPU is a
measured bottleneck and the extra memory is acceptable.

---

## Part 5: Parallelism-Aware Cost Model (Waterfall Penalty)

### 5.1 The Problem

The current cost model is blind to execution topology. It treats these two plans identically:

```
Plan A: 4 sequential operations (waterfall)

  Step 1 → Step 2 → Step 3 → Step 4
  Depth: 4 rounds     Total ops: 4     Current cost: 40.0

Plan B: 1 root + 3 parallel lookups (fan-out)

  Step 1 → Step 2
         → Step 3
         → Step 4
  Depth: 2 rounds     Total ops: 4     Current cost: 40.0
```

Plan B executes in half the wall-clock time, but the planner scores them equally.

### 5.2 The Tradeoff

Parallelism is not always better. Each parallel operation has overhead (HTTP connection,
serialization, downstream load). The sweet spot depends on the fan-out width:

| Plan Shape | Depth | Total Ops | Latency | Overhead | Verdict |
|---|---|---|---|---|---|
| 4 sequential | 4 | 4 | High | Low | Bad — waterfall |
| 1 + 6 parallel | 2 | 7 | Low | Moderate | **Good — sweet spot** |
| 1 + 8 parallel | 2 | 9 | Low | Moderate-High | Acceptable |
| 1 + 20 parallel | 2 | 21 | Low | Very High | **Bad — excessive fan-out** |

The cost model needs to capture: **depth is the primary latency driver, total ops is
secondary, and excessive fan-out at any single level is penalized.**

### 5.3 The Formula

```
PathCost = depth × W_DEPTH + totalOps × W_OP + excessFanout × W_EXCESS
```

Where:

| Weight | Value | Meaning |
|---|---|---|
| `W_DEPTH` | 15.0 | Cost per sequential round (network hop latency) |
| `W_OP` | 1.5 | Marginal cost per operation (HTTP overhead) |
| `W_EXCESS` | 3.0 | Penalty per operation beyond fan-out threshold |
| `FanoutThreshold` | 8 | Max parallel ops at one level before penalty |

```
excessFanout = Σ  max(0, opsAtLevel[d] - FanoutThreshold)  for each depth level d
```

### 5.4 Validation Against Examples

**4 sequential** (depth=4, ops=4, levels=[1,1,1,1]):
```
  4 × 15 + 4 × 1.5 + 0 = 66.0
```

**1 → 6 parallel** (depth=2, ops=7, levels=[1,6]):
```
  2 × 15 + 7 × 1.5 + 0 = 40.5  ← 38% cheaper than sequential
```

**1 → 8 parallel** (depth=2, ops=9, levels=[1,8]):
```
  2 × 15 + 9 × 1.5 + 0 = 43.5  ← still good, at threshold
```

**1 → 12 parallel** (depth=2, ops=13, levels=[1,12]):
```
  2 × 15 + 13 × 1.5 + 4 × 3.0 = 61.5  ← approaching sequential cost
```

**1 → 20 parallel** (depth=2, ops=21, levels=[1,20]):
```
  2 × 15 + 21 × 1.5 + 12 × 3.0 = 97.5  ← worst option
```

**1 → 3 → 3 (two-level tree)** (depth=3, ops=7, levels=[1,3,3]):
```
  3 × 15 + 7 × 1.5 + 0 = 55.5  ← between fan-out and sequential
```

The model correctly ranks: **1→6 (40.5) < 1→8 (43.5) < 1→3→3 (55.5) < 1→12 (61.5) < 4-seq (66.0) < 1→20 (97.5)**

### 5.5 The Breakeven Point

When is adding N parallel operations better than 1 more sequential level?

```
N × W_OP < W_DEPTH + W_OP       (below threshold)
N < W_DEPTH/W_OP + 1
N < 15/1.5 + 1 = 11
```

**Up to 10 parallel operations are ALWAYS cheaper than adding 1 sequential round.**

Beyond the threshold, with the excess penalty:
```
N × W_OP + (N - threshold) × W_EXCESS < W_DEPTH + W_OP
N × 1.5 + (N - 8) × 3.0 < 16.5
4.5N - 24 < 16.5
N < 9
```

So once past the threshold, the breakeven drops sharply. This creates a natural "cliff"
that discourages excessive fan-out while strongly rewarding moderate parallelism.

### 5.6 Tracking Depth Incrementally on PlanNode

**Important design constraint:** The planner tracks `Dependents` (forward: "who depends
on me"), not dependencies (backward: "who do I depend on"). When step A has
`Dependents = {B, C}`, that means steps B and C depend on A's output
([OperationPlanStep.cs:23](src/HotChocolate/Fusion-vnext/src/Fusion.Execution/Planning/Steps/OperationPlanStep.cs#L23)).
A step does **not** record who feeds it, and during planning the dependency graph is
incomplete — new steps and relationships are still being discovered.

This means we cannot cheaply ask "what is step B's depth?" by looking at step B alone.
We would have to scan all existing steps for those whose `Dependents` contain B — which
is exactly the kind of graph traversal we want to avoid.

**Solution: propagate depth through work items.** When we push a work item to the backlog,
we already know which step (at which depth) spawned it. We stamp the work item with the
spawning step's depth. When the work item is later planned into a new step, the new step's
depth = `workItem.ParentDepth + 1`. This makes depth O(1) per step creation with no
graph traversal.

```csharp
internal sealed record PlanNode
{
    // ... existing fields ...

    // Parallelism tracking (maintained incrementally, O(1) per node creation)
    public int MaxDepth { get; init; }            // deepest level in current plan
    public int OperationStepCount { get; init; }  // total operation steps
    public int ExcessFanout { get; init; }        // cumulative excess across all levels

    // Per-level operation counts (small — typically 2-5 levels)
    // Level 1: root ops, Level 2: first lookups, Level 3: chained lookups, etc.
    public ImmutableDictionary<int, int> OpsPerLevel { get; init; }
        = ImmutableDictionary<int, int>.Empty;

    public double PathCost
        => MaxDepth * 15.0 + OperationStepCount * 1.5 + ExcessFanout * 3.0;

    // Weighted A*
    public double TotalCost => PathCost + Weight * BacklogCost + ResolutionCost;
    internal const double Weight = 1.5;
    internal const int FanoutThreshold = 8;
}
```

When creating a new step from a work item:

```csharp
// Depth comes from the work item, which was stamped when pushed to the backlog.
// Root operations sit at depth 1; lookups/requirements sit at parentDepth + 1.
int newStepDepth = workItem.EstimatedDepth;

// Update level counts
var opsAtLevel = current.OpsPerLevel.GetValueOrDefault(newStepDepth, 0) + 1;
var newOpsPerLevel = current.OpsPerLevel.SetItem(newStepDepth, opsAtLevel);

// Compute excess fan-out change
var newExcessFanout = current.ExcessFanout;
if (opsAtLevel > FanoutThreshold)
    newExcessFanout++;  // this op pushes the level over the threshold

var next = new PlanNode
{
    // ...
    MaxDepth = Math.Max(current.MaxDepth, newStepDepth),
    OperationStepCount = current.OperationStepCount + 1,
    ExcessFanout = newExcessFanout,
    OpsPerLevel = newOpsPerLevel,
};
```

And when pushing work items to the backlog (e.g. in `InlineLookupRequirements`,
`PlanFieldWithRequirement`), stamp them with the current step's depth:

```csharp
// We know the current step's depth because it was derived from its own work item.
// New backlog items carry that depth forward so their future steps know their level.
backlog = backlog.Push(
    new OperationWorkItem(
        OperationWorkItemKind.Lookup,
        workItemSelectionSet with { Node = selectionSet },
        FromSchema: lookup.SchemaName)
    {
        Dependents = ImmutableHashSet<int>.Empty.Add(lookupStepId),
        ParentDepth = currentStepDepth  // ← stamped here
    });
```

This is O(1) per step — no dictionary lookups for other steps' depths, no graph traversal.

**Limitation:** This is an approximation. A step's actual critical-path depth could be
deeper if multiple prerequisite steps at different depths feed into the same lookup. But
since `Dependents` only tracks the forward direction, the exact depth isn't cheaply
knowable during planning. For heuristic cost estimation, the parent-depth approximation is
sufficient — it captures the waterfall structure (depth 1 → 2 → 3 → …) accurately for the
common case, and worst-case underestimates by at most 1 level for multi-parent steps.

### 5.7 Estimating Remaining Depth in the Heuristic

For the backlog cost (h(n)), we need to estimate how many additional depth levels the
remaining work items will add. Since each work item carries its `ParentDepth`, we can
project where its resulting step will land:

```csharp
// Depth information propagated through the backlog
internal abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];

    // The depth level of the step that spawned this work item.
    // Stamped when the work item is pushed to the backlog.
    public int ParentDepth { get; init; }

    // Projected depth of the step this work item will produce.
    public int EstimatedDepth => this switch
    {
        OperationWorkItem { Kind: OperationWorkItemKind.Root } => 1,
        _ => ParentDepth + 1  // lookups, requirements add a level
    };

    public virtual double Cost => 1;
}
```

Then the heuristic estimates additional depth:

```csharp
double EstimateBacklogDepthCost(ImmutableStack<WorkItem> backlog, int currentMaxDepth)
{
    var maxEstimatedDepth = currentMaxDepth;
    var estimatedOpsPerLevel = new Dictionary<int, int>();

    foreach (var item in backlog)
    {
        var level = item.EstimatedDepth;
        maxEstimatedDepth = Math.Max(maxEstimatedDepth, level);
        estimatedOpsPerLevel.TryGetValue(level, out var count);
        estimatedOpsPerLevel[level] = count + 1;
    }

    var additionalDepth = maxEstimatedDepth - currentMaxDepth;
    var estimatedExcessFanout = 0;
    foreach (var (_, count) in estimatedOpsPerLevel)
    {
        estimatedExcessFanout += Math.Max(0, count - FanoutThreshold);
    }

    return additionalDepth * 15.0
         + backlog.Count * 1.5   // approximate ops (refined by Part 1 estimates)
         + estimatedExcessFanout * 3.0;
}
```

**Note:** This iterates the backlog, which we identified as O(n) in Part 1.
To avoid this, maintain the estimated depth values incrementally alongside the
backlog cost — update on push/pop rather than recomputing.

### 5.8 How This Changes Schema Selection

The parallelism model has a direct impact on which schema the planner prefers. Consider
a selection set with 10 fields where:

- **Schema A** can resolve 8 fields → 2 unresolvable → 1 lookup (depth +1, fan-out 1)
- **Schema B** can resolve 4 fields → 6 unresolvable across 3 schemas → 3 lookups (depth +1, fan-out 3)

Under the old model (ops only): A costs ~30 (3 ops), B costs ~40 (4 ops). Mild preference.

Under the parallelism model:
- **A:** depth=2, ops=3, fanout=[1,2] → 2×15 + 3×1.5 + 0 = 34.5
- **B:** depth=2, ops=4, fanout=[1,3] → 2×15 + 4×1.5 + 0 = 36.0

Similar depth (both 2 rounds), so the difference is modest. But if Schema B's lookups
themselves need further lookups:
- **B (cascading):** depth=3, ops=6, fanout=[1,3,2] → 3×15 + 6×1.5 + 0 = 54.0

Now A is clearly preferred. **The depth penalty naturally cascades — schemas that cause
deeper dependency chains are penalized more heavily with each additional level.**

### 5.9 Tuning the Weights

The weights should be configurable per deployment:

```csharp
class PlannerCostConfig
{
    // Latency-optimized (default): minimize round-trips aggressively
    public double WDepth { get; init; } = 15.0;
    public double WOp { get; init; } = 1.5;
    public double WExcess { get; init; } = 3.0;
    public int FanoutThreshold { get; init; } = 8;

    // Throughput-optimized: minimize total operations, tolerate depth
    // (for high-latency backends where parallelism has less benefit)
    public static PlannerCostConfig Throughput => new()
    {
        WDepth = 8.0, WOp = 5.0, WExcess = 2.0, FanoutThreshold = 12
    };

    // Minimal fan-out: conservative, fewer total requests
    // (for scenarios with strict rate limits or connection pool constraints)
    public static PlannerCostConfig Conservative => new()
    {
        WDepth = 10.0, WOp = 8.0, WExcess = 5.0, FanoutThreshold = 4
    };
}
```

---

## Part 6: Putting It All Together

### Updated PlanNode

```csharp
internal sealed record PlanNode
{
    // ... existing fields ...

    // Parallelism-aware cost tracking (all maintained incrementally, O(1))
    public int MaxDepth { get; init; }
    public int OperationStepCount { get; init; }
    public int ExcessFanout { get; init; }
    public ImmutableDictionary<int, int> OpsPerLevel { get; init; }
    public ImmutableDictionary<int, int> StepDepths { get; init; }
    public double BacklogCost { get; init; }

    public double PathCost
        => MaxDepth * Config.WDepth
         + OperationStepCount * Config.WOp
         + ExcessFanout * Config.WExcess;

    public double TotalCost => PathCost + Weight * BacklogCost + ResolutionCost;
    internal const double Weight = 1.5;
}
```

### Updated Work Item Costs

```csharp
internal abstract record WorkItem
{
    public ImmutableHashSet<int> Dependents { get; init; } = [];

    /// <summary>
    /// Estimated minimum cost to fully resolve this work item,
    /// including cascading operations.
    /// </summary>
    public abstract double EstimateCost(PlanNode current, FusionSchemaDefinition schema);
}

internal sealed record OperationWorkItem(...) : WorkItem
{
    public override double EstimateCost(PlanNode current, FusionSchemaDefinition schema)
    {
        // This will create at least 1 new operation
        var baseCost = 10.0;

        // Estimate spillover: how many additional schemas will be needed?
        var spillover = CostEstimator.EstimateSpillover(SelectionSet, current.SchemaName, schema);

        return baseCost + spillover;
    }
}

internal sealed record FieldRequirementWorkItem(...) : WorkItem
{
    public override double EstimateCost(PlanNode current, FusionSchemaDefinition schema)
    {
        if (Lookup is not null)
            return 12.0;  // guaranteed operation + requirement overhead

        // Check if inline is likely to succeed
        return CostEstimator.EstimateInlineProbability(this, current.Steps) < 0.5
            ? 10.0   // likely needs new operation
            : 1.0;   // likely inlines
    }
}
```

### Updated Backlog Cost Computation (Incremental)

Instead of iterating the full stack on every `PlanNode` creation:

```csharp
// When popping a work item from the backlog:
var poppedCost = workItem.EstimateCost(current, schema);
var newBacklogCost = current.BacklogCost - poppedCost;

// When pushing new work items:
foreach (var newItem in newWorkItems)
    newBacklogCost += newItem.EstimateCost(nextNode, schema);

var next = new PlanNode
{
    // ...
    BacklogCost = newBacklogCost,  // O(1), no iteration
    OperationStepCount = current.OperationStepCount + 1,
};
```

### Updated GetPossibleSchemas

```csharp
static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
    FusionSchemaDefinition compositeSchema,
    SelectionSet selectionSet,
    PlanningCostOracle? oracle = null)
{
    var analysis = AnalyzeSchemaFit(compositeSchema, selectionSet);

    foreach (var (schemaName, fit) in analysis)
    {
        double cost;

        // Prefer learned cost if available
        if (oracle?.TryGetResolutionCost(selectionSet.Type.Name, schemaName, out var learned) == true)
        {
            cost = learned;
        }
        else
        {
            var coverageRatio = (double)fit.Resolvable / fit.TotalFields;
            cost = (1.0 - coverageRatio) * (1.0 - coverageRatio) * 20.0  // quadratic falloff
                 + fit.SpilloverSchemaCount * 5.0                         // future operations
                 + fit.WithRequirements * 2.0;                            // requirement chains
        }

        yield return (schemaName, cost);
    }
}
```

---

## Expected Impact

### Search Space Reduction

| Scenario | Current Nodes Explored | With Redesign | Reduction |
|---|---|---|---|
| Simple (3 schemas, 10 fields, 2 lookups) | ~20 | ~8 | 2.5x |
| Medium (5 schemas, 30 fields, 8 lookups) | ~500 | ~40 | 12x |
| Large (5 schemas, 80 fields, 20 lookups) | ~10,000+ | ~200 | 50x+ |
| Ultra-large (8 schemas, 200 fields, 50 lookups) | unbounded | ~1,000 | orders of magnitude |
| Repeated (any, after cache warm-up) | same as above | ~10 | near-instant |

### Why These Estimates

1. **Tighter heuristic (10x improvement):** The priority queue immediately focuses on the
   most promising branch instead of exploring all branches equally. This is the single
   biggest impact — it's the difference between Dijkstra's and A\*.

2. **Spillover-aware resolution cost:** Schema selection becomes highly discriminating.
   Instead of exploring 5 schemas equally (branching factor 5), the cost function
   clearly identifies the 1-2 best schemas (effective branching factor ~2).

3. **Dead transition pruning:** Pre-computed reachability eliminates branches that can
   never succeed. For schemas with complex topologies, this can eliminate 30-50% of
   branches at each level.

4. **Sub-plan caching (cross-session):** After warm-up, common subtrees are resolved in
   O(1). For production workloads where 80% of queries share common patterns, this
   makes planning effectively free for the shared portions.

5. **Weighted A\* (w=1.5):** Additional 2-3x reduction in explored nodes with negligible
   impact on plan quality. The weight makes the search "commit" to promising paths
   earlier instead of hedging across many alternatives.
