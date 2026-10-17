# C5 bounded BenchmarkDotNet gate

This CPU-only hypothesis must show a clear worthwhile repeated elapsed reduction, not merely avoid regression. No allocation reduction is predicted. Ordinary is an unchanged control; duplicate96-to1 resolves 96 depth-two product-list targets through GetStartResult, while batch9 resolves 18. The original warmed profile attributes 1.489% exclusive thread-time samples to GetStartResult; this is a workload-specific upper-context estimate, not a promised CPU improvement.

The exact common actual-executor host from a59c7ac49408e02bd60e53704ae1f7d37a5af6db is reused without rebuilding. Only the baseline/C5 Execution DLL/PDB differ. All three check-only data/dispatch oracles passed with .NET10.0.12 serverGC and default adaptation. Existing 93 correctness tests cover root/fields/list chunks/references/null/unsupported traversal.

The fixed schedule is B/C5/B/C5/B. Each fresh outer process runs ordinary, batch9 and duplicate96-to1 with three-second prewarm, three BDN warmup iterations and twelve actual iterations of1,000 fully awaited real executor operations including disposal. InProcessEmit, defaultGC, noMemoryDiagnoser, nooutlierremoval. BDN elapsed is not processCPU. Full rawActual/Result/Overhead values will be retained and arithmeticmeans compared to both applicable controls.

Estimated measurement 57 seconds, hard budget 180 seconds. Runner allows 60 seconds per child and 165 seconds total plus at most 10 seconds of verified PID/start process-group cleanup. Any identity/oracle/count/timeoutfailure stopswithoutretry. An unclear benefit or adverse ordinary/fallback cost ends the candidate; no favorable-result retries or VU follow an inconclusive gate.

After explicit root exclusivegrant:

```sh
flock -x /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 /workspaces/platform-1/.worktrees/fusion-apollo-path-lookup-20261017b/performance/fusion-apollo-path-lookup-20261017b/bdn/run.py
```
