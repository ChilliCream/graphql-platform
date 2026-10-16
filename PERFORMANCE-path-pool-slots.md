# Fusion path pool slot storage

Verdict: **SUCCESS**, reduced retained managed metadata. No gateway throughput or steady-state CPU improvement is claimed.

The repeated normal-load heaps contain one pool reference array of **108,962,104 bytes** on baseline and **32,792 bytes** on the candidate: **108,929,312 bytes (103.883 MiB) less live slot metadata**. This holds with default GC and the separately validated fixed-heap measurement configuration. Default-GC normal committed heap is 105.6–122.2 MiB lower in these captures. Physical RSS ranges overlap, so this is not a claim of a 103.9 MiB RSS reduction.

## Source and mechanism

Baseline is `3a7d93b0eea45a365bc80d6445ba3d519865967b`, freshly fetched `origin/main`. Branch `perf/fusion-path-pool-slots-20261016` starts independently from that commit. The measured final candidate is that source plus patch SHA-256 **`358fa9a6a94a11dfe893e88abeadb4f6402765fcb55ec9f3639fc0565440d489`**. Its [exact patch](/workspaces/platform-1/perf/path-pool-candidate2-3a7d93b0ee/production.patch), [identity](/workspaces/platform-1/perf/path-pool-candidate2-3a7d93b0ee/identity.json), and [frozen assembly hashes](/workspaces/platform-1/perf/path-pool-candidate2-3a7d93b0ee/gateway-binaries.json) identify the measured uncommitted tree. The final commit contains the same production and test source. The gateway has 30 HotChocolate source-project dependencies; no NuGet gateway implementation is used.

