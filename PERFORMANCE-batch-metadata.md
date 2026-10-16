# Batched result segment tables

Verdict: **SUCCESS**, for gateway allocation reduction above the metadata-pool boundary. At 5,000 entities, allocation fell from a median 3.435 MB to 1.989 MB/request (42.09%). A fresh pair reproduced a 38.12% reduction. No general gateway CPU or throughput improvement is claimed.

Exact baseline: `9a563e814f3b6213c80a6bc96a01fbcb4d605096`, freshly fetched `origin/main`. Branch: `perf/fusion-batch-metadata-20261016`. The measured candidate is that baseline plus source/test patch SHA-256 `b7854ea9cbec93851c2f9c6c733e7454a4c8bb131cd0f3e6a80d40dc99343ab2`. The frozen baseline patch is empty. The final commit adds this measured code, tests, fixture and report. Frozen binary hashes and source patch identify the measured variant independently of the final commit. Neither earlier finding's fix is included.

Current [JSON-lines parsing](src/HotChocolate/AspNetCore/src/Transport.Http/JsonLines/JsonLinesReader.cs) already has a single-span path for records up to 16 KiB. It rents a one-entry data segment table; [metadata creation](src/HotChocolate/Fusion/src/Fusion.Execution/Text/Json/SourceResultDocument.MetaDb.cs) requests nine entries for tiny records. Both round to sixteen slots in [MemorySegmentTablePool](src/HotChocolate/Utilities/src/Utilities.Buffers/MemorySegmentTablePool.cs), sharing one bounded 4,096-table bucket. Thousands of concurrently owned documents exceed it. Surplus returned tables are dropped, so subsequent warm requests still allocate. This was checked against current source; PR #10548's path-pool batching changes do not remove it.

The prototype changes the minimum power-of-two bucket length from sixteen to one. All existing bucket lengths and capacities remain; the new lengths are 1, 2, 4 and 8. Tiny data tables become 40-byte objects rather than 280-byte objects on this runtime, and no longer consume the metadata bucket. Metadata still rounds nine slots to sixteen. The change preserves clearing, tracking, growth, seal/dispose ownership, and abandoned-arena behavior. It does not return documents early, expand bucket capacity, change batching protocols or planning, or redesign document ownership.

Risks identified before editing were extra bounded pool storage, extra `ReturnBatch` bucket scanning, and earlier growth for callers starting with small tables. Four added bucket slot arrays reserve approximately 128 KiB. Fully populating all four new buckets could additionally retain about 1.31 MiB of table objects on this runtime; these are structural upper bounds, not measured physical memory. Actual memory and allocation are measured below. The extra return scanning did not produce a material control-case regression in this experiment.

The five cases are:

- HTTP 1,000-entity lookup, 10 requests/second, 93,493 response bytes.
- HTTP 5,000-entity lookup, 1 request/second, 473,493 response bytes.
- HTTP plain 5,000-row list, 20 requests/second, 291,707 response bytes.
- BDN arena operation with 2,048 records, each renting tables of minimum lengths 1 and 9.
- The same BDN operation with 5,000 records.

Lookup selections are `upc name price weight inStock shippingEstimate`; plain selections omit the Inventory fields. Every list count and every selected value is checked, including ordered IDs, stock values and required price/weight forwarding into shipping estimates. Additional validation-only nested and deferred controls check 65 users, 189 reviews and 135 products, and all five deferred labels, values, completed IDs and final `hasNext:false`. These controls are not additional benchmark cases. Both variants passed every validation.

The gateway is built in Release from source project references, never a gateway package. Preparation exports four fresh schemas, composes an archive, copies independent frozen binaries, checks project-reference dependencies and hashes DLLs. The deterministic cached subgraphs use packages 16.6.8 and delay each HTTP request 4 ms. Two 30-second warmups precede three unprofiled 60-second repetitions per HTTP case. The lookup offered rates are well below observed closed-loop throughput. Separate diagnostic runs capture counters for 20 seconds, CPU for 30 and allocation for 15 within a 95-second workload after a 60-second warmup. Memory uses a fresh process, 60 seconds of small load, a 60-second large burst at four closed-loop VUs, small load for 60 seconds, and 90 idle seconds, with heaps after normal load, the burst and idle. It reuses the existing two lookup cases at another concurrency point. No builds, tests or offline analysis ran during measurement.

