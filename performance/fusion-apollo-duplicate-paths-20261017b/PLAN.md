# C3: duplicate representation path backing

Status: QUEUED_FOR_HTTP. Corrected micro results and limitations are in REPORT.md. Candidate list frozen by the orchestrator. This branch contains only C3, independently based on `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`.

The warm original TestQuery produces 335 representation targets and 24 unique representations across eleven representation builds, eight of which contain duplicate paths. The 311 duplicate paths require eight exact outer `CompactPath[]` allocations, statically estimated at 2,680 bytes/request. Allocation-stack evidence identifies this recurring path. That estimate still requires actual production-path scoped allocation validation.

Fresh baseline evidence: https://github.com/ChilliCream/graphql-platform/tree/ef9fc1159a5a7a980754ebd42b511030320482cd/experiments/fusion-apollo-baseline-20261017b . Earlier investigations supply exclusion context only. This mechanism does not change #10548's underlying integer path segment pool, #10557's pool slot tables, or #10559's memory segment tables.

## Change and ownership

`RepresentationPathAccumulator.ApplyTo` rents an outer array for at most 1,024 duplicate paths and registers it with its `FetchResultStore`. The store returns and clears all owned arrays at Clean, subscription Reset, or Dispose. The owner list is created only on duplicate paths; list capacities greater than 16 are discarded after cleanup. This bounds per-store idle reference-list retention, not the number of live fetches in a request. Arrays above 1,024 entries use the unchanged exact-allocation fallback. Shared ArrayPool retention remains subject to its runtime policy, and active rented buffers can be larger than the former exact arrays.

The exact immutable `EntityResultPath[]` result is unchanged. Underlying integer arrays remain owned by the existing path pool. Pending merges and error expansion consume the outer arrays before store cleanup. Apollo source clients receive one combined root variable value, not per-entity additional paths. Diagnostic tracking explicitly detaches paths before escaping request lifetime. If insertion into the owner list fails, the freshly rented array is returned immediately. Exceptions after registration leave the array owned by the store until its normal cleanup.

## Correctness

Focused tests cover multiple pending representations, concurrent calls through the store lock, Clean/Reset/Dispose with detached diagnostic paths, oversized fallback, existing duplicate result/error expansion, and Apollo source cancellation and post-yield transport exceptions with duplicate targets. The final focused run passed all 70 tests. No public buffer-pooling API, reflection test seam, or changed result-array contract is introduced. A first duplicate-list cancellation fixture was rejected by default cost policy before execution; the fixture records its actual fixed list size through `@listSize` rather than changing security policy. Failed test evidence is retained.

## Planned micro gate

Reuse the C1 fixture skeleton and production schema/planner/result-store methods, with independent C3 outputs. Baseline uses the frozen source-built baseline execution assembly. Candidate uses only this branch's execution assembly. Preseed selected source results outside measurement; production representation creation and store cleanup remain measured. Consume representation values and exact paths. Cases include 1/5/6/9 unique entities, 96 targets deduplicated to one, mixed original-query fetch dimensions, a larger unique case, and oversized duplicate fallback.

Default-GC timing and controlled no-GC scoped allocated-byte validation are separate launches. Validate allocation counters with known allocated arrays and require GC counts to remain unchanged within each controlled interval. Record all raw blocks, independent launch order, warmup, checksums, runtime and binaries. No micro measurements may begin until the root grants an exclusive slot. The joint C1/C3 campaign uses one identical frozen harness and changes only the Fusion execution DLL/PDB for each arm. The planned default-timing launch order is B,C1,C3,B,C3,C1,B,C1,C3,B; controlled allocation launches are separate. Exact bounded settings and raw evidence are owned by the shared runner, with C3 interpretation here.

Stop if ownership is unsafe, ordinary cases regress materially, scoped allocations fail validation, or applicable HTTP comparisons are inconclusive. A favorable micro alone is insufficient. Relevant HTTP comparisons must preserve original TestQuery/configuration/source work and use planned adjacent controls because the fresh baseline exhibited substantial drift. The initial ordinary HTTP baseline saturated the generator; a headroom-safe ordinary comparison requires its own explicitly different configuration and controls.

## Frozen inputs and review

The common harness SHA-256 is `d9d5b1c4b2075ded61a47d43da9d9956dbab38b33d35f10e4cafdc387bb5fac1`. Baseline execution SHA-256 is `9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b`; C3 execution SHA-256 is `359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf`. See the complete hash manifests and production patch in `results/`. Check-only validation passed all 13 cases before measurement. The common baseline and C3 directories differ only in the execution assembly and its symbols.

The root and C1 worker independently reviewed the ownership contract. They identified no new public retaining escape: telemetry detaches paths, source dispatch exposes only the combined root payload, and the existing request-context lifecycle completes consumers before store cleanup. This remains a prerequisite of the optimization. Clearing and returning the new outer arrays occurs before existing path-pool reset.

Build and correctness commands, run from this worktree:

```sh
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock dotnet build src/HotChocolate/Fusion/test/Fusion.Execution.Tests/HotChocolate.Fusion.Execution.Tests.csproj -c Release --disable-build-servers
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock dotnet src/HotChocolate/Fusion/test/Fusion.Execution.Tests/bin/Release/net10.0/HotChocolate.Fusion.Execution.Tests.dll -class '*FetchResultStoreRepresentationTests' -class '*ApolloOperationExecutionNodeTests' -noColor -result-xml performance/fusion-apollo-duplicate-paths-20261017b/results/representation-tests.xml
```

An initial `dotnet test --filter` invocation built successfully but the repository's xUnit v3 in-process runner rejected the test-platform handshake (zero tests ran, exit 5). The explicit runner command above is the applicable correctness result. Build output has zero errors and two existing ASP.NET TestServer deprecation warnings.