[PR #10548](https://github.com/ChilliCream/graphql-platform/pull/10548) is merged at this baseline. Its batches of 32 and retained request-local pools are present. The remaining constructor allocates the maximum reference table even with `preAllocate:false`. `PathSegmentMemory` supplies levels from 4,096 to 13,620,260. See [PathSegmentMemory](src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/PathSegmentMemory.cs) and [PathSegmentPool](src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/PathSegmentPool.cs).

The hypothesis was that allocating slot storage at the current level would remove the unused live reference reservation. The candidate initializes `_buffers` at `levels[0]`, grows it under the existing batch lock, and shrinks it with the logical trim level. Growth occurs when every existing slot is rented and null, so it creates an empty replacement. Shrink preserves the free prefix. Trim decisions, index changes, and level changes now share the same lock. Segment sizes, maximum logical capacity, batch sizes, ownership, exhausted-pool fallback, and local retention stay the same.

The main regression risks are allocation on growth/shrink, lock duration at large levels, and returning arrays while trimming. The existing pool can grow through very large configured levels; upper-limit copy/GC pauses have not been benchmarked. The final implementation copies references on shrink and does not copy the empty old storage on growth.

## Cases and protocol

There are five primary cases. Nested and deferred requests are validation controls.

| Case | Definition | Load |
|---|---|---|
| Small lookup | 10 products, six selected fields | 100 requests/s |
| Medium lookup | 1,000 products, same fields | 20 requests/s |
| Large lookup | 20,000 products, same fields | 5 requests/10s |
| Pool construction | Actual source-built pool, production levels, no contained-array preallocation | BenchmarkDotNet |
| Pool growth/return/trim | Rent 6,145 arrays, return them, trim twice | BenchmarkDotNet |

Every HTTP primary case has two 30-second warmups followed by three unprofiled 60-second repetitions. Rates are below gateway saturation. k6 allocates 64 VUs for arrival-rate runs. A separate C4 closed-loop large burst lasts 60 seconds between small-load phases; idle recovery is observed for 90 seconds. Each variant has CPU sampling, allocation stacks, and runtime counters from a separate 1,000-element diagnostic session. No builds, tests, or offline trace reduction run during measurements.

BenchmarkDotNet 0.15.8 uses .NET 10, server/concurrent GC, three process launches, five warmup iterations, and fifteen measured iterations per launch. Construction has one invocation per iteration; growth calibrates invocations. Both cases completed 45 actual iterations per source variant. Construction iterations are short, and timing is sensitive to timer/scheduling overhead. The tables use BDN statistics after its outlier treatment; all raw iterations remain available. Reflection binds a trim delegate during setup, outside timing. Each worker checks the frozen Fusion DLL hash. The micro timer period is one day and trim is explicit; the HTTP timer period remains one minute.

The first micro prototype used `Array.Resize` for growth. It measured 85.838 µs median for growth/trim versus baseline 82.125 µs, with overlapping ranges, and 778,616 versus 573,720 bytes/operation. The final prototype replaces that empty-storage copy with a fresh array. Both iterations have the same allocation cost; the final timing overlaps baseline. Prototype 1 has no HTTP measurements and is preserved at [prototype artifacts](/workspaces/platform-1/perf/path-pool-candidate1-3a7d93b0ee) with source patch `d4aecc36abdd73d232d8dea647cad8fdf026651f67cbe2180201aa81c7b7decc`.

## Configuration and validated responses

SDK is 11.0.100-rc.1.26425.128, MSBuild 18.11.0-1.26425.128. Applications use .NET/ASP.NET Core 10.0.12, Release net10.0. The Debian 12 ARM64 VM has 16 Apple virtual CPUs and about 31 GiB RAM. There is no cgroup CPU quota. Physical-host frequency and contention are not observable. Gateway affinity is 2,3,10,11; subgraphs share 4-7,12-15; k6 uses 1,9. Process monitors record visible competing work throughout each interval. See [cgroup/affinity record](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/environment.json), [hardware](/workspaces/platform-1/perf/path-pool-baseline-3a7d93b0ee/lscpu.txt), and [SDK/runtime](/workspaces/platform-1/perf/path-pool-baseline-3a7d93b0ee/dotnet-info.txt). Tools: dotnet diagnostics 10.0.750501, k6 2.3.0, Nitro 16.6.8, TraceEvent 3.1.21. [Tool versions and hashes](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/tool-versions.json).

Four packaged subgraphs use 16.6.8. Cached deterministic products and Inventory fallback entities remove repeated fixture generation; GraphQL execution and transport remain real work. Each downstream HTTP request has one controlled 4 ms delay. Both sources use identical fresh schema exports and configuration. The gateway uses source project references, copied into independent frozen binaries. Preparation records commands, package/project identities, and hashes before measuring.

Gateway port is 5320; Accounts/Inventory/Products/Reviews use 5321/5322/5323/5324. Minimum threads are 256, HTTP client connection limit is 256, execution admission remains 64, and HTTP batch limit is 25,000. Default security/cost limits are deliberately disabled. All 20,000 elements are accepted.

Validation checks every `upc`, `name`, `price`, `weight`, `inStock`, and forwarded `shippingEstimate`. Bodies are 895, 93,493, and 1,928,496 bytes. The nested control checks 65 users, 189 reviews, and 135 products. The deferred control checks all five labels, all values, completed IDs, and final `hasNext:false`. The earlier report's aliased deferred fixture returns a null first stock value, so the control here uses five deferred root aliases with fully validated values. These controls are identical between compared sources.

## Counter validation and accounting

Default adaptive GC reproduced the counter problem: allocation fell from 455,046,840 to 336,540,480 during a measured interval while its overall delta remained positive. That run misleadingly reported about 3.8 KB/request after warmups near 24 KB/request. It is preserved and excluded at [invalid default-counter session](/workspaces/platform-1/perf/path-pool-baseline-3a7d93b0ee/matrix-default-counter-invalid) and [counter audit](/workspaces/platform-1/perf/path-pool-baseline-3a7d93b0ee/default-counter-audit.json).

Primary comparisons consistently set gateway-only `DOTNET_GCDynamicAdaptationMode=0`; subgraphs retain default GC. Every fixed-heap run samples the allocation API once/second and rejects observed decreases. Construction allocation provides an additional known-size check: baseline 108,962,352 minus candidate 33,040 equals the exact measured reference-array size difference, 108,929,312 bytes. Default-GC heap confirmations are separate retention checks and do not supply comparative allocation results.

CPU and allocation use the same gateway timestamps and completed counts around actual measured iterations, including drain. CPU ms/request = process CPU seconds × 1,000 / completed; bytes/request = allocation delta / completed. State/monitor overhead is included on both sources. `/proc` CPU is interpolated at those accounting boundaries; the largest difference across primary and confirmation runs is 1.04%. Runtime counter CPU increments agree with aligned `/proc` within 0.55%, and allocation increments with the aligned API within 0.42%. See [accounting/source/cleanup audit](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/audit.json).

Ranges below are min–max of three repetitions; no confidence interval is implied. Latencies are end-to-end k6 iteration durations including complete response and lightweight checks. HTTP-only latency distributions remain in raw summaries. Large-case p99 has only 30–31 observations/run and is statistically weak. All retained primary/confirmation runs have zero client-observed GraphQL/HTTP failures, cancellations, timeouts, or dropped starts.

## HTTP results

| Case: metric | Baseline median [range] | Final median [range] | Difference (%) |
|---|---|---|---|
| 10: CPU ms/request | 1.653 [1.532, 1.685] | 1.839 [1.791, 1.873] | +0.187 (+11.29%) |
| 10: KiB/request | 23.43 [23.43, 23.46] | 23.43 [23.42, 23.51] | +0.01 (+0.02%) |
| 10: Allocation MiB/s | 2.288 [2.288, 2.291] | 2.288 [2.287, 2.296] | +0.000 (+0.01%) |
| 1,000: CPU ms/request | 5.786 [5.765, 6.077] | 5.598 [5.195, 6.324] | -0.189 (-3.26%) |
| 1,000: KiB/request | 312.67 [311.25, 312.77] | 312.16 [310.83, 312.28] | -0.50 (-0.16%) |
| 1,000: Allocation MiB/s | 6.108 [6.082, 6.109] | 6.098 [6.073, 6.102] | -0.010 (-0.17%) |
| 20,000: CPU ms/request | 102.873 [78.497, 111.910] | 91.399 [88.547, 95.042] | -11.473 (-11.15%) |
| 20,000: KiB/request | 17,122.73 [16,145.58, 17,358.25] | 17,053.55 [16,853.10, 18,040.54] | -69.17 (-0.40%) |
| 20,000: Allocation MiB/s | 8.491 [8.006, 8.605] | 8.477 [8.228, 8.959] | -0.014 (-0.16%) |

| Case: metric | Baseline median [range] | Final median [range] | Difference (%) |
|---|---|---|---|
| 10: requests/s | 99.987 [99.987, 99.995] | 99.993 [99.983, 99.994] | +0.006 (+0.01%) |
| 10: p50 ms | 13.389 [13.220, 13.719] | 13.693 [13.537, 13.854] | +0.304 (+2.27%) |
| 10: p95 ms | 17.019 [16.763, 17.233] | 18.590 [17.627, 18.687] | +1.571 (+9.23%) |
| 10: p99 ms | 20.301 [20.205, 21.633] | 24.203 [22.077, 24.687] | +3.902 (+19.22%) |
| 1,000: requests/s | 20.007 [19.999, 20.009] | 20.007 [20.004, 20.009] | -0.001 (-0.00%) |
| 1,000: p50 ms | 26.827 [26.257, 27.059] | 25.448 [24.653, 27.419] | -1.379 (-5.14%) |
| 1,000: p95 ms | 34.515 [34.356, 34.589] | 33.194 [32.971, 34.381] | -1.321 (-3.83%) |
| 1,000: p99 ms | 36.996 [36.676, 37.107] | 36.415 [35.869, 37.999] | -0.581 (-1.57%) |
| 20,000: requests/s | 0.508 [0.508, 0.508] | 0.509 [0.500, 0.509] | +0.001 (+0.15%) |
| 20,000: p50 ms | 995.612 [986.042, 1,004.388] | 986.974 [985.964, 1,003.654] | -8.638 (-0.87%) |
| 20,000: p95 ms | 1,079.344 [1,076.861, 1,097.431] | 1,082.628 [1,064.091, 1,130.532] | +3.284 (+0.30%) |
| 20,000: p99 ms | 1,103.614 [1,091.084, 1,107.723] | 1,090.473 [1,066.813, 1,152.978] | -13.142 (-1.19%) |

Offered load is fixed, so these RPS differences are not capacity gains. The large case remains materially influenced by packaged subgraph execution. Gateway utilization is well below its assigned four CPUs.

| Elements | Completed B/C | CPU seconds total B/C | Gateway cores B/C | Subgraph cores B/C | k6 cores B/C |
|---|---|---|---|---|---|
| 10 | 18,003 / 18,003 | 29.222 / 33.024 | 0.165 / 0.184 | 0.229 / 0.265 | 0.076 / 0.090 |
| 1,000 | 3,602 / 3,603 | 21.165 / 20.558 | 0.116 / 0.112 | 0.468 / 0.405 | 0.017 / 0.018 |
| 20,000 | 93 / 92 | 9.092 / 8.430 | 0.052 / 0.047 | 1.643 / 1.653 | 0.006 / 0.006 |

## Small-case CPU confirmation

The initial small-case comparison ran in separate periods and showed higher candidate CPU and p95. Three additional fresh-process repetitions per source use the same full response validation, two warmups, fixed GC, and 100 requests/s. Source order is baseline, candidate, candidate, baseline, baseline, candidate.

| Pair | Baseline CPU ms/req | Candidate CPU ms/req | Difference ms | Difference % |
|---|---|---|---|---|
| 1 | 1.696091 | 1.695529 | -0.000562 | -0.033% |
| 2 | 1.678917 | 1.822573 | +0.143656 | +8.556% |
| 3 | 1.741378 | 1.756002 | +0.014624 | +0.840% |

The median paired CPU delta is +0.014624 ms/request (+0.840%). The difference of the two marginal medians is a different statistic, +0.059910 ms/request (+3.53%). Ranges overlap; one pair remains an outlier. These data do not support a steady-state CPU speedup or a repeatable regression beyond observed variability. The initial measurements remain visible above.

| Metric | Baseline median [range] | Final median [range] | Difference (%) |
|---|---|---|---|
| CPU ms/request | 1.696 [1.679, 1.741] | 1.756 [1.696, 1.823] | +0.060 (+3.53%) |
| KiB/request | 23.43 [23.41, 23.43] | 23.49 [23.48, 23.50] | +0.06 (+0.27%) |
| p95 ms | 16.642 [16.316, 16.884] | 16.904 [16.559, 16.998] | +0.262 (+1.58%) |
| requests/s | 99.985 [99.985, 99.992] | 99.986 [99.981, 99.988] | +0.001 (+0.00%) |

## GC and process memory during primary repetitions

Committed heap and RSS are process readings at accounting end. Observed peak RSS includes endpoint and one-second process samples; instantaneous peaks can be missed. Last-GC heap values and fragmentation are retained in JSON and are not treated as live-object measurements. Generation counts include collections of lower generations and must not be summed.

| Case: metric | Baseline median [range] | Final median [range] | Difference (%) |
|---|---|---|---|
| 10: Gen0 | 0 [0, 0] | 0 [0, 1] | +0 (n/a) |
| 10: Gen1 | 0 [0, 0] | 0 [0, 1] | +0 (n/a) |
| 10: Gen2 | 0 [0, 0] | 0 [0, 0] | +0 (n/a) |
| 10: GC pause ms | 0.000 [0.000, 0.000] | 0.000 [0.000, 34.047] | +0.000 (n/a) |
| 10: GC pause % | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.057] | +0.000 (n/a) |
| 10: End committed MiB | 332.7 [332.7, 332.7] | 228.9 [228.9, 418.2] | -103.8 (-31.20%) |
| 10: End RSS MiB | 461.1 [360.6, 593.4] | 441.7 [373.8, 494.8] | -19.4 (-4.21%) |
| 10: Observed peak RSS MiB | 474.2 [360.6, 593.4] | 442.7 [373.8, 505.6] | -31.5 (-6.65%) |
| 1,000: Gen0 | 2 [2, 3] | 2 [2, 3] | +0 (+0.00%) |
| 1,000: Gen1 | 0 [0, 0] | 0 [0, 0] | +0 (n/a) |
| 1,000: Gen2 | 0 [0, 0] | 0 [0, 0] | +0 (n/a) |
| 1,000: GC pause ms | 2.542 [1.731, 4.189] | 3.039 [2.465, 4.156] | +0.497 (+19.55%) |
| 1,000: GC pause % | 0.004 [0.003, 0.007] | 0.005 [0.004, 0.007] | +0.001 (+19.50%) |
| 1,000: End committed MiB | 530.8 [530.3, 531.8] | 423.9 [423.0, 424.7] | -107.0 (-20.15%) |
| 1,000: End RSS MiB | 500.3 [499.4, 504.2] | 497.8 [497.0, 498.2] | -2.5 (-0.51%) |
| 1,000: Observed peak RSS MiB | 504.2 [501.4, 659.4] | 498.5 [498.4, 502.6] | -5.8 (-1.15%) |
| 20,000: Gen0 | 1 [0, 2] | 1 [1, 2] | +0 (+0.00%) |
| 20,000: Gen1 | 1 [0, 2] | 1 [1, 1] | +0 (+0.00%) |
| 20,000: Gen2 | 0 [0, 1] | 1 [0, 1] | +1 (n/a) |
| 20,000: GC pause ms | 77.609 [0.000, 115.488] | 42.811 [22.655, 67.516] | -34.798 (-44.84%) |
| 20,000: GC pause % | 0.127 [0.000, 0.189] | 0.071 [0.037, 0.111] | -0.056 (-43.86%) |
| 20,000: End committed MiB | 1,096.8 [817.4, 2,007.5] | 1,081.6 [737.7, 1,302.8] | -15.2 (-1.39%) |
| 20,000: End RSS MiB | 1,762.1 [1,371.3, 1,966.9] | 1,135.0 [829.2, 1,371.0] | -627.1 (-35.59%) |
| 20,000: Observed peak RSS MiB | 1,762.1 [1,371.3, 1,968.2] | 1,135.0 [878.4, 1,387.7] | -627.1 (-35.59%) |