Environment: .NET/ASP.NET 10.0.12, SDK 11.0.100-rc.1.26425.128, ARM64 Debian 12 VM, sixteen Apple virtual CPUs, about 31 GiB RAM, no CPU quota. Gateway CPUs 2,3,10,11; subgraphs 4-7,12-15; k6 1,9. Host contention and frequency are uncontrolled. Server/concurrent GC, minimum 256 threads, maximum 256 downstream connections, batch limit 25,000, admission 64, and deliberately disabled security/cost limits are identical. Tools: BDN 0.15.8, k6 2.3.0, Nitro 16.6.8, diagnostics 10.0.750501 and TraceEvent 3.1.21.

Both gateway variants and BDN jobs use the separately validated measurement-only `DOTNET_GCDynamicAdaptationMode=0`. Earlier calibration observed allocation-counter decreases under default DATAS even when whole-run deltas were positive. Fixed mode samples once per second and rejects observed decreases; none occurred here, including burst phases. Subgraphs retain default GC. This setting is not the optimization, and default-policy absolute allocations/RSS were not independently measured. Matching inner request intervals and completed counts normalize CPU and allocation; `/proc` and runtime counters cross-check the accounting. Outer monitoring boundaries are saved separately.

Tables show median [min,max] across three primary repetitions. MB is decimal; MiB is binary. Latency is the complete load-generator iteration, with HTTP-only percentiles preserved in the numeric summary.

| Case | Metric | Baseline | Candidate | Median difference |
|---|---|---:|---:|---:|
| list-1000-lookup | Completed requests | 601.000 [601.000, 601.000] | 601.000 [601.000, 601.000] | +0.000 (+0.00%) |
| list-1000-lookup | CPU ms/request | 7.724 [5.741, 8.287] | 7.265 [6.972, 8.122] | -0.459 (-5.95%) |
| list-1000-lookup | Allocated B/request | 323,947.794 [323,394.396, 325,599.747] | 323,493.058 [322,543.987, 325,867.354] | -454.735 (-0.14%) |
| list-1000-lookup | Allocated B/s | 3,243,009.120 [3,237,020.809, 3,260,082.904] | 3,238,967.534 [3,229,502.631, 3,261,550.695] | -4,041.587 (-0.12%) |
| list-1000-lookup | p50 ms | 35.816 [25.254, 36.726] | 35.750 [33.716, 38.753] | -0.065 (-0.18%) |
| list-1000-lookup | p95 ms | 50.872 [47.989, 50.978] | 52.511 [51.601, 53.866] | +1.639 (+3.22%) |
| list-1000-lookup | p99 ms | 56.405 [54.311, 56.438] | 57.973 [55.634, 58.272] | +1.568 (+2.78%) |
| list-1000-lookup | Requests/s | 10.011 [10.010, 10.013] | 10.012 [10.009, 10.013] | +0.002 (+0.02%) |
| list-5000-lookup | Completed requests | 61.000 [60.000, 61.000] | 61.000 [61.000, 61.000] | +0.000 (+0.00%) |
| list-5000-lookup | CPU ms/request | 27.489 [26.816, 35.541] | 28.219 [27.256, 29.600] | +0.730 (+2.65%) |
| list-5000-lookup | Allocated B/request | 3,434,530.492 [3,423,497.836, 3,454,860.933] | 1,989,071.869 [1,970,905.574, 2,100,596.459] | -1,445,458.623 (-42.09%) |
| list-5000-lookup | Allocated B/s | 3,473,381.797 [3,454,534.773, 3,486,715.263] | 2,018,047.412 [1,999,613.504, 2,131,394.720] | -1,455,334.386 (-41.90%) |
| list-5000-lookup | p50 ms | 111.758 [106.738, 113.929] | 112.559 [110.627, 113.653] | +0.800 (+0.72%) |
| list-5000-lookup | p95 ms | 124.180 [121.122, 134.649] | 125.819 [124.577, 128.645] | +1.639 (+1.32%) |
| list-5000-lookup | p99 ms | 137.058 [128.739, 142.902] | 139.086 [130.925, 154.028] | +2.029 (+1.48%) |
| list-5000-lookup | Requests/s | 1.015 [1.000, 1.015] | 1.015 [1.015, 1.015] | -0.000 (-0.00%) |
| list-5000-plain | Completed requests | 1,201.000 [1,201.000, 1,201.000] | 1,201.000 [1,201.000, 1,201.000] | +0.000 (+0.00%) |
| list-5000-plain | CPU ms/request | 5.837 [5.634, 5.867] | 5.205 [4.736, 5.325] | -0.632 (-10.83%) |
| list-5000-plain | Allocated B/request | 22,406.062 [22,327.467, 22,591.520] | 22,486.768 [22,423.740, 22,520.240] | +80.706 (+0.36%) |
| list-5000-plain | Allocated B/s | 448,283.313 [446,745.849, 452,038.149] | 449,909.778 [448,736.494, 450,636.060] | +1,626.466 (+0.36%) |
| list-5000-plain | p50 ms | 21.530 [20.982, 21.790] | 19.551 [18.040, 19.832] | -1.979 (-9.19%) |
| list-5000-plain | p95 ms | 25.634 [25.149, 25.977] | 24.313 [24.082, 24.807] | -1.321 (-5.15%) |
| list-5000-plain | p99 ms | 28.514 [26.885, 28.735] | 26.195 [26.181, 27.233] | -2.319 (-8.13%) |
| list-5000-plain | Requests/s | 20.009 [20.007, 20.009] | 20.010 [20.008, 20.012] | +0.001 (+0.01%) |

