C9: Build compact path segments during the upward parent walk

Status: QUEUED_FOR_MICRO. Independent baseline43a8dca2bd479ddc411d832ed97e42d9fcc8bd85; no C5 or other prototype changes included. Registered after user explicitly reopened discovery.

Warm CPU evidence: CompositeResultDocument.CreateCompactPath has1,220.632ms exclusive sampled thread time,1.706% of71,543.995 filtered milliseconds. Sampled time is not invocation counts or predicted gain. Original representation collection repeatedly creates target paths. Immutable source: https://github.com/ChilliCream/graphql-platform/blob/2c47637e385c25a3cea65d8df1d9be8b4a37871e/performance/fusion-apollo-discovery-20261017b/results/cpu-summary.json

The old method stores64DbRows and64Cursors, then replays the chain. The prototype records only reverse integer segments while visiting the same parents, then feeds the unchanged CompactPathBuilder in forward order. Field IDs, array index calculation, logical references, root handling, visited-row64limit, exception type/text and final path-pool behavior remain unchanged. No allocation saving is claimed. This is distinct from C5 downward lookup and excluded#10548/#10557 path-pool mechanics.

Release/style build passed0errors;151focused document/store/representation tests passed. Four new nested-array/shared-reference/deep-path oracles also passed against exact pinned baseline engine9c436477, verified via loaded module path and SHA. Existing diagnostics are preserved; no benchmark has run.

Independent source review: upward field emission and immediate-child array indices are equivalent to legacy property/value skipping; Cursor.Index spans metadata chunks. No reference chasing, lifetime or pool change; root and64rowboundary preserved. No semantic blocker found.

Screened direction, not registered: final response field-name escape scans account for137.676ms (about0.192%filtered) in existing warm stacks; custom encoder behavior complicates a fast path. No prototype or benchmark was pursued for that direction.
