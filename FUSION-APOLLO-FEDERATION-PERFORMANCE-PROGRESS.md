# Fusion Apollo Federation performance investigation

Updated: 2026-10-17 16:08 UTC. Status: **ANALYZING_RESULTS**. Discovery is **closed** and the candidate list is **frozen**. Three candidates are registered; none is accepted yet.

## Active work and decisions

Fresh source builds, composition/wire fidelity and the bounded baseline campaign are complete. Warm CPU/allocation evidence supports the three candidates below. Only these registered mechanisms will now be pursued to terminal decisions; no unrelated ideas will be added.

| Owner | Status | Ownership and next handoff |
|---|---|---|
| Root | INVESTIGATING | Coordination, this report, preservation checks, exclusive measurement queue, final decisions and publication audit. |
| `benchmark_baseline` | ANALYZING_RESULTS | Full native HTTP summaries, source/runtime/configuration identities, lossless baseline evidence publication. |
| `execution_discovery` | ANALYZING_RESULTS | Fresh CPU/allocation traces, recurring-cost evidence and bounded candidate recommendations, with prior experiments excluded. |
| `history_environment` | ANALYZING_RESULTS | Independent full-campaign accounting, headroom, GC/memory and tail review; completed exclusion and composition audits. |

Measurement queue: empty. All granted baseline and profile sessions completed; owner and root verified owned cleanup and the free lock. Shared work explicitly resumed for all workers. No further measurement is authorized without a specific ready request. Full HTTP and CPU/allocation analysis is in progress, and the three evidence-backed candidates are now frozen below.

Plain final cleanup was 15:57:44.579 UTC. Profiles ran 15:58:11.307–16:00:59.462 UTC, target gateway PID 18692 / start ticks 124428, and all three trace collectors exited 0. Analysis verified actual durations 30.089 s CPU / 15.121 s original allocation / 15.080 s entities9 allocation, correct target PID and zero reported event loss. CPU window contains 114,759 successful requests and 68.595359 process CPU-s; original allocation window 61,630 successes and 37.092032 CPU-s. Entities9 window has 138,914 HTTP 200 completions versus 138,913 success callbacks due to one boundary crossing, with 30.031472 CPU-s and no failures. These are diagnostic windows, separate from plain timing. Source files are `profiles/{cpu.nettrace,cpu.speedscope.json,alloc.nettrace,entities9-alloc.nettrace}` under the baseline artifact directory. The runner reused `trace.log`, so the initial CPU console log was overwritten; actual invocation, process identity and machine-readable trace remain. This does not authorize a repeated capture. Raw/lossless data and traces will be compressed and pushed on the baseline evidence branch; frozen binaries are represented by fingerprints and reproduction commands, not indiscriminate build output.

Profile runner SHA-256 `e02f4b626bca2ece21a741932de1ad5aacb46ec495fdb4607ab06ed47bf7285b`, changing only trace duration format to documented `dd:hh:mm:ss`. Plain runner `c5fa13be9f1846333533727f225489dcb341684e41b0fceba797567a4f984d5f` is preserved at `perf/fusion-apollo-baseline-20261017b/campaign-plain.py`; focused driver and frozen binaries unchanged.

Resume started 15:49:33.922 UTC, owner tool session `35157`, campaign PID 14787. The r1-delay0 continuation completed and cleaned up at 15:50:46.319 UTC; r1-delay4 constant and burst passed and cleaned up at 15:53:25.119 UTC. r2-delay0 cleaned up at 15:55:07.951 UTC; r2-delay4 completed at 15:57:44.579 UTC, with no new failure. Burst setup retained the benchmark's 1,000 preliminary requests, excluded from measured scenario accounting. The profile duration syntax correction occurred before any trace capture.