## BenchmarkDotNet

Times are µs/operation, shown as mean; median [range]. These are CPU-bound elapsed timings, not process CPU seconds or HTTP savings. Construction occurs once per shared pool, and its result must not be multiplied by request rate.

| Method | Baseline time | Final time | Median time difference | Allocated B/op, B → C | Allocation difference |
|---|---|---|---|---|---|
| ConstructAndDispose | 15.382; 15.146 [7.895, 27.166] | 2.490; 2.208 [1.458, 7.542] | -12.938 (-85.42%) | 108,962,352 → 33,040 | -108,929,312 (-99.9697%) |
| GrowReturnAndTrim | 81.259; 82.125 [72.025, 92.769] | 81.291; 82.058 [76.767, 84.776] | -0.067 (-0.08%) | 573,720 → 778,616 | +204,896 (+35.7136%) |

The growth allocation increment is 204,896 bytes (35.71%), matching four reference-array allocations for the forced cycle. Final mean/median time is effectively unchanged within run variability. The actual production timer trims once per nominal minute, while this stress case forces two trims every operation. The retained-memory saving is about 103.9 MiB per normally warmed shared pool; this explicit allocation tradeoff is accepted for that persistent reduction and lower normal committed heap.

## Live heaps and recovery

| Phase | Live managed MiB, B → C | Live difference MiB | Committed MiB, B → C | RSS MiB, B → C |
|---|---|---|---|---|
| Default GC normal 1 | 110.832 → 6.951 | -103.881 | 142.6 → 20.4 | 157.1 → 138.7 |
| Default GC normal 2 | 111.105 → 6.891 | -104.214 | 121.7 → 16.1 | 136.4 → 131.6 |
| Fixed heaps normal | 111.587 → 7.293 | -104.294 | 253.8 → 149.5 | 265.6 → 263.7 |
| Fixed heaps after burst | 418.794 → 347.738 | -71.056 | 1730.5 → 1441.3 | 1711.9 → 1823.6 |
| Fixed heaps after 90s idle | 206.911 → 123.865 | -83.047 | 1490.9 → 1101.1 | 1533.6 → 1420.8 |