All measured requests succeeded with zero dropped starts. CPU ranges are broad, especially the small lookup. Warmup CPU fell substantially, and fresh diagnostic processes again showed startup effects. These observations limit gateway CPU conclusions. The large lookup's primary CPU median actually rose 2.65% with overlapping ranges; its fresh confirmation was nearly unchanged. The plain control's CPU drop is also not attributed to this change. Throughput is offered-rate limited and downstream execution remains substantial. No throughput gain is inferred from the four-VU burst.

| Case | Metric per interval | Baseline | Candidate |
|---|---|---:|---:|
| list-1000-lookup | Gateway CPU seconds | 4.642 [3.450, 4.981] | 4.366 [4.190, 4.881] |
| list-1000-lookup | Gen0 collections | 1.000 [1.000, 2.000] | 1.000 [1.000, 1.000] |
| list-1000-lookup | Gen1 collections | 1.000 [0.000, 1.000] | 0.000 [0.000, 1.000] |
| list-1000-lookup | Gen2 collections | 0.000 [0.000, 1.000] | 0.000 [0.000, 0.000] |
| list-1000-lookup | GC pause ms | 6.771 [2.700, 8.396] | 2.724 [1.222, 4.981] |
| list-1000-lookup | GC pause % | 0.011 [0.004, 0.014] | 0.005 [0.002, 0.008] |
| list-1000-lookup | Last-GC heap MiB | 137.865 [135.536, 141.569] | 136.288 [133.911, 138.107] |
| list-1000-lookup | Committed MiB | 298.949 [296.375, 305.117] | 303.074 [299.387, 304.949] |
| list-1000-lookup | Ending RSS MiB | 272.617 [270.707, 309.738] | 297.293 [294.184, 298.582] |
| list-1000-lookup | Observed peak RSS MiB | 309.738 [279.367, 309.754] | 301.070 [300.355, 316.629] |
| list-1000-lookup | Gateway CPU cores | 0.077 [0.057, 0.083] | 0.073 [0.070, 0.081] |
| list-1000-lookup | Combined subgraph CPU cores | 0.326 [0.238, 0.331] | 0.356 [0.318, 0.389] |
| list-1000-lookup | Load-generator CPU cores | 0.015 [0.012, 0.016] | 0.015 [0.014, 0.017] |
| list-5000-lookup | Gateway CPU seconds | 1.677 [1.636, 2.132] | 1.721 [1.663, 1.806] |
| list-5000-lookup | Gen0 collections | 0.000 [0.000, 2.000] | 0.000 [0.000, 1.000] |
| list-5000-lookup | Gen1 collections | 0.000 [0.000, 2.000] | 0.000 [0.000, 0.000] |
| list-5000-lookup | Gen2 collections | 0.000 [0.000, 1.000] | 0.000 [0.000, 0.000] |
| list-5000-lookup | GC pause ms | 0.000 [0.000, 37.155] | 0.000 [0.000, 10.488] |
| list-5000-lookup | GC pause % | 0.000 [0.000, 0.062] | 0.000 [0.000, 0.017] |
| list-5000-lookup | Last-GC heap MiB | 299.314 [299.314, 750.147] | 266.205 [266.205, 266.205] |
| list-5000-lookup | Committed MiB | 373.922 [373.922, 759.699] | 397.758 [397.758, 397.758] |
| list-5000-lookup | Ending RSS MiB | 686.992 [486.691, 843.375] | 419.859 [376.812, 507.141] |
| list-5000-lookup | Observed peak RSS MiB | 686.992 [486.691, 843.375] | 419.859 [376.812, 507.188] |
| list-5000-lookup | Gateway CPU cores | 0.028 [0.027, 0.036] | 0.029 [0.028, 0.030] |
| list-5000-lookup | Combined subgraph CPU cores | 0.182 [0.165, 0.187] | 0.192 [0.186, 0.193] |
| list-5000-lookup | Load-generator CPU cores | 0.007 [0.007, 0.008] | 0.007 [0.007, 0.008] |
| list-5000-plain | Gateway CPU seconds | 7.010 [6.766, 7.046] | 6.251 [5.688, 6.395] |
| list-5000-plain | Gen0 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| list-5000-plain | Gen1 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| list-5000-plain | Gen2 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| list-5000-plain | GC pause ms | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| list-5000-plain | GC pause % | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| list-5000-plain | Last-GC heap MiB | 750.147 [750.147, 750.147] | 266.205 [266.205, 266.205] |
| list-5000-plain | Committed MiB | 759.699 [759.699, 759.699] | 397.758 [397.758, 397.758] |
| list-5000-plain | Ending RSS MiB | 923.273 [897.137, 947.555] | 581.957 [560.785, 608.949] |
| list-5000-plain | Observed peak RSS MiB | 923.273 [897.137, 947.555] | 581.957 [560.785, 608.949] |
| list-5000-plain | Gateway CPU cores | 0.117 [0.113, 0.117] | 0.104 [0.095, 0.107] |
| list-5000-plain | Combined subgraph CPU cores | 0.192 [0.186, 0.195] | 0.171 [0.156, 0.174] |
| list-5000-plain | Load-generator CPU cores | 0.033 [0.030, 0.034] | 0.030 [0.028, 0.032] |

