# Review Fusion value completion and error handling

Review PR #10967 as it stands in the current worktree against `origin/main`. Include both branch commits, staged and unstaged edits, and untracked tests. Follow `AGENTS.md`. This is a read-only review. Preserve the implementation and its staging.

## Clarity and architecture

My main readability concern is how the code blocks in `ValueCompletion` fit together. Can a maintainer read the ordinary success path from top to bottom and see what happens next and why? Does each helper finish a meaningful piece of work, or does it leave the caller to reconstruct a decision from its outputs?

For example, `GetErrorPath(..., out var isPublic)` returns a path and sets a flag. One caller uses the flag to choose between two paths. Another uses it to decide whether to pass a type to `ForwardErrors`. Even with a perfect name for the flag, would those blocks make sense on first reading? Assess whether those decisions belong with the code that computes the path or with the callers that act on it. Separately, assess whether `isPublic` tells the reader exactly what is public.

Apply the same judgment to other `out` parameters, flags, special cases, duplicated completion paths, and state carried between fetches. Where the flow is hard to follow, point to the blocks a reader has to piece together and show the smallest change that would make their relationship clear. Identify any structure worth keeping for a required behavior or a measured performance benefit.

Consider how planning, result selection, merging, value completion, and serialization divide responsibility. Flag changes that make one layer depend on another layer's implementation details or require the same rule to be maintained in multiple places.

## Correctness

Test every distinct value-completion behavior exercised by the changed code and its callers in both `Propagate` and `Null` modes. Cover scalars and enums, concrete objects, interfaces and unions, lists of each, and nested lists. For each applicable shape, cover successful values, explicit nulls, omitted fields, source-reported errors, and non-null violations detected by the gateway. For lists, distinguish an empty list, a null list, a null item, and a null field on an item. Include nullable and required fields, lists, and items so the expected client response establishes exactly where propagation stops.

- Does the planner retain the selections needed to report a source error below an unshared parent? Does `IsSharedValueType()` handle objects, interfaces, and unions consistently? Is the planner version bump to 2.12.0 appropriate for the change in planning behavior?
- Given what the gateway requested and what each source returned, does the client receive the correct data and errors in both `Propagate` and `Null` modes? Can an error disappear, appear twice, or acquire the wrong client path after an alias, inline fragment, list index, or entity lookup?
- Does a non-null violation stop at the correct nullable ancestor? When a source has already reported errors beneath a nulled field, which errors belong in the client response? Consider separately any errors the gateway generates during completion.
- Cover single-source results and contributions from multiple sources, including shared objects and shared lists. When fetch order changes, do client-visible values, sibling fields, and errors remain correct? Can a later contribution distinguish a field the earlier source explicitly returned as null from one it omitted?

Include additional cases wherever the implementation takes a distinct completion path. Report which behaviors were exercised and identify any untested cases or existing limitations.

## Performance

`ValueCompletion` is an execution hot path. Measure whether the changes improve ordinary successful operations as well as error handling. Successful operations should match or beat the baseline in execution time and allocations. Report each workload separately so a gain on one path cannot hide a regression on another.

Cover the value shapes in the correctness review, including scalars, enums, concrete objects, interfaces, unions, and flat and nested lists. For each distinct completion path, pair a successful response with the same operation and data shape containing a failure. Include explicit nulls in nullable fields or list items and empty lists among the success cases. Exercise omitted required fields, invalid enum values, subgraph errors, and gateway-detected non-null violations in both `Propagate` and `Null` modes. Include single-source completion, shared objects and lists assembled from multiple sources, and entity lookups. Cover the case where one source returns a null shared object and another contributes data while a required child remains missing. Vary list size where the work scales per item.

Use BenchmarkDotNet with commit `6e2b810eff1a8c23d2c5d3380baaf7b6a9eb4396` as the baseline for every benchmark. Microbenchmark affected code paths where feasible, or use full-query execution benchmarks when the cost depends on how the pieces interact. Choose the workloads that answer the question. Report execution time in ns/op, allocations in B/op, and enough measurement uncertainty to judge small differences. Identify comparisons where the client receives a different response, since the two versions may be doing different amounts of work.

## Review report

I don't like assumptions. Ground each finding in observable behavior, a code contract, or measurements. If the evidence cannot resolve a question, say what remains unknown. An inconclusive timing result is insufficient to establish performance parity.

Lead with findings in severity order. Give the file and line, whether each issue was introduced by a branch commit or an uncommitted edit or already exists on `origin/main`, and whether it blocks merging. For behavior bugs, include the smallest reproducing case and the expected and actual client responses. For performance findings, include baseline and working-tree measurements. Finish with the merge verdict, the smallest changes you recommend, and material gaps in coverage.