Both default-GC candidate normal captures contain the 32,792-byte array; both baseline captures contain the 108,962,104-byte array. Normal total-live-heap differences closely track that object. The fixed-heap normal capture independently reproduces the reduction.

Large .NET 10 gcdumps carry the tool's lossy-buffer fallback warning, and pool occupancies differ after the burst. Their totals are diagnostic and do not establish the exact removable bytes or an RSS benefit. The C4 burst's observed peak RSS was 1,794.8 MiB baseline and 1,867.2 MiB candidate, a 4.0% increase in this single phase. Post-GC burst RSS was also higher, while 90-second idle RSS was lower. Physical peak/recovery behavior remains uncertain; the verdict rests on repeated normal live metadata and committed-heap evidence, not the burst RSS readings.

## Stack evidence

CPU sampling is thread-time on Linux. Idle semaphore waits, GC suspension/native frames, I/O frames, and uncertain `Monitor.Enter` frames must not be presented as runnable CPU or pool contention. Self and inclusive reports plus complete caller chains are preserved. The representative application chains remain JSON-lines reading → `SourceResultDocument.ParseJson`, result merging/completion, and `RawJsonFormatter.WriteObject/WriteValue`. Sampled time and inclusive percentages are not call counts or possible savings.

The 1,000-entity allocation traces remain dominated by `SourceSchemaResult` (28.8% baseline / 29.6% final sampled weight), `SourceResultDocument` (26.6% / 26.6%), and tracker `MemorySegment[][]` arrays (15.9% / 15.4%). This agrees with nearly unchanged warmed allocation/request. The static slot table is a long-lived constructor allocation; its measured heap and BDN allocation establish the memory result.

