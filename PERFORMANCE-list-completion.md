# Concrete list completion

Verdict: **SUCCESS**, for concrete object-list completion in this .NET 10 ARM64 fixture. Direct completion is about 24% faster with unchanged BDN allocations. Six fresh interleaved HTTP sessions confirm a median 6.40% reduction in gateway CPU/request at 20,000 rows, with paired reductions ranging from 1.25% to 9.37%. The range and the narrower confirmation result matter more than the initial unpaired 11.33% CPU reduction. No throughput improvement is claimed.

Exact baseline: `9a563e814f3b6213c80a6bc96a01fbcb4d605096`, freshly fetched `origin/main`. Branch: `perf/fusion-list-completion-20261016`. The measured candidate is baseline plus tracked source/test patch SHA-256 `68464d69593b2d4a063fe0f61b5e543751ae565b91003a589c0ea0a2a7581af1`. The frozen baseline patch is empty. The final commit contains this measured patch, fixture and report. Frozen DLL hashes and patch identity identify the executable code independently of the final commit. No earlier finding's production fix is included.

The backlog identified parsing, completion and formatting work for large homogeneous lists. Current source already caches concrete child selection sets in `Selection.GetSelectionSet(typeContext)`, uses prepared `SelectionSet.ObjectTemplate` rows, handles scalar fields directly, and resolves abstract type names through existing UTF-8 paths/memos. PR #10548 and current source were checked before editing. An uncached selection layout or repeated type-name string allocation is not the mechanism claimed here.

The narrower hypothesis was that [ValueCompletion.TryCompleteList](src/HotChocolate/Fusion/src/Fusion.Execution/Execution/Results/ValueCompletion.cs) still enters the selection-based object helper per row, resolves the same concrete type and invokes the cached child-selection resolver per new object. The prototype keeps the concrete type at list scope, resolves the selection set lazily on the first non-null object, and calls the core object-completion overload with that prepared set. Other callers pass explicit null and keep their existing resolution path. The required parameter is not optional. Per-object target/context initialization, source-name mapping, property enumeration, scalar/enum checks, null propagation, error handling, sibling merging, aliases and final formatting remain. Abstract lists still resolve each runtime type normally. Empty and all-null lists do not resolve a child set eagerly.

This is one measured production prototype, combining loop metadata hoisting and direct core-overload dispatch. The experiment does not isolate their individual contributions or count native instructions/calls. No raw JSON bypass, fused parser, plan-layout rewrite or per-request shared-value cache was added. Those larger changes require substantially more semantic/ownership work. Planning behavior and JSON plan format do not change, so their versions are not bumped.

There are exactly five benchmark cases:

- HTTP plain lists of 1,000, 5,000 and 20,000 rows at 100/s, 50/s and 20/s.
- Direct BDN completion of 1,000 rows.
- Direct BDN completion of 20,000 rows.

All HTTP cases select `upc name price weight`, producing 55,707, 291,707 and 1,206,710 bytes. Validation checks every ordered ID, name, numeric price and weight, count, and absence of GraphQL errors. Additional validation-only nested/deferred controls check all 65 users, 189 reviews, 135 products, five deferred labels and values, completed IDs and final `hasNext:false`. Every baseline, candidate and confirmation validation passed. These controls are not timed benchmark cases. Interface, alias, null/error and shared-result behavior are covered by the selected regression tests.

Release gateways use source project references, never a gateway NuGet package. Preparation rebuilds, exports all four subgraph schemas, composes a fresh archive, freezes independent binary copies and audits all gateway HotChocolate dependencies as projects. Four cached deterministic subgraphs use HotChocolate 16.6.8 packages and delay each HTTP request 4 ms. Their identical schema inputs and request payloads are hashed. The source/binary audit prevents a later build from silently replacing measured DLLs.

Environment: .NET/ASP.NET 10.0.12, SDK 11.0.100-rc.1.26425.128, sixteen ARM64 Apple virtual CPUs, Debian 12 VM, about 31 GiB RAM, no CPU quota. Gateway CPUs 2,3,10,11; subgraphs 4-7,12-15; k6 1,9. Host contention/frequency are uncontrolled. Server/concurrent GC, minimum 256 threads, maximum 256 downstream connections, batch limit 25,000, admission 64 and deliberately disabled security/cost limits are identical. Tools are BDN 0.15.8, k6 2.3.0, Nitro 16.6.8, diagnostics 10.0.750501 and TraceEvent 3.1.21.