Remaining plain case list: a new r1-delay0 continuation runs original 30 s warmup only, ordinary 20 s and entities9 20 s; then r1-delay4, r2-delay0 and r2-delay4 follow the original plan below. Planned remaining plain load is 410 seconds plus setup. No automatic retries. Builds, restores, tests and substantial offline analysis are prohibited during the slot and otherwise use shared `flock -s -o`.

Exact plain repetitions: two per delay configuration. At 0 ms delay each launch runs 30 s warmup, 30 s original TestQuery, 20 s ordinary and 20 s entities9. At 4 ms delay each launch runs 30 s warmup, 30 s original TestQuery and exact 10/40/10 burst stages with peak 500 VUs. Total planned plain load is 440 seconds plus setup/startup. Profiles use a new 0 ms launch: warm TestQuery, 30 s thread-time trace, 15 s allocation trace, then separately warm entities9 and capture 15 s allocations. No timing follows profiling. No automatic retry or extra repetition is authorized.

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

All new branches start independently from the pinned platform commit. Original benchmark and user prompt remain untouched. Coordination evidence, including the completed PR exclusion audit, is published at `326621ede613b3722a4210b87a437a9d457d628c` on `origin/perf/fusion-apollo-coordination-20261017b` (remote verified). No draft PR or candidate success exists.

## Baseline and workload evidence

Focused smoke passed with 7,417 ordinary and 1,587 entities9 successful scenario requests; exact representative oracle check passed once per VU, status/errors checked on every response. Cleanup finished 15:49:16.966 UTC and free lock verified. This validates the corrected supplemental driver, not performance.

Partial campaign outcome: original `r1-delay0/TestQuery` completed with 122,191 successful scenario requests. The following ordinary-driver setup failed before timing (`ReferenceError: __ITER is not defined`, k6 exit 107). Runner stopped; all owned processes cleaned up by 15:46:37.607 UTC. Cause: focused driver omitted the original script's `typeof __ITER` setup guard. Fix reuses guarded setup IDs without changing measured traffic. Failed evidence remains in `baseline-command.log` and `baseline-r1-delay0/ordinary.log`. Resume only ordinary/entities9 in a new continuation launch plus remaining r1-delay4/r2 configurations; the valid original run is not repeated. Independent headroom/accounting review passed: 122,191 native scenario request/duration/iteration/success records agree; 100 setup requests excluded, zero failures. The 30.00769 s window gives 4,071.99 requests/s and 0.584605 ms gateway process CPU/request. Gateway averaged 2.3805/4 assigned cores, combined subgraphs 3.0528/8, generator 0.8569/2. No aggregate CPU saturation blocks continuation. Median/p95/p99/p99.9 were 11.360/21.675/28.961/42.084 ms. These are baseline observations, not an improvement claim.

Counter JSON ended without the container closing delimiter after non-TTY SIGINT. Raw complete events are preserved and parsed with an explicit incomplete-container flag; no valid timing is repeated for this. Runtime `cpu.time` rows are per-second rates split by user/system, not cumulative totals. Summed 28 intervals report 66.209714 CPU-s versus `/proc` interpolation 66.696015 over the corresponding approximately 27.995 s callback window (0.729% lower). Callback timing, 10 ms ticks and 100 ms interpolation limit alignment; this is not a whole-phase uncertainty bound. Full review/scripts remain local pending publication after measurements.

Plain baseline actually started at 15:45:35.294 UTC (owner tool session `55273`, campaign PID 12647); the first gateway PID is 12653 with `/proc` start ticks 48826. Independent composition audit confirms all four captured `_service` SDLs exactly match archive sources except a terminal newline. FAR SHA-256 is `1bad4cfa6c3b3a6fa0b8dd2746c758c4b94d2fe8640c822240e4f4ea2f13a5af` in benchmark, prepared host and frozen host. Federation 2.7 directives and ports 5221–5224 agree with the actual wire requests. Detailed review: `environment-history/BASELINE-FIDELITY-REVIEW.json` (local, pending publication after the exclusive slot).