Source: [PathSegmentPool](src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/PathSegmentPool.cs), especially initial storage, growth, and synchronized trim; [PathSegmentMemory](src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/PathSegmentMemory.cs), configured levels; [HttpSourceSchemaClient](src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Clients/HttpSourceSchemaClient.cs), result ownership; [ValueCompletion](src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Results/ValueCompletion.cs), completion work.

## Correctness and reproduction

Release gateway and test-project builds pass with zero IDE style errors. The final filtered executable run passes **123 tests**, including all configured growth boundaries, empty/exhausted batches, returned-buffer preservation, preallocation, shrink with outstanding rentals, null/foreign returns, local reset/replacement/straggling returns, document detachment, and eight concurrent renters trimming across 8,000 cycles. Response validation passes for all five HTTP shapes. See [test output](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/tests-candidate2.log) and [test results](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/tests-candidate2.xml). The local `dotnet test` MTP handshake did not execute tests; the built xUnit executable supplied the actual filtered run.

From the original workspace, with the investigation branch checked out in its worktree:

```bash
cd /workspaces/platform-1
export FUSION_PERF_TOOLS=/workspaces/platform-1/perf/tools/bin
export DOTNET_LOCAL_DEV=1
export DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1
FUSION_FIXTURE=/workspaces/platform-1/.worktrees/fusion-path-pool-slots-20261016/benchmarks/fusion-performance
BASELINE_SOURCE=/tmp/fusion-path-pool-repro-baseline
BASELINE_OUTPUT=/workspaces/platform-1/perf/path-pool-repro-baseline-3a7d93b0ee
CANDIDATE_OUTPUT=/workspaces/platform-1/perf/path-pool-repro-candidate-3a7d93b0ee
git worktree add --detach "$BASELINE_SOURCE" 3a7d93b0eea45a365bc80d6445ba3d519865967b
python3 "$FUSION_FIXTURE/prepare.py" "$BASELINE_SOURCE" "$BASELINE_OUTPUT" --tools "$FUSION_PERF_TOOLS"
python3 "$FUSION_FIXTURE/prepare.py" /workspaces/platform-1/.worktrees/fusion-path-pool-slots-20261016 "$CANDIDATE_OUTPUT" --tools "$FUSION_PERF_TOOLS"
python3 "$FUSION_FIXTURE/micro.py" "$BASELINE_OUTPUT" --prepare-only
python3 "$FUSION_FIXTURE/micro.py" "$CANDIDATE_OUTPUT" --prepare-only
```