Each HTTP case has two 30-second warmups then three unprofiled 60-second repetitions. A 15-second baseline pilot validated the offered rates. Representative profiles run separately: a 60-second warmup then 95 seconds of large-list load containing sequential 20-second runtime counters, 30-second CPU sampling and 15-second allocation sampling. Builds, tests and offline trace analysis were stopped during measurements. One measurement session ran at a time.

Both gateway variants and BDN jobs use the separately validated measurement-only `DOTNET_GCDynamicAdaptationMode=0`. Earlier default DATAS calibration observed cumulative allocation decreases even with positive whole-run deltas. This comparison checks once-per-second cumulative samples and rejects decreases; none occurred. Subgraphs keep default GC. The configuration is not a production optimization, and default-policy absolute allocations/CPU effects are not independently established. CPU and allocations are normalized to matching inner timestamps and completed counts; outer `/proc` samples and runtime counters cross-check their own aligned windows.

Primary tables show median [min,max] across three repetitions. MB is decimal; MiB binary. Latency covers the complete k6 iteration. HTTP-only percentiles and every individual interval remain in the numeric evidence.

| Rows | Metric | Baseline | Candidate | Median difference |
|---|---|---:|---:|---:|
| 1000 | Completed requests | 6,001.000 [6,001.000, 6,001.000] | 6,001.000 [6,001.000, 6,001.000] | +0.000 (+0.00%) |
| 1000 | CPU ms/request | 2.091 [2.079, 2.101] | 1.739 [1.720, 1.842] | -0.351 (-16.79%) |
| 1000 | Allocated B/request | 15,924.610 [15,917.927, 15,966.398] | 15,917.788 [15,905.604, 15,986.909] | -6.822 (-0.04%) |
| 1000 | Allocated B/s | 1,592,280.543 [1,591,690.432, 1,596,487.291] | 1,591,684.171 [1,590,431.490, 1,598,612.380] | -596.372 (-0.04%) |
| 1000 | p50 ms | 10.160 [10.155, 10.177] | 9.492 [9.426, 9.773] | -0.668 (-6.58%) |
| 1000 | p95 ms | 12.790 [12.664, 12.792] | 12.313 [12.191, 12.425] | -0.477 (-3.73%) |
| 1000 | p99 ms | 14.910 [14.511, 15.110] | 14.309 [14.102, 14.374] | -0.601 (-4.03%) |
| 1000 | Requests/s | 99.990 [99.989, 99.994] | 99.994 [99.992, 99.995] | +0.004 (+0.00%) |
| 5000 | Completed requests | 3,001.000 [3,000.000, 3,001.000] | 3,001.000 [3,001.000, 3,001.000] | +0.000 (+0.00%) |
| 5000 | CPU ms/request | 3.070 [2.923, 3.089] | 2.821 [2.815, 2.857] | -0.248 (-8.08%) |
| 5000 | Allocated B/request | 20,105.855 [20,063.608, 20,142.704] | 20,106.554 [20,072.331, 20,147.335] | +0.698 (+0.00%) |
| 5000 | Allocated B/s | 1,005,368.048 [1,003,261.443, 1,007,059.333] | 1,005,433.063 [1,003,710.729, 1,007,429.365] | +65.016 (+0.01%) |
| 5000 | p50 ms | 12.726 [12.208, 12.896] | 12.581 [12.546, 12.596] | -0.145 (-1.14%) |
| 5000 | p95 ms | 14.544 [14.467, 14.644] | 14.135 [13.990, 14.261] | -0.409 (-2.81%) |
| 5000 | p99 ms | 16.099 [16.050, 16.708] | 15.531 [15.435, 15.660] | -0.568 (-3.53%) |
| 5000 | Requests/s | 50.004 [49.996, 50.004] | 50.005 [50.003, 50.005] | +0.001 (+0.00%) |
| 20000 | Completed requests | 1,201.000 [1,201.000, 1,201.000] | 1,201.000 [1,200.000, 1,201.000] | +0.000 (+0.00%) |
| 20000 | CPU ms/request | 8.339 [8.255, 8.363] | 7.394 [7.368, 7.580] | -0.945 (-11.33%) |
| 20000 | Allocated B/request | 35,048.859 [35,020.403, 35,057.572] | 35,286.814 [35,162.058, 35,297.633] | +237.955 (+0.68%) |
| 20000 | Allocated B/s | 701,175.252 [700,656.108, 701,345.490] | 705,932.384 [703,491.627, 705,990.669] | +4,757.131 (+0.68%) |
| 20000 | p50 ms | 27.306 [27.218, 27.370] | 25.943 [25.932, 26.148] | -1.363 (-4.99%) |
| 20000 | p95 ms | 29.523 [29.361, 29.853] | 27.914 [27.848, 28.911] | -1.609 (-5.45%) |
| 20000 | p99 ms | 31.977 [31.534, 32.069] | 30.127 [29.860, 31.281] | -1.850 (-5.79%) |
| 20000 | Requests/s | 20.006 [20.006, 20.007] | 20.007 [19.999, 20.007] | +0.001 (+0.01%) |

