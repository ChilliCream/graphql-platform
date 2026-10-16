# Variable coercion dictionary capacity

Verdict: **SUCCESS**, for transient allocation reduction. At 200 supplied variables, gateway allocation fell about 22%; direct coercion saved exactly 24,160 B/op. No throughput or general gateway CPU improvement is claimed.

Baseline: `9a563e814f3b6213c80a6bc96a01fbcb4d605096`, freshly fetched `origin/main`. Branch: `perf/fusion-variable-coercion-20261016`. The measured candidate is baseline plus the source/test patch SHA-256 `873541084ad38a3b885afea2a359f18614ca2f3ee4731cadd7552c67a22a5d05`. The frozen baseline patch is empty. Binary hashes, project dependency audit, source patch and commands are recorded in each raw directory. No earlier investigation fix is included. The final commit contains this measured source patch and the fixture/report; the patch fingerprint identifies the measured code independently of that commit.

The backlog combined coercion dictionary resizing with forwarding metadata. Current source still starts the dictionary empty, then inserts every supplied variable. This repeatedly allocates buckets and entry arrays. `OperationPlanContext.GetPathThroughVariables` also creates forwarding fields and names per request. PR #10548 already changed path-pool batching and local retention; it does not pre-size this dictionary. Current source was inspected before this candidate was edited.

The change in [VariableCoercionHelper.cs](src/HotChocolate/Fusion/src/Fusion.Execution/Execution/VariableCoercionHelper.cs) counts supplied JSON properties without allocating, bounded by the definition count and 256, and uses that count as initial capacity. Coercion, defaults, required-variable errors, duplicate-property last-value behavior, and forwarding semantics retain their existing paths. Unknown or duplicate names can overestimate capacity, bounded by 256. Default-only requests can still resize. The bounded property scan adds CPU work. Forwarding nodes, type wrapper creation and repeated JSON property searches remain potential costs; this branch makes no claims about eliminating them.

There are exactly five benchmark cases: HTTP requests with 50, 100 and 200 variables, and BenchmarkDotNet direct coercion with 200 nullable definitions and either 1 or 200 supplied values. HTTP selections remain the same two aliases, one each on Products and Inventory. Both receive all values through nested tags and echo the entire ordered list. Validation checks both complete strings, no GraphQL errors, and response bytes of 858, 1,658 and 3,458. A validation-only nullable omission control supplies one of 200 values and checks both complete outputs (490 bytes). The four packaged 16.6.8 subgraphs cache deterministic data, with 4 ms delay per HTTP request. Schema export and composition ran separately for both source builds and produced identical schema inputs.

All HTTP cases use 100 requests/second, two 30-second warmups, then three unprofiled 60-second repetitions. Each completed 6,001 requests per repetition, with zero failed requests or dropped starts. Source-project Release gateways run .NET/ASP.NET 10.0.12, SDK 11.0.100-rc.1.26425.128, server/concurrent GC. Gateway affinity is CPUs 2,3,10,11; subgraphs 4-7,12-15; k6 1,9. The VM has 16 ARM64 Apple virtual CPUs and about 31 GiB RAM, Debian 12, no CPU quota. Host contention and clock frequency are uncontrolled. Minimum threads are 256, maximum downstream connections 256, batch limit 25,000, admission 64; security/cost limits are deliberately disabled for this controlled fixture. The four SGs have eight CPUs available and gateway has four; neither saturates in these runs.

Allocation accounting uses the separately validated gateway-only `DOTNET_GCDynamicAdaptationMode=0` configuration for both variants and both BDN jobs. Earlier calibration observed a decreasing `GC.GetTotalAllocatedBytes(true)` counter under default DATAS despite a positive whole-interval delta. Fixed mode checks 1 Hz samples and rejects decreases; none occurred here. This is a measurement configuration, not part of the production change. Default-policy HTTP allocation savings are not independently quantified. Counters, `/proc` and endpoint snapshots use matching timestamps and completed counts; setup/teardown endpoint requests are outside normalized work. Profiler runs are separate from primary results. No builds, tests or trace analysis ran during measurement.

