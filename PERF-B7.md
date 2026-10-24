# B7: plan-derived merge program (B4-lite)

Branch `perf/B7-merge-program` from `origin/main` @ d4eaccc1e5. Idea B7 of the Fusion gateway big-ticket campaign: M1 (merge program over a sequential source-row reader), M2 (object lists created as one block), M3 (unchecked writes into slots created by the same merge), P1 (envelope read in document order). Every number below is marked measured (M) or inferred (I).

## Commits

| commit | change | measured separately as |
|---|---|---|
| harness | `FirstMergeBenchmark`: parse-only, merge-only and parse-and-merge of the captured benchmark responses for both families, resources under `benchmarks/Fusion.Execution.Benchmarks/__resources__/merge-program/` | base |
| M1 | `MergeProgramTemplate` (source response order from the step's subgraph selection set, attached to `ResultSelectionSet` by the planner and by `JsonOperationPlanParser`), `MergeProgram` (resolved per composite selection set, cached on the `ResultSelectionSet`), `SourceResultDocument.MetaDb.SequentialReader`, program-driven completion in `ValueCompletion.Program.cs` with per-property fallback to `TryCompleteValue` | M1 |
| M2 | `CompositeResultDocument.CreateObjectArray`: the array, its element objects and the element references in one call; `CompleteObjectList` addresses element objects by `firstObject + i * blockRows` | M1+M2 |
| M3 | `RegisterSource` once per record, unchecked leaf, reference and null writes into slots created by the same merge, `CompleteLeafList` | M1+M2+M3 |
| P1 | `SourceResultDocument.TryGetPropertyForward`: the record envelope (`data`, `requestIndex`, `variableIndex`) and the source path are read in document order | M1+M2+M3+P1 |

## What the program path does

`ValueCompletion.BuildResult` takes the program path for a record when it has no error trie, the result selection set has no source response name mappings and the target object has a selection set with a program. The program lists the response names the subgraph writes, in its order, with the target value row offset, the selection and how a non-leaf value is completed (object with a child program, object list, leaf list, enum list, or generic). Per property the reader yields the name row and the value row; the name is compared with the entry predicted next and, on a miss, found by a linear scan of the program (an unknown name is skipped, like today). Scalars are written as leaves (enum strings through `CompleteEnumValue`), objects and lists with a program entry run the program, everything else (null, abstract values, nested lists, opaque or aliased selection sets, values of an unexpected kind, existing objects with another selection set) calls today's `TryCompleteValue`/`TryCompleteObjectValue` from the same state and then skips the value's rows. Records with errors, response name mappings, event streams and introspection keep today's path entirely.

Tokenization is unchanged and stays on the read task; the store lock holds the same work as before minus the removed per-property overhead.

## Tests

TBD

## Micro

TBD

## Risks

TBD
