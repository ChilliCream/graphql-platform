# C5 terminal outcome

C5 is abandoned. The CPU-only hypothesis did not produce a repeated worthwhile affected-case improvement, and ordinary execution cost increased. No allocation saving is predicted. No further BenchmarkDotNet run, VU test, or production PR is planned.

The five fresh B/C5/B/C5/B launches completed from 17:27:44.647514 to 17:28:41.135653 UTC on 2026-10-17, taking 56.488 seconds. All 180 native WorkloadActual and 180 WorkloadResult observations are retained; all exits were zero. Check-only data/dispatch, runtime, engine identity, and fixed sample-count guards passed. No observation was removed or rerun.

Arithmetic mean elapsed time per fully awaited executor operation, microseconds:

| Launch | Arm | Ordinary | Batch9 | Duplicate96-to1 |
|---|---|---:|---:|---:|
| 01 | B | 11.5272 | 36.8814 | 55.8326 |
| 02 | C5 | 13.8692 | 37.0845 | 55.1225 |
| 03 | B | 11.8936 | 34.9666 | 58.0799 |
| 04 | C5 | 12.3297 | 34.8502 | 56.1766 |
| 05 | B | 11.8792 | 34.8573 | 55.7391 |

Duplicate96-to1 changed by -1.27%/-5.09% against the first surrounding controls and -3.28%/+0.78% against the second. Its first absolute reduction was 0.710/2.957 microseconds, while the second was 1.903 microseconds faster than the middle control and 0.438 microseconds slower than the final control. The improvement does not repeat against both controls. Batch9 changed by +0.55%/+6.06%, then -0.33%/-0.02%.

Ordinary changed by +20.32%/+16.61%, then +3.67%/+3.79%. The first candidate ordinary block trend decreases, with last-three/first-three means 0.9198; the second is 1.0057. This does not establish an acceptable tradeoff. The result is insufficient benefit proof, not a universal process-CPU regression claim.

The actual production route resolves 96 depth-two product-list targets in duplicate96-to1 and 18 targets in batch9. Ordinary root merge skips the changed GetStartResult traversal. The original warmed profile attributed 1.489% exclusive thread-time samples to GetStartResult; that is a workload-specific context for the small CPU hypothesis, not an effect estimate. BDN measures elapsed time, not process CPU.

All observations, native confidence intervals, raw overhead/actual/result samples, and block trends are in results/analysis.json and the unchanged BDN exports. Fixed 1,000-operation iterations are below BDN recommended 100 ms duration; warnings remain visible. Default GC and the exact common dependency tree were retained, with only the C5 Execution DLL/PDB overlay. No memory-diagnoser or allocated-byte claim is made.

Owner cleanup verified all five exact PID/start identities absent and lock free at 17:28:46.056622 UTC; independent cleanup confirmed this at 17:29:02.912226 UTC. All start/end/exit and cleanup metadata are complete.

Reproduce analysis with `python3 analyze.py results`. Preparation/source/engine evidence is immutable at e76d05b1b4b4bb379bda1e349a990cc1ef6a7d5b and aaf7e69ddb556a74ca43c20d2ff2342113954ee5.