Fidelity passed at 15:45:03.188–15:45:06.649 UTC, with owned cleanup complete and lock free. Independent review confirms original TestQuery plan `a1b4bd...` has 13 nodes (2 Operation, 1 ApolloOperation, 10 ApolloOperationBatch); its 7 subgraph HTTP fetches include 4 aliased batch bodies and real `_entities` representations with unchanged Authorization. Observed batches include 6 users, 5 products and three deduplicated single-product batches. The unchanged k6 smoke has 400 successful scenario requests plus 4 setup HTTP requests. Its full structure/type check runs once per VU; this is not an exact-value oracle on every response. Representative exact JSON oracles and source SDL are retained in `perf/fusion-apollo-baseline-20261017b/fidelity/`.

Fidelity frozen manifest SHA-256 `8c468238e50eac57ad679df87d08938dc699935736cc5c8dad7cd5f7125a80ce`; Fusion.Execution DLL `9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b`; host DLL `c6dd072c617cfb4b2ff2ee2d568dbe56ab9ca8261c325360e17c480586db7733`; runner `179b1e49a2afb26a9c849d94e669897881359b9ff7564563572f8079ed617819`. Gateway and subgraph Release builds succeeded. Fidelity-only capture enables plan exposure; timing keeps it disabled. First unmeasured preparation output was preserved when this diagnostic gate was added.

Shared-lock preflight caught inherited lock FDs in completed MSBuild/compiler servers. Root validated 17 exact investigation-owned PID/start identities and terminated them, then verified the lock free. No preexisting .NET processes were present at entry. Cleanup evidence: `perf/fusion-apollo-20261017-43a8dca2/build-server-cleanup.json`. Future shared commands use `flock -s -o` so background build servers cannot inherit the lock.

Discovery source map, byte-identical reused TraceAnalysis helper, CPU analysis, lossless allocation observations and candidate registration are published at `fcb10a2aa8efc58cb5f36e654a8061306ef32e1a` on `origin/perf/fusion-apollo-discovery-20261017b`; its Release build passed.

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

## Candidate register (frozen)

Freeze time: 2026-10-17 16:05 UTC. Evidence uses the unchanged warm TestQuery and nested entities9. The original plan performs 11 representation builds across 7 transports (10 batched members and 1 single member), with 335 target occurrences, 24 distinct representations, and 311 duplicate paths across 8 fetches. All are distinct from the 25 excluded prior mechanisms.

| ID | Status / owner | Bounded hypothesis and smallest prototype | Gate and terminal stopping condition |
|---|---|---|---|
| C1 | PROTOTYPING / `execution_discovery` | Reuse the Apollo deduplication table and reduce repeated oversized clearing, keeping writer/JsonWriter lifetimes unchanged. Warm caller stacks show 5.329 s sampled thread time in representation building, including 2.107 s in dedup resize/rent/zeroing; these are sampled thread times, not exact process CPU or call counts. Allocation caller evidence includes 546 JsonWriter, 349 VariableDedupTable and 334 ChunkedArrayWriter ticks. | Prove parallel-fetch isolation, detached output lifetime, reset/cancellation and retained-capacity limits. Production-path micro: unique 1/5/6/9/96/1024, duplicate 96→1, mixed, null/snapshot and post-large retention. Abandon if ownership cannot be preserved, ordinary/unique cases regress materially, or valid HTTP leaves benefit inconclusive/adverse. |
| C2 | INVESTIGATING / `benchmark_baseline` | Delay per-entity error-attribution `VariableValues[]` until needed while preserving public `ToVariableValues` behavior. Warm allocation samples contain 783 ticks at this caller. Static 64-bit layout estimates 1,224 B per original request, to validate rather than claim as a measurement. | Production-path micro includes ordinary/unique/duplicate/mixed and error fallback; correctness covers null/missing entities, errors, callbacks, cancellation and shared native behavior. Require measured scoped savings and no unresolved repeated adverse HTTP behavior. Abandon on contract failure, negligible practical saving or inconclusive/adverse HTTP. |
| C3 | PROTOTYPING / `history_environment` | Pool duplicate-representation `CompactPath[]` backing with explicit request-store cleanup ownership. This is separate from excluded integer path-segment pools/slot sizing. Largest newly actionable array caller: 1,600 allocation ticks at accumulator application. Static original-request estimate is 2,680 B, pending micro validation. | Prove duplicate error-path mapping, callback/detach lifetimes, cancellation/exception cleanup, parallel/cross-request isolation and bounded retention. Preserve the `EntityResultPath[]`/ImmutableArray contract. Production-path micro covers unique 1/5/6/9, duplicate 96→1 and mixed cases. Abandon on unsafe ownership, adverse micro or inconclusive/adverse HTTP. |