Build/run the correctness checks before measurement:

```bash
cd /workspaces/platform-1/.worktrees/fusion-path-pool-slots-20261016
dotnet build src/HotChocolate/Fusion/test/Fusion.Execution.Tests/HotChocolate.Fusion.Execution.Tests.csproj -c Release -m:4 --disable-build-servers
src/HotChocolate/Fusion/test/Fusion.Execution.Tests/bin/Release/net10.0/HotChocolate.Fusion.Execution.Tests -class 'HotChocolate.Fusion.Text.Json.PathSegment*' -class 'HotChocolate.Fusion.Text.Json.CompositeResultDocumentTests' -class 'HotChocolate.Fusion.Execution.Results.FetchResultStoreTests'
```

Then run the sequential micro, matrix, profile, memory, and confirmation commands in [the fixture README](benchmarks/fusion-performance/README.md), with the two prepared output directories. Use three `confirm-fixed-*` labels per source and the order above. Default retention confirmation uses `normal-memory --label confirm-default-1` and `confirm-default-2` in baseline/candidate/candidate/baseline order. After all sessions stop, run `analyze.py`, `analyze_profiles.py`, and `audit.py` as documented. Preparation refuses an existing output directory. The micro local solution scopes BDN project discovery despite other worktrees with the same friend-assembly name. Initial project-discovery failures produced no measurements and remain in separate failed-artifact directories.

