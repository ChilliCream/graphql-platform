# Environment and prior experiment exclusion audit

Pinned baseline: `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`. Audit UTC: 2026-10-17T15:43:59.261521+00:00.

The latest user instruction excludes previous authored performance experiments from this fresh investigation. Exclusions apply to the actual mechanisms below, not entire files or broad areas such as entity batching. Historical numbers only explain previous decisions and do not establish the new baseline.

## Coverage and sources

Authenticated GitHub user is `tobias-tengler`. The root last-100 authored PR list reaches June 30, 2026; the independent date-filtered query includes all 75 authored PRs created since September 1. Twenty-five relevant PR descriptions, changed-file lists and issue comments were read. Comments were CI coverage bot notes and supplied no extra closure rationale. Full API snapshots and precise production/test paths are in adjacent JSON files.

## Explicit exclusion register

| PR | State / pinned baseline | Excluded mechanism | Prior decision and limitation |
|---|---|---|---|
| [#10580](https://github.com/ChilliCream/graphql-platform/pull/10580) | MERGED / merged ancestor verified | Apollo retained source result disposal when final stream read cancels before merge transfer | Already merged correctness fix. |
| [#10579](https://github.com/ChilliCream/graphql-platform/pull/10579) | MERGED / merged ancestor verified | Native operation retained-result disposal and temporary-buffer return on cancellation | Already merged correctness fix. |
| [#10578](https://github.com/ChilliCream/graphql-platform/pull/10578) | CLOSED | Flatten callback-null zero/one-variable source-result mapping into HTTP-owning iterator | Closed after repeated matched HTTP CPU/request increases: +5.02–6.60% and +2.09–4.78% versus surrounding controls despite 240 B scoped saving. |
| [#10577](https://github.com/ChilliCream/graphql-platform/pull/10577) | CLOSED | Replace layered feature Except(Type/object pairs) with ExceptBy(local Type keys) | Closed for insufficient overall proof: repeated p99.9 increases (24.775/23.399 ms against 19.686/16.288/20.243 ms controls), no established CPU regression. |
| [#10576](https://github.com/ChilliCream/graphql-platform/pull/10576) | CLOSED | Freeze concrete feature dictionary directly when defaults absent, removing intermediate dictionary/enumerator | Closed after repeated matched HTTP CPU/request increases +3.54–5.10% and +4.50–7.26%, despite method allocation saving. |
| [#10575](https://github.com/ChilliCream/graphql-platform/pull/10575) | CLOSED | Lazy variable dictionary capacity hint using JSON property count on net9+, capped by declarations, <=3 definitions skipped | Closed: ordinary 8-variable workload repeated worse p95/p99/p99.9 against both adjacent controls; large-workload CPU mixed. Alternative to #10558. |
| [#10574](https://github.com/ChilliCream/graphql-platform/pull/10574) | OPEN | Reuse runtime deferred-branch chain links within conditional cost evaluation | Open draft. Prior HTTP proof applies to 128-condition all-false fallback, not general gateway traffic. |
| [#10573](https://github.com/ChilliCream/graphql-platform/pull/10573) | OPEN | Lazy single-client scope holder, allocating concurrent dictionary only on fallback | Open draft. Prior supported claim is 520 B scoped bookkeeping saved; no general CPU/throughput gain. |
| [#10560](https://github.com/ChilliCream/graphql-platform/pull/10560) | OPEN | Resolve concrete type and child selection set once per homogeneous object list during value completion | Open draft. Prior fixed-rate large-list HTTP CPU claim; not a new candidate. |
| [#10559](https://github.com/ChilliCream/graphql-platform/pull/10559) | OPEN | Add segment table pool buckets of 1/2/4/8 entries to separate tiny data from metadata | Open draft. Prior large lookup allocation claim has explicit CPU/tail/retention limitations. |
| [#10558](https://github.com/ChilliCream/graphql-platform/pull/10558) | OPEN | Pre-size variable-coercion dictionary by counting supplied properties, capped by definitions and 256 | Open draft. Prior scoped/allocation claim with controlled GC; superseded alternative #10575 is closed but does not change this PR state. |
| [#10557](https://github.com/ChilliCream/graphql-platform/pull/10557) | OPEN | Size shared path-segment pool slot table to current level and grow/shrink under lock | Open draft. Prior normal retained metadata reduction; not stable CPU/RSS proof. |
| [#10554](https://github.com/ChilliCream/graphql-platform/pull/10554) | CLOSED | Weak-cache encoded operation requirement keys and reuse within multi-entity native lookup variable batches | Closed. Prior HTTP observations +6.5%/+10.7% CPU and -4.9%/-6.8% throughput; time-separated controls limit causality but do not establish a success. |
| [#10552](https://github.com/ChilliCream/graphql-platform/pull/10552) | MERGED / merged ancestor verified | Buffer response formatter JSON writes in cached chunked pipe writer before Kestrel writes | Merged in pinned baseline. |
| [#10548](https://github.com/ChilliCream/graphql-platform/pull/10548) | MERGED / merged ancestor verified | Batch shared path-pool rent/return by 32 and retain capped local path pool across pooled requests | Merged in pinned baseline. |
| [#10540](https://github.com/ChilliCream/graphql-platform/pull/10540) | MERGED / merged ancestor verified | Avoid torn numeric value-node caches and add whole-array segment backing with unchanged generic memory support | Merged correctness alternative to earlier #10518; pinned baseline has this version. |
| [#10537](https://github.com/ChilliCream/graphql-platform/pull/10537) | MERGED / merged ancestor verified | Cache enum value literal on schema value and reuse through core/Fusion coercion | Merged in pinned baseline; PR body empty, mechanism verified by changed files and source. |
| [#10534](https://github.com/ChilliCream/graphql-platform/pull/10534) | MERGED / merged ancestor verified | Core UTF8 input/enum lookup and cached NameNode, processed-field index tracking | Merged in pinned baseline. |
| [#10527](https://github.com/ChilliCream/graphql-platform/pull/10527) | MERGED / merged ancestor verified | Reuse request Utf8MemoryBuilder during numeric variable literal conversion and seal/return it correctly | Merged ownership fix in pinned baseline. |
| [#10524](https://github.com/ChilliCream/graphql-platform/pull/10524) | MERGED / merged ancestor verified | Serialize forwarded variables once per native entity fetch and copy raw properties into each variable set | Merged in pinned baseline; buffer retained on pooled store with >16 KB release. |
| [#10523](https://github.com/ChilliCream/graphql-platform/pull/10523) | CLOSED | Compact ReadOnlyMemorySegment from 32 to16 bytes using one object source plus start/length | Closed. Description records mixed memory-owner/forwarding timings; comments contain no explicit closure reason. Baseline still has ReadOnlyMemory<byte> plus source/start/length, not this compaction. |
| [#10522](https://github.com/ChilliCream/graphql-platform/pull/10522) | MERGED / merged ancestor verified | Fusion UTF8 field/enum lookup with schema NameNode reuse and escaped-name fallback | Merged in pinned baseline. |
| [#10518](https://github.com/ChilliCream/graphql-platform/pull/10518) | CLOSED | Rework all value-node cache publication using references/volatile flags and numeric representation changes | Closed. Description has extensive performance tradeoffs but comments provide no explicit closure reason. Related narrower fix #10540 merged; do not reopen this experiment. |
| [#10517](https://github.com/ChilliCream/graphql-platform/pull/10517) | MERGED / merged ancestor verified | Keep coerced forwarded string/enum literals UTF8 and write literal spans in FetchResultStore | Merged in pinned baseline. |
| [#10516](https://github.com/ChilliCream/graphql-platform/pull/10516) | MERGED / merged ancestor verified | Unescape UTF8 JSON strings in JsonValueParser | Merged correctness prerequisite in pinned baseline; empty description, source/file scope confirmed. |

Every merged PR above passes `git merge-base --is-ancestor <merge-sha> 43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`. Exact head/merge SHA and full production/test paths are in `exclusion-register.json`. A closed PR is not assumed merged, and absent closure explanations are marked explicitly.

## Other current open work

Open PR #9462 (other author) changes result document operations; #10515 changes operation planning, #10367 adds WebSocket transport, and #10299 changes diagnostics exception behavior. These are ordinary duplication checks, not new exclusions inferred from the user. The current branch/file must be checked if a candidate overlaps.

## Boundary for current Apollo discovery

No direct authored PR overlap was found for per-fetch Apollo representation buffer/dedup ownership, eager `RepresentationValue.ToVariableValues` on success-only requests, or Apollo alias request-body composition. This does not register or validate candidates. Cached native requirement key bytes (#10554), reuse of child selections within list completion (#10560), and existing source iterator/scope experiments remain excluded.

## Verified environment

Linux arm64 under OrbStack 7.2.9, 16 available CPUs 0-15, Apple vendor without a reliable physical model exposed. Reported 2 GHz is virtualized and is not a hardware-cycle measurement. Memory total 33,670,590,464 bytes; swap configured, unused at audit. `/init` and root cgroup both have unlimited CPU quota/memory and CPUs0-15; no throttling/OOM was recorded. Initial process snapshot showed no benchmark gateway/subgraph/load generator and Docker had no running containers. Preserve unrelated editor/agent processes.

SDK selected by global.json: 11.0.100-rc.1.26425.128; installed runtimes include .NET8.0.31,9.0.20,10.0.12,11 RC. Node24.21.0, npm11.19.0, gh2.102.0. `DOTNET_LOCAL_DEV=1` and `DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1` are active, so standalone/source target properties must be checked. Do not infer runtime from SDK.

Despite docs/profiling.md listing installed tools, no user-global .NET tools, k6, dotTrace or dotMemory were present at standard user/opt paths. Only needed trace/counter tools were installed, under a shared measurement lock, at version10.0.750501. The baseline owner separately prepared k6.

Reproduction for prepared tools:

```sh
flock -s /workspaces/platform-1/perf/fusion-common-path.lock dotnet tool install dotnet-trace --tool-path /workspaces/platform-1/perf/fusion-apollo-20261017-43a8dca2/environment-history/tools --version 10.0.750501
flock -s /workspaces/platform-1/perf/fusion-common-path.lock dotnet tool install dotnet-counters --tool-path /workspaces/platform-1/perf/fusion-apollo-20261017-43a8dca2/environment-history/tools --version 10.0.750501
```

Both report `10.0.750501+ad724c4b85e7ae775e42f393c6e95a7557a57954`. Tool shim hashes and paths are in prepared-tools.json. Binaries are not included in evidence; reinstall the pinned packages.

## Reusable analysis and publication guidance

Reviewed historical allocation parser at [immutable source](https://github.com/ChilliCream/graphql-platform/tree/1e75ce82d3ec18dea4b2083272e0e946ab7c0872/performance/fusion-fresh-alloc/TraceAnalysis). It targets net10.0 with TraceEvent3.1.21, filters actual PID, emits trace start/end/loss and allocation type/caller/timestamp data, and preserves sampled-byte limitations. Reusing its source does not reuse historical numbers. Historical HTTP harnesses depend on native/composite StarWars/eShop fixture assumptions; inspect lifecycle and observation primitives before reuse.

PR10559 is only a prose structure reference. Its median table and accepted adverse tradeoff do not supersede this request: new PR tables require arithmetic Mean with exact specified columns, and repeated adverse HTTP patterns disqualify acceptance.

All source changes, user files, staged state, historical branches and missing/prunable worktree metadata were preserved. This worker creates only owned audit/tool artifacts and performs no workload measurement.