All tables show median [minimum, maximum] across three primary repetitions. B means bytes; KB in prose is decimal. Latencies include the complete load-generator iteration, with HTTP-only percentiles also in the committed numeric summary.

| Variables | Metric | Baseline | Candidate | Median difference |
|---|---|---:|---:|---:|
| 50 | CPU ms/request | 1.567 [1.478, 1.574] | 1.540 [1.509, 1.563] | -0.026 (-1.69%) |
| 50 | Allocated B/request | 41,957.934 [41,948.209, 42,200.173] | 37,616.225 [37,579.491, 37,818.044] | -4,341.708 (-10.35%) |
| 50 | Allocated B/s | 4,195,462.365 [4,194,890.096, 4,219,847.749] | 3,761,522.594 [3,757,914.639, 3,781,458.609] | -433,939.770 (-10.34%) |
| 50 | p50 ms | 7.567 [7.452, 7.587] | 7.472 [7.459, 7.530] | -0.095 (-1.25%) |
| 50 | p95 ms | 9.990 [9.791, 9.991] | 9.901 [9.807, 9.930] | -0.089 (-0.89%) |
| 50 | p99 ms | 12.817 [12.417, 12.990] | 12.289 [12.130, 12.479] | -0.528 (-4.12%) |
| 50 | Requests/s | 99.996 [99.992, 100.002] | 99.997 [99.991, 99.999] | +0.001 (+0.00%) |
| 100 | CPU ms/request | 1.792 [1.772, 1.797] | 1.765 [1.669, 1.770] | -0.027 (-1.53%) |
| 100 | Allocated B/request | 63,806.664 [63,782.395, 63,853.044] | 52,876.737 [52,859.371, 52,940.587] | -10,929.928 (-17.13%) |
| 100 | Allocated B/s | 6,380,285.686 [6,377,932.140, 6,385,082.047] | 5,287,365.112 [5,285,953.568, 5,292,901.803] | -1,092,920.574 (-17.13%) |
| 100 | p50 ms | 8.218 [8.148, 8.254] | 7.975 [7.882, 8.043] | -0.244 (-2.96%) |
| 100 | p95 ms | 10.480 [10.303, 10.483] | 10.526 [10.321, 10.739] | +0.047 (+0.45%) |
| 100 | p99 ms | 12.700 [12.414, 13.027] | 13.030 [12.647, 13.707] | +0.329 (+2.59%) |
| 100 | Requests/s | 99.995 [99.994, 99.997] | 99.994 [99.978, 100.000] | -0.001 (-0.00%) |
| 200 | CPU ms/request | 2.152 [2.127, 2.207] | 2.027 [1.986, 2.057] | -0.125 (-5.81%) |
| 200 | Allocated B/request | 109,436.833 [109,433.154, 109,469.507] | 85,297.317 [85,288.531, 85,464.605] | -24,139.515 (-22.06%) |
| 200 | Allocated B/s | 10,942,446.806 [10,941,406.529, 10,945,955.125] | 8,529,192.722 [8,529,121.313, 8,545,747.884] | -2,413,254.083 (-22.05%) |
| 200 | p50 ms | 8.985 [8.979, 9.029] | 8.920 [8.911, 8.930] | -0.065 (-0.72%) |
| 200 | p95 ms | 11.869 [11.703, 11.922] | 11.977 [11.867, 12.059] | +0.108 (+0.91%) |
| 200 | p99 ms | 14.165 [14.146, 14.189] | 14.360 [14.162, 15.102] | +0.195 (+1.38%) |
| 200 | Requests/s | 99.989 [99.983, 99.991] | 99.993 [99.992, 100.004] | +0.004 (+0.00%) |

The 200-variable CPU median fell 5.81% in the initial unpaired matrix, while the fresh confirmation pair fell only about 2%. Subgraph CPU also changed between sessions. The result supports an allocation win, with insufficient evidence for a stable gateway CPU gain. p95/p99 changes are small; the 200-variable p99 median rose about 1.4%, with overlapping ranges. The offered rate caps throughput, so no saturation throughput claim is possible.

