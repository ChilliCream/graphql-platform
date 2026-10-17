C12 actual HTTP transport microbenchmark

The four cases are synchronous default ordinary, asynchronous default ordinary, asynchronous default alias2, and asynchronous custom-client ordinary. Setup gates the HTTP handler until the operation is observed incomplete, then releases it and checks request variables plus complete results/disposal. Measured asynchronous handlers use Task.Yield; the handler suspends, but its caller can sometimes observe completion, so every operation is not assumed to allocate every async state box.

All cases consume request content with CopyToAsync(Stream.Null), await complete result enumeration, dispose results, seal the memory arena, and return context ownership. The custom wrapper deliberately calls the unchanged public Task API.

Native BenchmarkDotNet 0.15.8 MemoryDiagnoser and elapsed timing run first under default .NET 10.0.12 server GC adaptation, with 3 seconds fixture warmup, 3 warmup iterations, and 12 retained actual observations of 1000 awaited operations. Sequence B/C12/B/C12/B. Native MemoryDiagnoser has its own 1000-operation diagnostic invocation; totals must be interpreted with corroboration and anomalous values retained.

Separate calibrated process allocation follows B/C12/B under four heaps and disabled dynamic adaptation, using three 1000-operation NoGC regions per case. Exact before/after calibration, completed-operation counts, all generation counters and region termination are required. Invalid observations fail-stop, without retry. Thread-local byte differences are not valid allocation totals when continuations migrate.

The draft campaign deadline is 285 seconds plus at most 10 seconds owned-process cleanup; child limit is 60 seconds. Only benchmark owner/root may grant execution. Elapsed time is not measured process CPU. No network or end-to-end claim is made by these microbenchmarks.
