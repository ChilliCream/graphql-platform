C5: Resolve compact merge-target paths with one metadata read per segment

Status: QUEUED_FOR_MICRO. Release/style build passed, and focused real-document plus existing representation/document tests passed 93/93. The user explicitly reopened discovery after the first frozen candidate set. No new profile, microbenchmark, or VU measurement has run for this candidate.

Baseline: 43a8dca2bd479ddc411d832ed97e42d9fcc8bd85. Independent branch/worktree: perf/fusion-apollo-path-lookup-20261017b. No C1/C2/C3/C4 changes are included.

Evidence: the retained warm CPU trace has 1,065.136 ms of exclusive sampled thread time in FetchResultStore.GetStartResult(CompactPath), 1.489% of 71,543.995 filtered sampled thread milliseconds. This measures sampled time, not call counts or a predicted end-to-end gain. The original operation creates 335 target occurrences; merge fan-out repeatedly traverses object and list paths. Existing accessors reread metadata to obtain ValueKind and then resolve the same object property or array element.

Mechanism: an internal CompositeResultDocument traversal resolves references and reads one DbRow per segment, advances by selection ID or list index, and preserves the logical cursor for terminal values and null ancestors. Unsupported raw-object or invalid traversal shapes return to the original FetchResultStore loop, preserving its exception path. No caching, new lifetime, public API, path-buffer allocation, or pooling policy is introduced.

Exclusion audit: this is distinct from #10548 inner path-segment rent/return batching, #10557 shared pool slots, #10559 arena segment tables, #10560 homogeneous list type/selection resolution, and C3 duplicate outer path arrays. The other 21 authored experiment mechanisms do not change compact result-path traversal.

Correctness plan: real document root/field paths, nested list paths across metadata chunks, shared logical references with a null marker, null ancestor after path creation, invalid traversal fallback, and existing representation merge/error/cancellation coverage. Release build and style checks precede focused execution.

Performance plan: a genuine BenchmarkDotNet comparison of actual production path resolution/merge behavior with identical baseline and candidate engine overlays. Include ordinary/root or short paths and nested duplicate-heavy merge paths, return/consume resolved targets, default runtime timing, and no invented allocation benefit. Meaningful extra CPU cost disqualifies. No VU work before worthwhile positive BenchmarkDotNet proof and an explicit measurement grant.

Immutable profile source: https://github.com/ChilliCream/graphql-platform/blob/2c47637e385c25a3cea65d8df1d9be8b4a37871e/performance/fusion-apollo-discovery-20261017b/results/cpu-summary.json

Focused validation note: the first run had one incorrect test oracle that conflated null markers with IsNullOrInvalidated. Production remained unchanged; the final test checks actual formatted logical-reference JSON with snapshots. Both runs are retained.
