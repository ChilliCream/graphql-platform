C9 bounded BenchmarkDotNet gate

CPU-only hypothesis: construct compact path segments while walking parent rows upward. No allocation reduction is predicted. The warm original-query profile attributes 1.706% exclusive sampled thread time to CreateCompactPath; this is workload context, not a promised improvement. Ordinary is an unchanged control; batch9 and duplicate96-to1 create depth-two representation paths through the changed production method.

The actual executor fixture is copied from a59c7ac49408e02bd60e53704ae1f7d37a5af6db and changes only native [MemoryDiagnoser] plus an assertion that the attribute is present. The host is compiled against the established baseline binaries without rebuilding either engine. Both frozen arms have identical files except Execution DLL/PDB. All three payload/dispatch check-only oracles match. Runtime is .NET 10.0.12, default server GC adaptation mode1, no allocation-only heap override.

Five fresh outer launches: B, C9, B, C9, B. Each has ordinary, batch9 and duplicate96-to1; each case uses three seconds prewarm, three BDN warmup iterations, twelve actual iterations of 1,000 fully awaited executor operations including result disposal. InProcessEmit, no outlier removal, WithGcForce(false) for timing. Native MemoryDiagnoser has its own diagnostic work and GC measurement policy; retain every native allocation and Gen0/Gen1/Gen2 result per case. Timing is elapsed time, not process CPU. Fixed iterations below BDN recommended duration retain native warnings.

Expected approximately 60 seconds, hard bound 180 seconds. Runner enforces 60 seconds per child, 165 seconds total plus at most 10 seconds verified PID/start process-group cleanup. Strict frozen hashes, default runtime policy, exact three-case/12-observation/1,000-operation checks, and complete nonnegative native MemoryDiagnoser fields stop on failure without retry. Preserve all actual/result/overhead and memory/GC records. Compare arithmetic means to both adjacent controls and the full control range, including time trends.

A worthwhile repeatable improvement is required for this CPU-only candidate. Absence of a regression does not pass; unclear benefit or meaningful ordinary/fallback cost stops the candidate. No equivalent rerun or VU follows an inconclusive gate.

After explicit root grant and free-lock/load preflight:

```sh
flock -x /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 /workspaces/platform-1/.worktrees/fusion-apollo-compact-path-build-20261017b/performance/fusion-apollo-compact-path-build-20261017b/bdn/run.py
```

The [exact-version MemoryDiagnoser source audit](https://github.com/ChilliCream/graphql-platform/blob/1ef3921f5754001f2a88af30be361a381acc400a/evidence/fusion-apollo-20261017/method-review/memory-diagnoser-scope.txt) confirms process-wide managed allocation, including async continuations. The separate diagnostic denominator is 1,000 operations, background allocations are included, and per-operation JSON bytes are rounded. Native timing observations remain elapsed time.