All measured requests succeeded with zero dropped starts. The unpaired matrix favors the candidate on CPU and latency at every size, but those percentages include temporal drift and are not the final CPU claim. Allocation is effectively unchanged at the two smaller sizes; large HTTP allocation rises about 0.68% (238 B/request), with async/buffer variation rather than a new allocation site demonstrated. Direct BDN allocations are exactly unchanged. Offered rates cap throughput; no saturation curve or throughput claim is made. The packaged downstream serialization and client response processing remain external costs.

| Rows | Metric per interval | Baseline | Candidate |
|---|---|---:|---:|
| 1000 | Gateway CPU seconds | 12.545 [12.475, 12.609] | 10.439 [10.323, 11.051] |
| 1000 | Gen0 collections | 1.000 [0.000, 1.000] | 1.000 [0.000, 1.000] |
| 1000 | Gen1 collections | 0.000 [0.000, 1.000] | 0.000 [0.000, 1.000] |
| 1000 | Gen2 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 1000 | GC pause ms | 2.935 [0.000, 7.033] | 4.416 [0.000, 6.326] |
| 1000 | GC pause % | 0.005 [0.000, 0.012] | 0.007 [0.000, 0.011] |
| 1000 | Last-GC heap MiB | 121.323 [121.203, 121.323] | 122.723 [121.110, 122.723] |
| 1000 | Committed MiB | 276.566 [276.566, 281.211] | 276.062 [276.062, 282.828] |
| 1000 | Ending RSS MiB | 286.238 [257.656, 293.949] | 255.887 [249.426, 259.012] |
| 1000 | Observed peak RSS MiB | 293.949 [290.633, 293.980] | 260.914 [259.820, 287.504] |
| 1000 | Gateway CPU cores | 0.209 [0.208, 0.210] | 0.174 [0.172, 0.184] |
| 1000 | Combined subgraph CPU cores | 0.272 [0.272, 0.274] | 0.240 [0.230, 0.252] |
| 1000 | Load-generator CPU cores | 0.078 [0.078, 0.081] | 0.070 [0.070, 0.074] |
| 5000 | Gateway CPU seconds | 9.212 [8.770, 9.271] | 8.467 [8.448, 8.574] |
| 5000 | Gen0 collections | 0.000 [0.000, 1.000] | 0.000 [0.000, 1.000] |
| 5000 | Gen1 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 5000 | Gen2 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 5000 | GC pause ms | 0.000 [0.000, 1.892] | 0.000 [0.000, 1.955] |
| 5000 | GC pause % | 0.000 [0.000, 0.003] | 0.000 [0.000, 0.003] |
| 5000 | Last-GC heap MiB | 123.441 [123.441, 123.441] | 123.354 [123.354, 123.354] |
| 5000 | Committed MiB | 282.273 [282.273, 282.273] | 283.641 [283.641, 283.641] |
| 5000 | Ending RSS MiB | 247.820 [247.668, 248.434] | 244.262 [242.742, 245.527] |
| 5000 | Observed peak RSS MiB | 248.434 [248.113, 260.188] | 245.527 [245.129, 247.270] |
| 5000 | Gateway CPU cores | 0.153 [0.146, 0.154] | 0.141 [0.141, 0.143] |
| 5000 | Combined subgraph CPU cores | 0.239 [0.231, 0.244] | 0.241 [0.240, 0.242] |
| 5000 | Load-generator CPU cores | 0.040 [0.039, 0.041] | 0.038 [0.037, 0.039] |
| 20000 | Gateway CPU seconds | 10.015 [9.914, 10.044] | 8.880 [8.849, 9.096] |
| 20000 | Gen0 collections | 0.000 [0.000, 1.000] | 0.000 [0.000, 0.000] |
| 20000 | Gen1 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 20000 | Gen2 collections | 0.000 [0.000, 0.000] | 0.000 [0.000, 0.000] |
| 20000 | GC pause ms | 0.000 [0.000, 1.807] | 0.000 [0.000, 0.000] |
| 20000 | GC pause % | 0.000 [0.000, 0.003] | 0.000 [0.000, 0.000] |
| 20000 | Last-GC heap MiB | 131.137 [131.137, 134.981] | 128.217 [128.217, 128.217] |
| 20000 | Committed MiB | 292.723 [292.723, 296.605] | 287.902 [287.902, 287.902] |
| 20000 | Ending RSS MiB | 256.355 [256.199, 260.387] | 251.703 [250.703, 252.969] |
| 20000 | Observed peak RSS MiB | 257.527 [256.645, 260.387] | 252.176 [251.746, 252.969] |
| 20000 | Gateway CPU cores | 0.167 [0.165, 0.167] | 0.148 [0.147, 0.152] |
| 20000 | Combined subgraph CPU cores | 0.274 [0.274, 0.275] | 0.266 [0.266, 0.269] |
| 20000 | Load-generator CPU cores | 0.035 [0.034, 0.036] | 0.033 [0.032, 0.034] |