| Variables | Metric per 60 s interval | Baseline | Candidate |
|---|---|---:|---:|
| 50 | Gateway CPU seconds | 9.402 [8.872, 9.447] | 9.243 [9.055, 9.377] |
| 50 | Gen0 count | 2.000 [1.000, 2.000] | 1.000 [1.000, 2.000] |
| 50 | Gen1 count | 0.000 [0.000, 1.000] | 0.000 [0.000, 1.000] |
| 50 | Gen2 count | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 50 | GC pause ms | 6.197 [4.275, 17.607] | 8.887 [3.085, 9.631] |
| 50 | GC pause % | 0.010 [0.007, 0.029] | 0.015 [0.005, 0.016] |
| 50 | Last-GC heap MiB | 115.950 [114.740, 116.866] | 115.185 [112.873, 116.172] |
| 50 | Committed heap MiB | 282.652 [279.027, 282.902] | 281.027 [271.840, 281.152] |
| 50 | Ending RSS MiB | 262.059 [259.195, 275.887] | 251.852 [251.652, 267.246] |
| 50 | Observed peak RSS MiB | 275.887 [262.059, 276.309] | 274.734 [252.004, 281.070] |
| 50 | Gateway CPU cores | 0.157 [0.148, 0.157] | 0.154 [0.151, 0.156] |
| 50 | Combined subgraph CPU cores | 0.241 [0.233, 0.242] | 0.237 [0.234, 0.241] |
| 50 | Load-generator CPU cores | 0.081 [0.077, 0.083] | 0.080 [0.079, 0.081] |
| 100 | Gateway CPU seconds | 10.755 [10.632, 10.782] | 10.591 [10.014, 10.624] |
| 100 | Gen0 count | 2.000 [2.000, 3.000] | 2.000 [2.000, 2.000] |
| 100 | Gen1 count | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 100 | Gen2 count | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 100 | GC pause ms | 6.228 [4.800, 7.441] | 9.611 [7.116, 11.410] |
| 100 | GC pause % | 0.010 [0.008, 0.012] | 0.016 [0.012, 0.019] |
| 100 | Last-GC heap MiB | 121.093 [120.008, 121.834] | 119.811 [118.850, 121.133] |
| 100 | Committed heap MiB | 287.062 [285.984, 287.750] | 287.973 [282.719, 289.238] |
| 100 | Ending RSS MiB | 257.879 [254.875, 258.316] | 255.961 [252.730, 256.574] |
| 100 | Observed peak RSS MiB | 260.098 [259.227, 262.508] | 255.961 [252.730, 258.629] |
| 100 | Gateway CPU cores | 0.179 [0.177, 0.180] | 0.176 [0.167, 0.177] |
| 100 | Combined subgraph CPU cores | 0.279 [0.278, 0.282] | 0.275 [0.265, 0.279] |
| 100 | Load-generator CPU cores | 0.085 [0.085, 0.086] | 0.082 [0.078, 0.085] |
| 200 | Gateway CPU seconds | 12.912 [12.766, 13.244] | 12.161 [11.917, 12.346] |
| 200 | Gen0 count | 4.000 [4.000, 5.000] | 3.000 [3.000, 4.000] |
| 200 | Gen1 count | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 200 | Gen2 count | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 200 | GC pause ms | 11.729 [11.495, 20.869] | 8.687 [4.769, 10.891] |
| 200 | GC pause % | 0.020 [0.019, 0.035] | 0.014 [0.008, 0.018] |
| 200 | Last-GC heap MiB | 126.365 [124.834, 127.485] | 126.630 [124.401, 127.678] |
| 200 | Committed heap MiB | 301.070 [295.695, 302.762] | 302.504 [292.941, 303.641] |
| 200 | Ending RSS MiB | 271.066 [270.465, 271.926] | 271.980 [267.219, 273.430] |
| 200 | Observed peak RSS MiB | 272.273 [271.520, 273.645] | 273.430 [267.219, 273.559] |
| 200 | Gateway CPU cores | 0.215 [0.213, 0.221] | 0.203 [0.199, 0.206] |
| 200 | Combined subgraph CPU cores | 0.347 [0.347, 0.360] | 0.327 [0.325, 0.330] |
| 200 | Load-generator CPU cores | 0.087 [0.085, 0.087] | 0.082 [0.081, 0.082] |

