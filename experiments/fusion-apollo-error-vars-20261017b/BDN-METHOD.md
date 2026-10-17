# Bounded BenchmarkDotNet elapsed-time gate

BenchmarkDotNet 0.15.8 runs the existing real Apollo executor fixture for ordinary, batch9, and duplicate96-to1. Each invocation fully awaits 1,000 executions and response disposals. Each case verifies complete data and actual single/batch dispatch before three seconds of prewarm. The provider is disposed after the case.

The fixed job uses InProcessEmit in a fresh outer process, three warmup iterations, twelve measurement iterations, invocation count one, unroll factor one, no outlier removal, and no forced GC between timing iterations. Runtime and server GC remain .NET 10.0.12 defaults, with dynamic adaptation enabled. The schedule is B/C2/C3/B/C3/C2/B. Only the exact previously measured Execution DLL/PDB differ among the frozen dependency trees.

This is a BenchmarkDotNet elapsed-time gate. It does not measure process CPU or directly establish allocation savings. Separate calibrated process-allocation results remain the byte evidence: C2 batch9 saves 768 bytes, C2 duplicate96-to1 saves 64 bytes, and the C3 duplicate96-to1 production path saves 784 bytes. Ordinary is unaffected. The broader executor scope may contain other allocations.

MemoryDiagnoser is omitted. BenchmarkDotNet's extra-statistics path forces collections after iteration setup and clamps negative allocated-byte deltas. NoGC setup around that path would not validate the measurement on the investigated runtime. Existing controlled allocation observations and their failures remain published separately.

All twelve observations are retained and arithmetic means are compared against both applicable surrounding controls. A repeated candidate elapsed regression greater than five percent against both controls disqualifies the candidate; mixed results or broad drift do not establish a speedup. Existing HTTP process-CPU evidence remains a separate constraint on the allocation tradeoff. An execution, oracle, identity, sample-count, or deadline failure stops the campaign without retry.

The runner records owned PID/start identities and cleans only matching live process groups. Per-child deadline is 60 seconds, the campaign deadline is 285 seconds, and bounded cleanup adds at most ten seconds. The declared hard measurement budget is five minutes. No VU tests are included.

Preparation:

```sh
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock dotnet build experiments/fusion-apollo-error-vars-20261017b/Bdn/Bdn.csproj -c Release --no-restore -p:BuildProjectReferences=false --disable-build-servers
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock python3 experiments/fusion-apollo-error-vars-20261017b/freeze_bdn.py
```

After an explicit exclusive grant:

```sh
flock -x /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 /workspaces/platform-1/.worktrees/fusion-apollo-error-vars-20261017b/experiments/fusion-apollo-error-vars-20261017b/run_bdn.py
```

Check-only validation invokes each frozen assembly with `verify <output-directory>`, performs one correctness request per case, and omits prewarm and BDN execution.