Memory values are last-GC telemetry and one-second RSS samples, not forced live heaps or precise instantaneous peaks. This investigation claims no retained-memory or RSS reduction. No new retained layout or request buffer was introduced.

Fresh confirmation used three processes per variant, each with validation, the same two warmups and one 60-second 20,000-row interval. Actual order was baseline-1, candidate-1, candidate-2, baseline-2, baseline-3, candidate-3. The middle pair reverses order. Groups still contain three measured repetitions per variant.

| Pair | Baseline CPU ms/request | Candidate CPU ms/request | Difference | Percentage |
|---|---:|---:|---:|---:|
| 1 | 7.902821 | 7.804318 | -0.098503 | -1.25% |
| 2 | 8.207961 | 7.438938 | -0.769022 | -9.37% |
| 3 | 8.139379 | 7.618140 | -0.521239 | -6.40% |

| Confirmation metric | Baseline median [min,max] | Candidate median [min,max] | Median difference |
|---|---:|---:|---:|
| CPU ms/request | 8.139 [7.903, 8.208] | 7.618 [7.439, 7.804] | -0.521 (-6.40%) |
| Allocated B/request | 35,258.105 [35,065.226, 35,506.425] | 35,419.427 [35,276.596, 35,507.277] | +161.322 (+0.46%) |
| Requests/s | 20.007 [20.005, 20.007] | 20.005 [19.999, 20.007] | -0.002 (-0.01%) |
| p50 ms | 26.802 [26.510, 26.894] | 26.415 [25.978, 26.770] | -0.388 (-1.45%) |
| p95 ms | 29.538 [28.609, 29.886] | 29.049 [28.115, 29.372] | -0.489 (-1.66%) |
| p99 ms | 31.231 [30.756, 32.757] | 30.570 [30.048, 31.833] | -0.661 (-2.12%) |

All three CPU pairs favor the candidate; baseline and candidate CPU ranges are separated in this confirmation set. Median saving is 0.521 ms/request, 6.40%, also the median paired percentage. Pair magnitudes vary considerably (1.25–9.37%). Confirmation allocation medians differ by about 0.46%; latency ranges overlap. The first pair's small effect and the shifted baseline justify reporting the confirmation range rather than the initial unpaired gain. The small/medium HTTP CPU reductions were not separately confirmed in fresh interleaved runs.

BDN compiles the operation and parses immutable source data in setup using the exact composed schema, then validates every completed row/value. Each measured call initializes the real result store over a fresh arena, resets the held source document's binding ID for that sequential call, invokes real `ValueCompletion.BuildResult`, checks count/errors, cleans the store, seals and disposes the result arena. Parsing, HTTP and formatting are excluded; real result allocation and cleanup are included. Workers check the frozen Fusion assembly hash. Source data lives in a held sealed arena until global cleanup. This fixture measures hot completion work with resident parsed input, not total request execution.