Heap sizes are last-GC telemetry, not forced full-GC live heaps. This is an allocation investigation; no retained-memory or RSS improvement is claimed. One-second peak sampling can miss shorter peaks. Gen0 reductions are consistent with reduced allocation, but GC pauses remain noisy and small.

A fresh baseline followed by a fresh candidate repeated the 200-variable case, each with two 30-second warmups and one 60-second measured interval:

| Variant | Requests | RPS | CPU ms/request | Allocated B/request | p95 ms |
|---|---:|---:|---:|---:|---:|
| variables-baseline-9a563e814f | 6001 | 99.987 | 2.160 | 109505.080 | 11.949 |
| variables-candidate1-9a563e814f | 6001 | 99.996 | 2.116 | 85300.933 | 11.624 |

Confirmation saved 24,204.147 B/request (22.10%). Only one fresh confirmation pair was run; the allocation difference is also independently repeated by the primary matrix and three BDN launches.

BenchmarkDotNet 0.15.8 uses the same frozen source-built Fusion assembly (hash checked in every worker) and composed schema, .NET 10 server/concurrent GC with fixed adaptation, three launches, five warmups and fifteen measured iterations per launch. Parsing definitions and input JSON is setup work. Each operation executes the actual coercion helper. The sparse case scans all 200 definitions and supplies only one property. Timing is calibrated ns/op, not a direct CPU hardware counter; the isolated synchronous helper makes it useful CPU-cost evidence. The runner also records process CPU and RSS. Ranges below are BDN retained sample ranges after its reported outlier filtering, rather than confidence bounds. All 45 raw actual measurements per case were preserved.

| Supplied of 200 | Baseline median ns [min,max] | Candidate median ns [min,max] | Median change | Baseline B/op | Candidate B/op | Allocation change |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 5,324.137 [5,232.056,5,668.441] | 5,427.530 [5,315.665,5,585.425] | +103.393 (+1.94%) | 400 | 400 | +0.000 (+0.00%) |
| 200 | 72,749.339 [71,124.193,74,234.781] | 70,134.746 [69,445.655,74,808.118] | -2,614.593 (-3.59%) | 50,760 | 26,600 | -24,160.000 (-47.60%) |

The sparse median rose 1.94% (about 103 ns), with overlapping sample ranges and unchanged allocation. This is an explicit bounded-scan tradeoff for avoiding 24,160 B in the wide case. Wide mean time was 72.691 to 70.476 µs; sparse mean was 5.352 to 5.425 µs. Wide BDN Gen0 collections were 2 to 1 per 8,192 operations; sparse was zero per 131,072 operations, with no Gen1/2 collections. BDN reported outliers; distributions are preserved. An initial setup-only micro run used an invalid minimal Fusion schema and produced no measurements; it was corrected to the exact composed schema and rerun for baseline before production editing. There are no earlier measured production candidates.

Baseline allocation ticks identify `Entry<string,VariableValue>[]` as 31.65% of sampled bytes, primarily beneath `OperationVariableCoercionMiddleware.TryCoerceVariables` after inlining. Candidate entry arrays are 8.70%. Total sampled bytes over the separate allocation capture were 165,017,800 to 128,042,872. These sampled totals establish location, not per-request savings; the unprofiled counters and BDN establish the reduction. Candidate forwarding `NameNode`, `StringValueNode`, `ObjectFieldNode`, their arrays and `NonNullType` remain visible. The important caller chain is coercion middleware → helper insertion → dictionary arrays; forwarding allocations remain under `OperationPlanContext.CreateVariableValueSets`.