C1 ownership refinement: full scratch reuse would retain the ChunkedArrayWriter first chunk (128 KiB) during downstream waits, potentially about 64 MiB at 500 concurrent requests. That variant is rejected before measurement and preserved in experimental history. The bounded prototype reuses only the dedup table (original-query peak about 8 KiB), releases it on request cleanup/reset/dispose, and passes the active writer explicitly to shared internal lookup callers. Shared native behavior requires focused correctness checks.

C3 ownership audit proposes store-owned pooled duplicate-path backing retained through all pending/error/detached consumers until cleanup, cleared before return, with bounded bookkeeping/oversized fallback. The immutable EntityResultPath array remains unchanged. Tests must establish this lifetime before performance promotion.

All micro timing uses actual baseline/candidate production implementations and explicit independent launches, with validated scoped allocation methods kept separate from default-policy timing. No benchmark runs without a ready request and exclusive slot. Required HTTP includes unchanged TestQuery, nested entities9 and an ordinary control with applicable matched/interleaved controls and fixed inputs. Delayed/burst cases and retained-memory checks apply where the mechanism's concurrency/ownership warrants them. Planned campaigns must preserve every valid run and inspect both adjacent controls; no rerun to seek a favorable result.

The initial original zero-delay envelope is not stationary: r1 4,071.99 requests/s and 0.584605 CPU ms/request versus r2 2,936.12 requests/s and 0.604489 CPU ms/request (throughput −27.90%, CPU/request +3.40%). Original p99.9 is 42.084 versus 46.554 ms. The ordinary 50-VU case uses 1.907/2 generator cores, so its throughput/tails are generator-limited and cannot establish gateway capacity. Future bounded ordinary diagnostics require their own applicable controls and explicit configuration. No valid baseline is repeated to erase this drift.

Not registered: representation-shape lookup and alias-body composition lack sufficient isolated opportunity; pooling `EntityResultPath[]` would conflict with public immutable ownership without a sufficiently small safe change. These are discovery dispositions, not an open backlog. Published historical experiments remain excluded.

Candidate worktrees, all independently based on `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`:

- C1: `.worktrees/fusion-apollo-scratch-20261017b`, `perf/fusion-apollo-scratch-20261017b`. Owns representation scratch/pooling lifecycle, focused tests and evidence.
- C2: `.worktrees/fusion-apollo-error-vars-20261017b`, `perf/fusion-apollo-error-vars-20261017b`. Owns lazy error-variable handling, focused tests and evidence; baseline publication completes first.
- C3: `.worktrees/fusion-apollo-duplicate-paths-20261017b`, `perf/fusion-apollo-duplicate-paths-20261017b`. Owns duplicate path backing/lifetime, focused tests and evidence; independent baseline review completes first.

Prototype branches preserve full accepted or rejected patches, tests and evidence. Only validated successes get clean `tte/<description>` branches and draft PRs; no such branch or PR exists yet.

## Validated successes

None yet.

## Abandoned candidates and negative evidence

None yet.