Jobs use .NET 10 server/concurrent GC, three launches, five warmups and fifteen actual iterations per launch with automatic invocation calibration. MemoryDiagnoser captures allocation; synchronous elapsed ns/op estimates CPU cost, not hardware cycles or an exact process-CPU denominator. No collections occurred in measured BDN work. All 45 actual iterations per case are retained; ranges below use BDN's reported filtered statistics. A later complete baseline repeat checks drift after all HTTP confirmation.

| Rows | Original baseline median ns [min,max] | Candidate median ns [min,max] | Median difference | Baseline/candidate B/op |
|---|---:|---:|---:|---:|
| 1000 | 114,407.358 [111,729.251,121,987.398] | 86,527.020 [85,358.323,92,009.733] | -27,880.338 (-24.37%) | 472/472 |
| 20000 | 2,218,957.189 [2,130,560.232,2,328,788.102] | 1,688,505.658 [1,647,661.822,1,837,487.616] | -530,451.531 (-23.91%) | 1,656/1,656 |

| Rows | Later baseline median ns [min,max] | Later baseline mean ns | B/op | Candidate change versus later baseline |
|---|---:|---:|---:|---:|
| 1000 | 115,358.685 [112,728.444,120,151.988] | 115,280.856 | 472 | -28,831.665 (-24.99%) |
| 20000 | 2,313,593.227 [2,227,088.182,2,387,849.660] | 2,303,938.561 | 1,656 | -625,087.568 (-27.02%) |

Original means were 115.637 to 87.436 µs and 2.218 to 1.713 ms. Original median reductions are 24.37% and 23.91%; later baseline timings retain the separation. Allocation is unchanged at 472 and 1,656 B/op. All three jobs report zero Gen0/1/2 collections; calibrated operation counts differ where work is faster. BDN filtering/outlier details are saved. An initial fixture build missed the error-handler namespace; its failed logs are preserved, it produced no measurements, and the import was fixed before successful baseline measurement. There are no earlier measured production candidates.

Baseline CPU stacks show `OperationPlanExecutor → ExecutionState.ApplyMerge → FetchResultStore.SaveSafeResult → ValueCompletion.BuildResult → TryCompleteList →` the selection-based object overload. The candidate list goes directly to the core overload with the prepared selection set. Parsing remains under `GraphQLHttpResponse.ReadAsResultInternalAsync → SourceResultDocument.ParseFilled → ParseJson`; formatting still walks composite objects/arrays. The code and caller chains explain a smaller completion boundary, not removal of required GraphQL work.

The approximate filtered CPU sets contain 5,391 ms baseline and 4,670 ms candidate. Inclusive completion is 1,852 versus 1,436 ms (34.34% versus 30.75% of those sets); source parsing is 1,352 versus 1,257 ms. These groups overlap. Idle thread samples dominate raw totals, with 894 versus 872 ms classified as GC suspension and uncertain native/synchronization frames remaining. Sampling does not count calls, and neither the group percentages nor a change in one exclusive frame is used to calculate the claimed CPU saving. JIT/code-generation effects are not isolated from the source-level hoist.

Allocation samples total 9,454,200 versus 9,556,448 bytes. Visible types include pipeline buffer segments, strings, I/O vectors and async HTTP state. No removed allocation site is claimed. Runtime CPU increments versus aligned `/proc`/endpoint deltas are 3.2210/3.2195/3.2229 s baseline and 3.2254/3.2381/3.2341 s candidate. Runtime allocation increments versus interpolated endpoint deltas are 15,101,520/15,369,205 B and 15,032,528/15,312,067 B. These short profiling windows include tooling effects and increment-boundary/interpolation error; they are cross-checks, not exact independent per-request denominators. Every primary and confirmation run retains its matching `/proc` CPU and utilization measurements.

[FetchResultStoreTests](src/HotChocolate/Fusion/test/Fusion.Execution.Tests/Execution/Results/FetchResultStoreTests.cs) adds a complete inline snapshot for empty, all-null and mixed nullable object lists, including an alias and an unselected source property. Selected store/document tests passed 88/88. Lookup, defer, null, require/shared-result, interface, interface-object, query error, source-schema error and shared-path pocketing HTTP classes passed 138, with three existing skips. Gateway and both test projects built in Release with no IDE errors; existing AOT-target and obsolete ASP.NET warnings remain. The total selected result is 226 passed and three skipped.