Linux EventPipe CPU samples contain extensive idle waits and uncertain synchronization/GC frames. The reducer separates idle, GC suspension and thread-pool wakeups. Remaining busy samples were 2,726 to 2,341 ms; inclusive coercion groups 634 to 512 ms (23.26% to 21.85% of the reducer's busy set). Baseline exclusive `Buffer.ZeroMemoryInternal` underneath coercion accounts for 539 ms in that set. Property lookup and value-set construction remain. Inclusive groups overlap, inlined methods lose detail, the busy filter is approximate, and sampling does not count calls. These profiles support the mechanism, not a CPU saving calculated from percentages.

Runtime-counter cross-checks over their own aligned profiling intervals show baseline CPU 4.759 s (runtime), 4.768 s (`/proc`), 4.762 s (endpoint); candidate 4.974, 4.979, 4.979 s. Runtime allocation increments versus interpolated endpoint deltas were 213,035,880 versus 212,883,267 B and 167,194,112 versus 166,811,194 B, respectively. Small interpolation/increment-boundary differences are expected. Every primary run also has matching `/proc` CPU in the numeric summary.

[VariableCoercionHelperTests.cs](src/HotChocolate/Fusion/test/Fusion.Execution.Tests/Execution/VariableCoercionHelperTests.cs) adds meaningful complete-value checks at 0, 1, 200 and 512 definitions, explicit null versus omission with unknown input, all defaults, and duplicate last-value behavior. The selected helper and middleware classes passed all 30 tests. Release gateway and test builds passed with zero errors or IDE violations; existing AOT-target and obsolete ASP.NET warnings remain. Full HTTP values are validated for every variant and confirmation session.

Reproduction uses [the fixture README](benchmarks/fusion-performance/README.md), [preparation](benchmarks/fusion-performance/prepare.py), [workloads](benchmarks/fusion-performance/workloads.py), [runner](benchmarks/fusion-performance/measure.py), [micro runner](benchmarks/fusion-performance/micro.py), [micro cases](benchmarks/fusion-performance/micro/VariableCoercionBenchmark.cs), [analysis](benchmarks/fusion-performance/analyze.py) and [audit](benchmarks/fusion-performance/audit.py). Create an exact baseline checkout and the measured candidate checkout, set the three environment variables in the README, prepare both destinations, and build both micro projects before measuring. Run baseline micro, pilot, matrix and profile; candidate micro, matrix and profile; then one baseline and candidate `confirm --gc fixed --case variables-200 --label confirm-fixed-1 --repetitions 1`. Do all offline reduction after services stop. `prepare.py` fails if a destination already exists; use fresh directories.

Exact test commands, from the candidate checkout:

```bash
DOTNET_LOCAL_DEV=1 DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1 dotnet build \
  src/HotChocolate/Fusion/test/Fusion.Execution.Tests/HotChocolate.Fusion.Execution.Tests.csproj \
  -c Release -m:4 --disable-build-servers
src/HotChocolate/Fusion/test/Fusion.Execution.Tests/bin/Release/net10.0/HotChocolate.Fusion.Execution.Tests \
  -class HotChocolate.Fusion.Execution.VariableCoercionHelperTests \
  -class HotChocolate.Fusion.Execution.OperationVariableCoercionMiddlewareTests
```

Committed [numeric results](benchmarks/fusion-performance/results/summary.json) contain all primary metrics, ranges, confirmation results, BDN statistics and audit. Raw [baseline](/workspaces/platform-1/perf/variables-baseline-9a563e814f), [candidate](/workspaces/platform-1/perf/variables-candidate1-9a563e814f) and [campaign](/workspaces/platform-1/perf/variables-campaign-9a563e814f) contain patches, hashes, exported schemas, responses, process/utilization samples, matching interval boundaries, full k6 summaries, BDN logs/distributions, counters and raw traces. These ignored local artifacts are not distributed by Git. SDK/runtime/hardware outputs are captured per variant; k6 2.3.0, Nitro 16.6.8, diagnostic tools 10.0.750501 and TraceEvent 3.1.21 were used.

The final audit verifies source fingerprint, all frozen binary hashes, all project-reference gateway dependencies, identical payloads and schema inputs, counter checks, exited service PIDs and closed ports 5320–5324. The original workspace and unrelated processes were preserved. The successful change is limited to dictionary allocation: the measured reduction exceeds observed variability at every HTTP size and survives fresh-process confirmation, with a small sparse micro timing tradeoff and no material HTTP regression demonstrated.