The [committed reduced summary](benchmarks/fusion-performance/results/path-pool-slots.json) preserves the numeric comparisons and source identities without raw traces.

## Artifact map and verdict audit

Raw artifacts are local to the original workspace, `/workspaces/platform-1/perf/`, and are not committed. No raw-artifact path is assumed to exist inside the isolated worktree.

| Evidence | Location |
|---|---|
| Baseline builds, schema, dependency audit, all run boundaries, responses, profiles and heaps | [baseline directory](/workspaces/platform-1/perf/path-pool-baseline-3a7d93b0ee) |
| Final candidate patch, frozen binaries, identical matrix, profiles, heaps and BDN | [final candidate directory](/workspaces/platform-1/perf/path-pool-candidate2-3a7d93b0ee) |
| First prototype, BDN results and patch identity | [prototype directory](/workspaces/platform-1/perf/path-pool-candidate1-3a7d93b0ee) |
| Primary normalized runs and full median/range groups | [runs](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/analysis/runs.json), [groups](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/analysis/groups.json) |
| Interleaved confirmation | [runs](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/confirmation-analysis/runs.json), [groups](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/confirmation-analysis/groups.json) |
| Full reduced summary, report arithmetic, tool and environment identities | [summary](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/summary.json), [report generator](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/write_report.py), [tools](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/tool-versions.json) |
| Source, fixture, counter and cleanup audit | [audit](/workspaces/platform-1/perf/path-pool-campaign-3a7d93b0ee/audit.json) |

The repeated normal heaps and known-size micro allocation independently demonstrate a 103.883 MiB reduction in permanently live slot metadata. The forced-growth allocation regression is explicit, final growth timing overlaps baseline, and ordinary warmed request allocation is unchanged. CPU, latency, throughput, burst retention, and physical RSS claims are bounded by the variability and limitations above. The source/fixture audit passes, only this finding's production/test/tooling/report changes are included, and all recorded gateway/subgraph/load/profiler/monitor processes have stopped.
