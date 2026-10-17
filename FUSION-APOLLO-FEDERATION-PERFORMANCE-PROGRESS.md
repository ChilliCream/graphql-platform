# Fusion Apollo Federation performance investigation

Updated: 2026-10-17 15:44 UTC. Status: **INVESTIGATING**. Discovery is open; no optimization candidate has yet been registered or accepted.

## Active work and decisions

The investigation starts from fresh source builds and fresh measurements. The first gate is proving that the source-built gateway runs the benchmark's Apollo Federation composition and `_entities` path. Warm CPU and allocation evidence will determine the candidate list; after it is frozen, only registered candidates will be pursued to terminal decisions.

| Owner | Status | Ownership and next handoff |
|---|---|---|
| Root | INVESTIGATING | Coordination, this report, preservation checks, exclusive measurement queue, final decisions and publication audit. |
| `benchmark_baseline` | INVESTIGATING | Isolated source-built benchmark host, fixed subgraphs, workload fidelity, baseline campaign and reusable measurement assets. |
| `execution_discovery` | INVESTIGATING | Current Apollo execution callers, recurrence hypotheses, correctness and production microbenchmark entry points. No source changes yet. |
| `history_environment` | VERIFYING | Completed 25-mechanism exclusion/environment audit; independent baseline fidelity and measurement-accounting review. |

Measurement queue: baseline preparation pending frozen identities and fidelity proof. The planned initial campaign has two independent gateway launches per delay configuration, original constant and delayed TestQuery, small ordinary and entity-list controls, exact 10/40/10 burst stages, and separate warm CPU/allocation diagnostics. The plan is approved in principle; the exclusive slot has not been granted. No benchmark is running or authorized yet. Builds, restores, tests and substantial offline analysis take a shared lock on `/workspaces/platform-1/perf/fusion-common-path.lock`. A measurement owner needs an explicit root grant and holds its exclusive lock through startup, measurement and owned cleanup.

## Pinned inputs and preservation

- Platform: `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`, both root `main` and fetched `origin/main` on 2026-10-17. Latest commit is Apollo canceled-result cleanup PR #10580.
- Benchmark: `941599e4e27719084189e12d737e147069fdf794`, clean nested checkout at `/workspaces/platform-1/graphql-federation-benchmarks`. Layout and runtime behavior are being inspected before use.
- Root initially has no staged or tracked modifications. Existing untracked paths are `FUSION-APOLLO-FEDERATION-PERFORMANCE-PROMPT.md` and `graphql-federation-benchmarks/`; neither belongs in investigation commits.
- User prompt SHA-256: `f68529c96b8054386919c7da026dc4c70ada734e8c0577222423328fd6e27ba5`; Git blob identity: `f1d6f8f163d969d5835d1d137bafdfffcb320e6f`; no index entry.
- No progress report, `perf/` directory or `.worktrees/` directory existed at entry. Historical worktree registrations remain in `.git`; many refer to missing directories. Preserve these registrations and branches without pruning or restoring historical files over current files.
- Initial identities, worktree registrations and process snapshot: `perf/fusion-apollo-20261017-43a8dca2/initial-state.json`. Only process names/start identities are recorded, avoiding unrelated command environments.
- Read root and benchmark `AGENTS.md` and `docs/profiling.md`. No co-author trailers. Production changes must use project conventions, focused correctness tests and Release/style checks.

## Workspace and publication ownership

| Owner | Branch | Worktree / artifacts |
|---|---|---|
| Root | `perf/fusion-apollo-coordination-20261017b` | `.worktrees/fusion-apollo-coordination-20261017b`; root alone edits the live report, then publishes a copy and coordination evidence here. |
| `benchmark_baseline` | `perf/fusion-apollo-baseline-20261017b` | `.worktrees/fusion-apollo-baseline-20261017b`; baseline assets and evidence. |
| `execution_discovery` | `perf/fusion-apollo-discovery-20261017b` | `.worktrees/fusion-apollo-discovery-20261017b`; source map, reused published trace parser and profile interpretation. |
| `history_environment` | Not yet needed | `perf/fusion-apollo-20261017-43a8dca2/environment-history/` |