Fresh-process confirmation repeated each lookup case once per variant after the same two warmups. Large baseline then candidate ran first, followed by a separate small baseline then candidate pair:

| Variant | Case | Count | CPU ms/request | Allocated B/request | RPS | p50/p95/p99 ms |
|---|---|---:|---:|---:|---:|---:|
| batch-metadata-baseline-9a563e814f | list-5000-lookup | 61 | 29.262 | 3,370,364.590 | 1.015 | 113.669/124.501/126.898 |
| batch-metadata-baseline-9a563e814f | list-1000-lookup | 601 | 7.542 | 327,349.764 | 10.012 | 36.640/52.128/56.824 |
| batch-metadata-candidate1-9a563e814f | list-5000-lookup | 61 | 29.124 | 2,085,402.885 | 1.015 | 114.625/127.439/132.688 |
| batch-metadata-candidate1-9a563e814f | list-1000-lookup | 601 | 6.285 | 325,370.409 | 10.012 | 30.527/50.948/54.556 |

Large confirmation saved 1,284,962 B/request (38.12%), while CPU changed about -0.47%. Candidate primary allocation ranges do not overlap baseline, and the microbenchmarks independently reproduce the mechanism. Small p95 initially rose 3.22% (1.64 ms), then fell 2.26% in the fresh pair. Large confirmation p95/p99 rose 2.36%/4.56%, while primary p95 ranges overlap. These observed tail changes are reported as uncertainty, not erased by the allocation benefit. Only one fresh pair per lookup was run; this does not establish latency equivalence in all workloads.

BDN uses three launches, five warmups, fifteen measured iterations per launch, automatic invocation calibration, server/concurrent .NET 10 GC and the same four-CPU affinity. Every worker verifies the frozen buffers assembly hash. Setup performs one operation to populate pools. Each operation constructs the real arena, rents one data and one metadata table per record, checks default first slots, seals and disposes. JSON parsing is excluded; tracker growth and batch-return work are included. Synchronous wall time estimates CPU cost for this isolated helper, with no hardware CPU counters. Ranges below are retained BDN samples after reported filtering; all 45 actual iterations per case remain in raw JSON.

