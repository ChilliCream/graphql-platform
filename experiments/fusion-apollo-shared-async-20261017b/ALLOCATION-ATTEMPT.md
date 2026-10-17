# Preserved allocation-method failure

The exclusive campaign ran from 17:38:53.399735 to 17:39:11.621957 UTC on 2026-10-17. The baseline completed all nine controlled allocation regions. C6 completed three ordinary regions and the first asynchronous-single9 region, then stopped on the next region's calibration. C8 and the native BenchmarkDotNet stage were never invoked.

C6 asynchronous-single9 block 1 completed 1,000 operations, recorded zero collections, remained in NoGCRegion, and ended the region successfully. Its before-calibration current-thread allocation was exactly 523,240 bytes, while the precise process counter reported 527,896 bytes, an unexplained additional 4,656 bytes. The after-calibration counters were exact. The strict process-calibration guard rejected the observation and terminated the campaign; no retry or relaxed guard followed. This is an allocation-method validity failure, not evidence of a candidate performance regression.

All complete and partial observations remain in `results/allocation-attempt`, including the invalid region, runtime identities, exact PID/start metadata, and raw stdout/stderr. The C6 process exited with SIGABRT (recorded exit -6). Owner cleanup confirmed both target identities absent and lock free at 17:39:59.531926 UTC, and independent cleanup confirmed this at 17:40:18.077750 UTC.

A subsequent C12 shared-lock build temporarily prevented the first free-lock check. Its log timestamps and MSBuild durations place observed build execution after this exclusive campaign; exact lock-entry times are unavailable. The bounds are preserved separately without inventing overlap or attributing the calibration discrepancy to another process.

Only the previously planned, never-run native BenchmarkDotNet/MemoryDiagnoser stage is eligible for continuation under a new explicit grant. The host, engines, cases, and BDN settings stay unchanged. No supplementary allocation retry is planned. Native memory results must be compared with the available valid regions and otherwise marked uncorroborated; no completed C6 or C8 allocation comparison exists yet.