All new branches start independently from the pinned platform commit. Original benchmark and user prompt remain untouched. Coordination evidence is published at `b88a4407c4da1b64db39083967594608b393245c` on `origin/perf/fusion-apollo-coordination-20261017b` (remote verified). No draft PR or candidate success exists.

## Baseline and workload evidence

Preparation is in progress, with no measurements yet. Actual environment is ARM64 Linux under OrbStack with 16 allowed CPUs and approximately 33.67 GB RAM; root cgroup CPU and memory are unlimited. SDK is 11.0.100-rc.1.26425.128; the benchmark Federation variant targets .NET 10 (installed runtime 10.0.12). Expected profiling executables and k6 were absent at entry and are being prepared with pinned versions.

The gateway installer defines `FUSION_16_7_OR_GREATER`, enabling `AddGraphQLGateway(disableDefaultSecurity: true)`. The isolated source host must preserve that benchmark policy explicitly, together with minimum worker threads 256 and the net10 runtime selection. Subgraphs remain pinned at Hot Chocolate 16.6.8. `constant-latency` maps to the k6 constant mode plus 4 ms source delay; it is not a fixed-arrival-rate scenario. k6 setup performs `vus * 2` preliminary requests, which must be distinguished from measured request/CPU windows.

Current-source inspection places representation-shape construction and lookup-query rewriting at plan construction, outside warm request optimization scope. Representation serialization and deduplication, copying entity result paths, converting variable values, and queued result merging recur during execution; these remain hypotheses pending warm profiles. The original `TestQuery` in benchmark `k6/k6.js` is the primary unchanged control. Exact query, variables, configurations, load semantics, source and loaded assembly fingerprints, resources and warm profile evidence will be recorded before candidate registration.

## Prior experiment exclusions

The user explicitly instructed us to check their recent open, merged and closed PRs and not pursue those experiments again. Authenticated author is `tobias-tengler`; the last 100 authored PRs were fetched with state `all` and retained in `perf/fusion-apollo-20261017-43a8dca2/authored-prs.json`. This instruction supersedes the earlier permission to revisit old mechanisms with new evidence. The mechanism-level exclusion audit is in progress, and no candidate will be promoted before it completes. General execution areas remain in scope where the proposed mechanism differs from excluded experiments.

The completed audit covers the last 100 authored PRs (back to June 30), independently cross-checks all 75 since September 1, and reads descriptions, comments and file lists for 25 relevant PRs. Exact mechanisms, prior decisions, states, head/merge SHAs, files and verified baseline ancestry are in `evidence/fusion-apollo-20261017/environment-history/AUDIT.md` and `exclusion-register.json` on the coordination evidence branch.

- Open exclusions: #10557 path pool slots, #10558 variable dictionary capacity, #10559 segment tables, #10560 list selection resolution, #10573 source-client scope, #10574 conditional cost allocations.
- Closed exclusions: #10518 broad value-node cache publication changes, #10523 memory-segment compaction, #10554 encoded requirement keys, #10575 lazy variable capacity, #10576 feature snapshots, #10577 feature enumeration, #10578 single-result iterator. Closed does not imply merged or necessarily prove a regression; the audit retains the actual stated reason and limitations.
- Merged exclusions, all verified in the pinned baseline: #10516 UTF8 JSON unescaping, #10517 forwarded UTF8 literals, #10522/#10534 UTF8 input/enum lookups, #10524 forwarded-once-per-fetch serialization, #10527 numeric coercion buffer ownership, #10537 enum literal cache, #10540 numeric cache torn-read fix, #10548 path pool batching/local retention, #10552 response chunked pipe buffering, #10579/#10580 cancellation cleanup.

Other authors' open PRs remain duplication checks, including #9462 result-document work. Current Apollo representation ownership/materialization and alias-body hypotheses have no direct authored PR overlap, but still require fresh warm profile evidence before registration.

## Candidate register

Discovery open. No candidates registered. Source-only hypotheses do not qualify as measured recurring costs. Acceptance requires worthwhile production-path evidence, relevant HTTP validation, correctness checks and published reproduction artifacts. An inconclusive valid campaign terminates without a success claim.

## Validated successes

None yet.

## Abandoned candidates and negative evidence

None yet.