| Records | Baseline median ns [min,max] | Candidate median ns [min,max] | Median time difference | Baseline B/op | Candidate B/op | Allocation difference |
|---|---:|---:|---:|---:|---:|---:|
| 2048 | 79,684.580 [78,634.875,86,337.972] | 78,402.579 [76,438.079,82,140.399] | -1,282.001 (-1.61%) | 97,888 | 97,888 | +0.000 (+0.00%) |
| 5000 | 368,725.391 [342,663.203,406,212.678] | 274,599.802 [252,338.671,335,655.422] | -94,125.589 (-25.53%) | 1,849,353 | 485,534 | -1,363,819.000 (-73.75%) |

At 2,048 records, both variants allocate 97,888 B/op, mainly tracker growth; timing ranges overlap. At 5,000, saved allocation is 1,363,819 B/op (73.75%) and median time falls 25.53%. The means are 80.941 to 78.604 µs and 370.067 to 277.483 µs. The boundary case's Gen0 count is 4 per 8,192 operations for both, with no Gen1/2. Large-case BDN Gen0/1/2 counts are 16/15/11 baseline and 22/21/19 candidate per 2,048 operations. Lower allocated bytes did not lower every GC count in this synthetic arena lifetime pattern; no general GC-count win is claimed. Baseline BDN flagged a multimodal distribution, and candidate reported outliers. This was the only measured prototype; no earlier rejected candidate is hidden.

Allocation stacks match current source: baseline `MemorySegment[]` is 50.20% of 48,642,272 sampled bytes, split around 25% each between `JsonLinesReader.ParseDocument` and `MetaDb.CreateForEstimatedRows`. Candidate is 10.53% of 34,387,440 sampled bytes. Source-result wrappers, document wrappers, variable-value arrays and grown tracker arrays remain. Sampling percentages do not measure savings or call counts; unprofiled deltas and BDN do. The representative chain is operation execution → HTTP source client → JSON-lines reader → data table rental / metadata creation → arena.

CPU profiles have 619 versus 820 ms in the reducer's approximate busy subset, with much larger idle/wait totals and 349 versus 346 ms classified as GC suspension. Exclusive `Monitor.Enter_Slowpath` under `StreamPipeReader`, socket receive and uncertain native/GC frames complicate interpretation. Inclusive mapping/parsing/completion groups overlap. The code still performs parsing, correlation, completion and formatting per result. Neither these percentages nor sparse 30-second samples establish a gateway CPU saving.

Runtime counter CPU sums versus aligned `/proc`/endpoint deltas were baseline 0.8865/0.9230/0.9133 s and candidate 0.8285/0.8535/0.8453 s. Allocation increments versus interpolated endpoint deltas were 64,910,176/67,779,306 B and 45,401,560/45,461,641 B. At one bursty request per second, one-second boundary interpolation can shift roughly a request's work, explaining several-percent counter disagreement. These are cross-checks, not independent exact B/request denominators. Primary `/proc` comparisons, raw timestamps and cumulative samples are preserved; no observed counter decrease was accepted.

The separate memory sequence produced the following after-capture states. Last-GC live estimate is heap minus fragmentation; it differs from RSS and committed pages. Captures induce collections and perturb recovery.

| Variant | Phase | Dump objects MiB | Last-GC live estimate MiB | Committed MiB | RSS MiB |
|---|---|---:|---:|---:|---:|
| batch-metadata-baseline-9a563e814f | normal | 118.902 | 118.706 | 277.539 | 288.965 |
| batch-metadata-baseline-9a563e814f | post-burst | 204.785 | 204.583 | 1639.617 | 2599.738 |
| batch-metadata-baseline-9a563e814f | idle | 141.857 | 141.655 | 1115.148 | 1502.637 |
| batch-metadata-candidate1-9a563e814f | normal | 118.571 | 118.376 | 278.512 | 295.316 |
| batch-metadata-candidate1-9a563e814f | post-burst | 214.736 | 214.535 | 1320.109 | 1339.578 |
| batch-metadata-candidate1-9a563e814f | idle | 143.977 | 143.775 | 1267.246 | 1261.949 |