The evidence supports this scoped CPU change: both direct sizes improve well beyond their retained timing ranges, a later baseline repeat reproduces the separation, all three interleaved large HTTP pairs favor the candidate, and no intrinsic allocation increase or correctness failure appears. The magnitude depends on workload and runtime. Only .NET 10 ARM64, concrete homogeneous lists, the stated load/configuration and hot parsed-input micro cases were measured. Interface/deferred controls establish correctness, not performance equivalence for those shapes. Host contention, scheduling and cache residency remain uncontrolled. There is no basis to extrapolate the full 24% micro result to total gateway CPU, default GC configurations, other architectures, mixed-source workloads, or throughput.

Reproduction uses the [README](benchmarks/fusion-performance/README.md), [preparation](benchmarks/fusion-performance/prepare.py), [HTTP definitions](benchmarks/fusion-performance/workloads.py), [measurement runner](benchmarks/fusion-performance/measure.py), [micro runner](benchmarks/fusion-performance/micro.py), [micro cases](benchmarks/fusion-performance/micro/ListCompletionBenchmark.cs), [analysis](benchmarks/fusion-performance/analyze.py), and [audit](benchmarks/fusion-performance/audit.py). Prepare an exact baseline checkout and candidate into new evidence directories, then build their micro projects before measuring. Run baseline micro/pilot/matrix/profile, candidate micro/matrix/profile, then the six sessions below with the tool environment from the README:

```bash
python3 "$BASE/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-1 --repetitions 1
python3 "$CAND/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-1 --repetitions 1
python3 "$CAND/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-2 --repetitions 1
python3 "$BASE/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-2 --repetitions 1
python3 "$BASE/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-3 --repetitions 1
python3 "$CAND/measure.py" confirm --gc fixed --case list-20000-plain --label confirm-fixed-3 --repetitions 1
python3 benchmarks/fusion-performance/micro.py "$BASE" --label micro-confirm --prepare-only
python3 benchmarks/fusion-performance/micro.py "$BASE" --label micro-confirm --run-only
```

`BASE` and `CAND` are the frozen evidence directories, not source checkouts. Do not run builds or analysis during the measurement commands. Reduce the primary results with `analyze.py "$BASE" "$CAND" --output <primary-output>`, and the confirmation results with the additional `--phase 'confirm-fixed-*'`. Run `analyze_profiles.py` in each evidence directory only after services stop. Every exact executed build/measurement command is also in raw artifacts.

For tests, build each selected test project with `DOTNET_LOCAL_DEV=1 DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1 dotnet build <project> -c Release -m:4 --disable-build-servers`. Run the `Fusion.Execution.Tests/bin/Release/net10.0/HotChocolate.Fusion.Execution.Tests` executable with `-class HotChocolate.Fusion.Execution.Results.FetchResultStoreTests -class HotChocolate.Fusion.Text.Json.CompositeResultDocumentTests`. Run the matching `Fusion.AspNetCore.Tests` executable with `-class` for `HotChocolate.Fusion.LookupTests`, `DeferTests`, `NullTests`, `RequireTests`, `InterfaceTests`, `InterfaceObjectTests`, `QueryErrorBehaviorTests`, `SourceSchemaErrorTests` and `SharedPathErrorPocketingTests`, all in the `HotChocolate.Fusion` namespace. Full paths and console logs are saved in the campaign directory.

Committed [numeric evidence](benchmarks/fusion-performance/results/summary.json) contains all primary and confirmation runs/ranges, paired arithmetic and order, all three BDN result sets, source identities, profile summaries and binary/counter/cleanup audit. Ignored local raw [baseline](/workspaces/platform-1/perf/list-completion-baseline-9a563e814f), [candidate](/workspaces/platform-1/perf/list-completion-candidate1-9a563e814f), and [campaign](/workspaces/platform-1/perf/list-completion-campaign-9a563e814f) preserve patches, DLL hashes, dependency manifests, schemas, responses, process samples, timestamps, k6 summaries, BDN distributions, counters, traces and build/test logs. Large raw artifacts are not committed.

The final audit checks the measured source fingerprint, every frozen DLL hash, project-reference gateway dependencies, identical requests/schema inputs, all fixed-mode counter samples, exited owned service processes and closed ports 5320–5324. All measurement monitors and micro workers have exited. The original workspace and user processes were preserved. No merge or PR is part of this investigation.
