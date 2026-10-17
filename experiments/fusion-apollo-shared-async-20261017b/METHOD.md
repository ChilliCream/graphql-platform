# C6/C8 shared asynchronous executor gate

The real Apollo executor fixture uses synchronous ordinary, asynchronous single9, and asynchronous batch9. Before each case's warmup, the entity response is held until the execution event has a registered continuation and the overall request remains incomplete. The gate then releases the source and verifies complete data and actual single/batch dispatch. This proves the correctness operation exercised entity suspension and an execution wakeup. Measured source responses use Task.Yield; waiter registration on every measured operation is not asserted, so the C6 per-wakeup saving is not an exact per-request prediction.

All operations naturally await execution and response disposal. Each service provider is disposed after its case. Exact baseline, C6, and C8 engine DLL/PDB files are overlaid onto one common dependency tree. No production mechanisms are stacked. Default and fixed-policy check-only commands each perform one oracle request per case, without warmup or allocation loops.

Native BenchmarkDotNet 0.15.8 MemoryDiagnoser is enabled for every timing case. Its raw Gen0/Gen1/Gen2 collection counts, TotalOperations, and BytesAllocatedPerOperation are retained. Native allocated-byte results are compared with the separate calibrated allocation observations; zero, clamped, or inconsistent values are not accepted as allocation proof. BDN's extra-statistics path uses its own forced collections, outside the separate NoGC method. The output format is documented in the [pinned exporter source](https://raw.githubusercontent.com/dotnet/BenchmarkDotNet/1e8238ef6c08e18821edae83ac8d6b0a2bb1bb98/src/BenchmarkDotNet/Exporters/Json/JsonExporterBase.cs).

BDN timing uses .NET 10.0.12 default server GC, three seconds of case warmup, three fixed warmup iterations, and twelve actual iterations of 1,000 awaited operations. InProcessEmit runs inside fresh isolated outer processes. No outliers are removed. Elapsed time is not process CPU; no matching per-case process-CPU measurement is claimed. A new CPU measurement framework is outside this bounded gate.

Separate allocation launches set only DOTNET_GCHeapCount=4 and assert four heaps with adaptation disabled. Each case uses three regions of 1,000 operations, a 512 MiB NoGC budget, exact process/thread known-allocation calibration of 523,240 bytes before and after the measured operations, zero collection checks, complete-operation checks, and successful EndNoGCRegion. Raw 50 ms idle observations provide background context and are never subtracted or presented as rigorous uncertainty bounds. Operational and validation failures are preserved before stopping.

The fixed allocation sequence is B/C6/C8/B, followed by default-policy BDN B/C6/C8/B/C8/C6/B. Estimated total duration is two to three minutes. Work deadline is 285 seconds, each child is bounded to 60 seconds, and verified PID/start process-group cleanup adds at most ten seconds, within a five-minute hard budget. Any correctness, identity, accounting, or allocation-method failure stops without retry.

Positive allocation savings must agree with method-valid evidence, and meaningful repeated elapsed-cost increases on affected or ordinary cases prevent promotion. No generic speedup or CPU saving follows from this gate. A positive worthwhile result is required before any further VU test.

The first unmeasured freeze is preserved under `frozen-bdn`, with its manifest and supersession reason. The final `frozen-bdn-memory` includes the subsequently explicit user requirement for native MemoryDiagnoser. Neither freeze had been measured when this method was registered.

Exact execution after an explicit exclusive grant:

```sh
flock -x /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 /workspaces/platform-1/.worktrees/fusion-apollo-error-vars-20261017b/experiments/fusion-apollo-shared-async-20261017b/run.py
```