Idle dump total rose about 2.12 MiB; post-burst live total also rose. Candidate idle committed memory is higher, while RSS is lower. Process peak RSS across the memory sequence was about 3.59 GB baseline versus 1.68 GB candidate. This is one unpaired diagnostic sequence under fixed adaptation with induced collections; the difference is not a general RSS saving. Peak sampled/live heaps cannot be treated as equivalent. Dump warnings say .NET 10 lacks the tool's non-lossy heap mode. Segment-table array size labels also do not reliably represent their actual element storage in these reports, so no exact table-byte total is inferred from those labels. Counts and source show the additional bounded small-table retention; aggregate dump/live telemetry is approximate. The successful verdict accepts this small managed-retention tradeoff for a large repeated transient-allocation reduction. A deployment dominated by ordinary requests should evaluate its own memory policy.

Correctness coverage includes complete clearing/recycling across 1,2,4,8,16,64,128 buckets; small odd-size rounding; arena growth preserving every existing slot and clearing every new one; and single tracked ownership after growth. Utility tests passed 38/38. Selected source-document, builder, arena writer and composite-document tests passed 91/91. Variable batching, lookup, defer and cancellation tests passed 52 with one existing planning-related skip. Release gateway, utility, execution and HTTP test builds passed with zero IDE violations; existing AOT-target and obsolete ASP.NET warnings remain. The total selected result is 181 passed and one skipped.

Reproduce with [README commands](benchmarks/fusion-performance/README.md), [preparation](benchmarks/fusion-performance/prepare.py), [HTTP cases](benchmarks/fusion-performance/workloads.py), [measurement runner](benchmarks/fusion-performance/measure.py), [micro runner](benchmarks/fusion-performance/micro.py), [micro cases](benchmarks/fusion-performance/micro/SegmentTablesBenchmark.cs), [analysis](benchmarks/fusion-performance/analyze.py), and [audit](benchmarks/fusion-performance/audit.py). Prepare exact baseline and candidate checkouts into fresh evidence directories, then build both micro projects before measurement. Run baseline micro/pilot/matrix/profile/memory, candidate micro/matrix/profile/memory, then `confirm --gc fixed --label confirm-fixed-1 --repetitions 1` baseline/candidate. Repeat with `--case list-1000-lookup --label confirm-small-1`. All primary modes use `--gc fixed`; all offline analysis runs after cleanup. Never reuse an existing preparation destination.

The selected tests are run from the checkout's Release `net10.0` executables using `-class HotChocolate.Buffers.MemorySegmentTablePoolTests -class HotChocolate.Buffers.MemoryArenaTests` for Utilities; `-class 'HotChocolate.Fusion.Text.Json.SourceResultDocument*' -class HotChocolate.Fusion.Text.Json.ArenaBufferWriterTests -class HotChocolate.Fusion.Text.Json.CompositeResultDocumentTests` for Execution; and `-class HotChocolate.Fusion.VariableBatchingTests -class HotChocolate.Fusion.LookupTests -class HotChocolate.Fusion.DeferTests -class HotChocolate.Fusion.CancellationTests` for Fusion.AspNetCore. Build each matching test project using `DOTNET_LOCAL_DEV=1 DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1 dotnet build <project> -c Release -m:4 --disable-build-servers` first.

Committed [numeric evidence](benchmarks/fusion-performance/results/summary.json) includes complete primary metrics/ranges, micro statistics, both confirmation pairs, memory states, profile summaries and source/binary audit. Ignored local raw [baseline](/workspaces/platform-1/perf/batch-metadata-baseline-9a563e814f), [candidate](/workspaces/platform-1/perf/batch-metadata-candidate1-9a563e814f), and [campaign](/workspaces/platform-1/perf/batch-metadata-campaign-9a563e814f) hold patches, commands, versions, source dependency checks, hashes, responses, counters, process samples, k6 data, BDN distributions, traces, heaps and build/test logs. They are not distributed through Git.

Final audit verifies the source fingerprint, all frozen DLL hashes, source-project gateway dependencies, identical requests/schema inputs, monotonic fixed-mode counters, exited owned service PIDs and closed ports 5320–5324. The original workspace and user processes were preserved. The measured allocation reduction is larger than variability, repeats in fresh processes and is isolated by BDN; bounded retention and uncertain latency/CPU effects remain explicit limits.
