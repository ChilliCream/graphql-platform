Current disposition: **C2 abandoned for unresolved ordinary execution cost after the bounded BenchmarkDotNet gate.** See [BDN-RESULTS.md](BDN-RESULTS.md). Prior calibrated allocation evidence remains valid and separately labeled.

# C2: defer Apollo error-attribution variables

Status: QUEUED_FOR_HTTP. The production-path microbenchmark supports a scoped allocation reduction. It does not establish a general timing improvement or HTTP performance benefit. The [independent complete-campaign review](https://github.com/ChilliCream/graphql-platform/tree/e699a4aca28684db7b84fbd6085414b7ee28a59d/performance/fusion-apollo-scratch-20261017b/c2-micro-review) agrees with promotion of the allocation hypothesis. No implementation PR exists yet.

The first shared HTTP baseline retained valid original/entity phases, then stopped on ordinary fixed-rate drops before any candidate ran. [Recorded-event diagnosis](HTTP-ORDINARY-DIAGNOSIS.md) supports correcting the supplemental generator's VU reserve and timer-based count accounting. All failed and valid observations remain published; the corrected continuation has not run in this revision.

## Source and recurring work

Platform base: `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`. Benchmark revision: `941599e4e27719084189e12d737e147069fdf794`. Fresh Apollo fidelity, original workload, seven real source HTTP fetches, thirteen-node execution plan, raw timing samples and warm profiles are preserved in [baseline evidence](https://github.com/ChilliCream/graphql-platform/tree/ef9fc1159a5a7a980754ebd42b511030320482cd/experiments/fusion-apollo-baseline-20261017b).

Apollo single and batch execution eagerly converted every representation into an immutable `VariableValues[]` used only for error attribution. The candidate materializes that array when transport, result storage or deferred merge errors require it. `PendingMerge` already retains the representation. Public `RepresentationValue.ToVariableValues` and native/composite variable behavior are unchanged. The patch is limited to `ApolloOperationExecutionNode`, `ApolloOperationBatchExecutionNode` and `PendingMerge`.

The falsifiable allocation hypothesis is one array of `24 + 40 × distinct representations` bytes per successful representation operation on this runtime. The observed original plan has eleven such operations and twenty-four distinct representation values, giving a source-shape estimate of 1,224 B/request. This is not an exact measured HTTP-total allocation saving. Warm profile callers and source-path analysis motivated the candidate before the list was frozen.

Ownership, cancellation, error attribution, batch fallback, null/missing entities and native behavior are the correctness risks. No new retained pool or public API is introduced. The candidate stops if scoped allocation validation fails without a diagnosed method correction, if repeated timing worsens against both adjacent controls, or if its required HTTP comparison remains adverse or inconclusive.

## Correctness and binary isolation

Seventeen new actual-executor snapshots cover single and batch dispatch, duplicate keys, success, transport failure, null/missing/protocol responses, source errors, deferred merge errors, cancellation and partial batch fallback. Known three-item lists have truthful `@listSize(assumedSize: 3)` metadata; default security remains enabled. Dispatch assertions prevent pre-execution cost failures from passing as Apollo coverage. Initial rejected cost-only snapshots are preserved under `results/rejected-preexecution-snapshots`.

The baseline generated the accepted snapshots. Candidate results matched; the targeted candidate run passed 83 tests, including the seventeen new cases and existing representation/native cancellation contracts. Build/test logs are retained. The corrected harness also validates ten complete response/dispatch oracles in both arms under both default and controlled allocation policies before any measurement.

The microbenchmark builds the actual gateway executor with composed Apollo schemas and a deterministic source-client seam. Each iteration awaits execution and response disposal. Providers are disposed between cases; normal completion seals production arenas. Query preparation, composition and complete response formatting are outside measured blocks. The fixture does not copy a representation conversion algorithm.

The established baseline execution DLL is `9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b`; candidate DLL is `244ad610bfe4ab6152885955e22776124510acb344f8c783a3f3a17260878301`. A common 99-file micro host is frozen twice, with only execution DLL/PDB differing. Earlier fresh builds changed unrelated metadata and file-local generated names. The retained assembly identity reports compare 6,318 normalized method bodies and document that control-build distinction. Those unmeasured freezes were superseded, not silently substituted.

Measured harness/source commit: `a1865092b4`. Runner SHA256: `c88c1a0b2e6be2af596b7daf3aa26d7056855d32cb41862f1be35a8e689a36e9`. Frozen manifest SHA256: `028c47e1b60ec83387cc5879eccf37c6b1e941186aa79776c93dfe5608c6c9e9`. Full per-file fingerprints and realized GC configurations are in `results/frozen-fixed-heaps/manifest.json` and `results/verify-fixed-heaps-commands.json`.

Environment: Linux ARM64, SDK `11.0.100-rc.1.26425.128`, Release `net10.0`, runtime `10.0.12`, server GC, roll-forward disabled. Target affinity is CPUs `2,3,10,11`; runner affinity is `0,8`. Production engines remain byte-identical throughout the method corrections.

## Retained method failures and correction

1. The initial thread-scoped allocation campaign stopped on the first control's `unique1` case because execution completed asynchronously. Ordinary observations remain diagnostic only. The thread counter could not account for all operation allocations. No candidate or timing measurement ran.
2. The corrected natural-async, process-scoped campaign completed one full default-policy allocation baseline, then stopped candidate `unique1` block 4 when three generation-0 collections ended the no-GC region. The successful baseline and every candidate partial block are retained. No timing measurement ran.
3. One unchanged candidate-only GC-reason probe reproduced the region failure in synchronous ordinary block 4. Its EventPipe stream is truncated; the readable prefix has explicit parse-error flags, and complete trace bounds/event-loss cannot be certified. Prefix events show two heaps after pre-region induced collections, followed by an `AllocSmall` background generation-2 collection and a generation-0 collection reporting four heaps. Runtime source prepares no-GC budgets for current heaps and initializes recommissioned heap budgets to zero. Adaptive heap growth is observed; the exact zero-budget internal trigger remains an inference. No profiled allocation number is accepted. The [independent runtime review and parser](https://github.com/ChilliCream/graphql-platform/tree/db0afc9dc15a98c7e34afd64a41e637c92d60031/performance/fusion-apollo-scratch-20261017b/c2-gc-review) records exact events and source references.

The final allocation-only policy explicitly sets `DOTNET_GCHeapCount=4`. This runtime disables dynamic adaptation when heap count is explicitly supplied. The harness asserts realized `HeapCount=4` and `GCDynamicAdaptationMode=0`. Default timing has no such override and asserts mode 1. This changed controlled allocation configuration receives its own planned controls; the earlier valid dynamic baseline is preserved separately.

Every controlled block uses a 512 MiB no-GC region, exact before/after calibration of 127 arrays of 4,096 bytes (523,240 allocated bytes), precise process allocation counters, completed-operation counts and zero-collection/end-region checks. Caller-thread counters are diagnostic only. Fifty-millisecond idle observations are retained as background context, never subtracted or used as rigorous uncertainty bounds.

The one-second warmup showed an ordinary-case transition before timing began. The final campaign predeclared three seconds per case in every arm. This is not proof of stationarity; all subsequent block trends remain visible.

## Completed bounded micro campaign

Exact order: controlled allocation `B/C2/B`, then default-policy timing `B/C2/B/C2/B`. Each allocation launch has ten cases × five blocks × 1,000 operations. Timing has ten cases × twelve blocks, with 5,000 operations per successful block and 500 per error/fallback block. There are no retries or discarded observations.

Actual campaign: `2026-10-17T16:40:18.269537Z` through `16:46:07.224370Z`, 348.955 seconds. All eight launches exited zero. All 150 allocation blocks passed exact calibration, no-GC and completion checks. All 600 timing blocks are retained. PID/start identities, per-mode environment, commands, source hashes, start/end times and cleanup are in `results/micro-campaign-fixed-heaps/launches.json`. Independent verification found every owned process gone and the lock free at `16:46:23.238192Z`.

The table reports arithmetic means of every retained observation: three baseline and two candidate timing launches; two baseline and one candidate allocation launch. The table does not replace the individual launch comparisons in `analysis.json`. Timing and allocation use their separate GC policies described above.

| Method | Version | Mean | Ratio | Allocated | Alloc Ratio |
|---|---|---:|---:|---:|---:|
| ordinary | `main` | 11.860 µs | 1.00 | 25,680.005 B | 1.00 |
| ordinary | C2 | 11.865 µs | 1.00 | 25,680.000 B | 1.00 |
| unique1 | `main` | 20.790 µs | 1.00 | 28,561.281 B | 1.00 |
| unique1 | C2 | 21.014 µs | 1.01 | 28,495.603 B | 1.00 |
| unique5 | `main` | 22.986 µs | 1.00 | 28,815.788 B | 1.00 |
| unique5 | C2 | 22.745 µs | 0.99 | 28,592.000 B | 0.99 |
| unique6 | `main` | 23.237 µs | 1.00 | 28,879.976 B | 1.00 |
| unique6 | C2 | 22.785 µs | 0.98 | 28,615.742 B | 0.99 |
| unique9 | `main` | 26.389 µs | 1.00 | 29,072.000 B | 1.00 |
| unique9 | C2 | 25.324 µs | 0.96 | 28,687.648 B | 0.99 |
| duplicate96-to1 | `main` | 56.495 µs | 1.00 | 29,344.000 B | 1.00 |
| duplicate96-to1 | C2 | 56.451 µs | 1.00 | 29,282.651 B | 1.00 |
| mixed96-to6 | `main` | 57.623 µs | 1.00 | 29,624.000 B | 1.00 |
| mixed96-to6 | C2 | 56.344 µs | 0.98 | 29,360.000 B | 0.99 |
| batch9 | `main` | 35.721 µs | 1.00 | 33,520.000 B | 1.00 |
| batch9 | C2 | 35.921 µs | 1.01 | 32,752.000 B | 0.98 |
| transport9 | `main` | 34.018 µs | 1.00 | 32,655.883 B | 1.00 |
| transport9 | C2 | 32.856 µs | 0.97 | 32,656.000 B | 1.00 |
| batch-fallback9 | `main` | 35.857 µs | 1.00 | 36,199.976 B | 1.00 |
| batch-fallback9 | C2 | 35.530 µs | 0.99 | 35,816.000 B | 0.99 |

Allocation medians match the hypothesized array sizes against both controls. Every block in the mixed-six and two-operation batch cases gives respective differences of 264 B and 768 B. Other full-operation totals have small negative residuals relative to their modal values and positive background variation, including -1.632 B/op and +13.256 B/op. All values remain in the arithmetic means. Calibration does not make every target block an exact array-only measurement. The ordinary and transport-error controls have unchanged modal allocation.

No candidate timing mean is more than 5% worse than both adjacent controls in either candidate launch. Ordinary ratios are 1.0148/0.9924 and 0.9984/0.9914 against preceding/following controls. Conflicting controls remain material: first `unique1` is +8.64% versus its preceding control and -7.53% versus its following control. The second candidate `unique6` ends with a last-three/first-three block ratio of 1.199. These trends and varying default-GC counts prevent a generic timing-speedup claim. The micro gate supports proceeding with an allocation-only hypothesis to HTTP validation.

## Reproduction and HTTP handoff

Source/build prerequisites are the pinned platform, SDK/runtime and baseline fixture above. Preserve historical outputs; each runner refuses an existing campaign directory. Shared builds and offline analysis use the common lock. Actual measurement additionally requires the orchestrator's exclusive grant.

```sh
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock dotnet build experiments/fusion-apollo-error-vars-20261017b/Micro/Micro.csproj -c Release --disable-build-servers
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock python3 experiments/fusion-apollo-error-vars-20261017b/freeze_micro.py
flock -x /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 experiments/fusion-apollo-error-vars-20261017b/run_micro.py
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock python3 experiments/fusion-apollo-error-vars-20261017b/analyze_micro.py experiments/fusion-apollo-error-vars-20261017b/results/micro-campaign-fixed-heaps
```

The actual final harness-only build used `--no-restore -p:BuildProjectReferences=false` to retain the already measured engine identities. `freeze_micro.py` starts with the previous common dependency set and changes only the rebuilt harness plus recorded engine overlays; its manifest rejects unexplained arm differences. Source/build-path metadata can change a fresh binary fingerprint, so newly rebuilt outputs require identity inspection before being treated as these frozen binaries.

`freeze_http.py` copies the established 106-file baseline gateway and overlays the same measured candidate engine DLL/PDB. Every other host, dependency, FAR and runtime/config file is baseline-identical. Candidate PDB SHA256: `818af50f43ba3fe21d4f27ae575b7782d7f6742fe6bc1010db398375f159ff5a`. The full HTTP freeze manifest is in `results/frozen-http/manifest.json`. It applies no allocation-only GC override.

The shared HTTP owner will use the unchanged original TestQuery, focused entities9, supplemental ordinary 5,000 arrivals/s control, delayed original/burst and observed post-burst idle, with both adjacent baseline controls. Existing saturated ordinary discovery results do not replace this supplemental headroom check. No HTTP result is claimed in this publication.

All failed methods, partial observations, trace prefix/original trace, test logs, source, analysis and manifests are included on this experimental evidence branch. Build outputs and unrelated user files are excluded. The experimental production patch remains available for review; acceptance still requires the relevant HTTP regression gate.
