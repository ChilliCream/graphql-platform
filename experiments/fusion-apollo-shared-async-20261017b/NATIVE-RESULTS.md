# C6/C8 native BenchmarkDotNet results

Seven strictly serial launches completed 2026-10-17 17:44:30.490105–17:45:46.429923 UTC. All seven exited zero, all 21 native MemoryDiagnoser records and 252 Workload/Actual observations passed fixed accounting. Independent cleanup at 17:46:48.410165 found every exact PID/start identity absent and the common lock free.

Each case uses the real executor, actual Apollo path, awaited response disposal and provider ownership. Default GC, three-second prewarm, three fixed warmup and twelve measured iterations of 1,000 operations, no outlier removal. The correctness handshake proves a registered signal waiter during an entity call; subsequent Task.Yield does not guarantee every operation registers a waiter before its producer resumes.

## Full native values

Elapsed values below are arithmetic means of all twelve native Workload/Result observations, in microseconds per operation. They are not process CPU. Bytes are native MemoryDiagnoser process-wide diagnostic values per operation, retained even when inconsistent.

| Launch | Ordinary µs / B | Async single9 µs / B | Async batch9 µs / B |
|---|---:|---:|---:|
| 01-benchmark-B | 11.946 / 25,703 | 23.323 / 30,423 | 28.640 / 34,870 |
| 02-benchmark-C6 | 12.164 / 477,980 | 25.085 / 30,352 | 29.767 / 34,805 |
| 03-benchmark-C8 | 11.800 / 25,703 | 23.383 / 30,236 | 30.354 / 34,680 |
| 04-benchmark-B | 14.126 / 25,703 | 23.530 / 30,416 | 29.831 / 34,870 |
| 05-benchmark-C8 | 12.197 / 479,137 | 22.348 / 149,688 | 29.157 / 34,672 |
| 06-benchmark-C6 | 12.036 / 145,202 | 23.741 / 30,327 | 29.448 / 34,770 |
| 07-benchmark-B | 12.298 / 25,703 | 22.288 / 30,416 | 29.004 / 34,864 |

## Both adjacent controls

Positive elapsed percentages mean more time. Positive bytes mean fewer native allocated bytes. No observation was removed.

| Candidate launch / case | Elapsed vs earlier / later | Bytes saved vs earlier / later |
|---|---:|---:|
| 02-benchmark-C6 / async-batch9 | +3.94% / -0.21% | 65 / 65 |
| 02-benchmark-C6 / async-single9 | +7.55% / +6.61% | 71 / 64 |
| 02-benchmark-C6 / ordinary | +1.83% / -13.89% | -452277 / -452277 |
| 03-benchmark-C8 / async-batch9 | +5.98% / +1.75% | 190 / 190 |
| 03-benchmark-C8 / async-single9 | +0.26% / -0.63% | 187 / 180 |
| 03-benchmark-C8 / ordinary | -1.22% / -16.47% | 0 / 0 |
| 05-benchmark-C8 / async-batch9 | -2.26% / +0.53% | 198 / 192 |
| 05-benchmark-C8 / async-single9 | -5.02% / +0.27% | -119272 / -119272 |
| 05-benchmark-C8 / ordinary | -13.66% / -0.82% | -453434 / -453434 |
| 06-benchmark-C6 / async-batch9 | -1.28% / +1.53% | 100 / 94 |
| 06-benchmark-C6 / async-single9 | +0.90% / +6.52% | 89 / 89 |
| 06-benchmark-C6 / ordinary | -14.80% / -2.13% | -119499 / -119499 |

## Memory method and limits

BenchmarkDotNet 0.15.8 source commit 1e8238ef6c08e18821edae83ac8d6b0a2bb1bb98 uses GC.GetTotalAllocatedBytes(precise: true) on this net8 library. Async thread migration is included. Its extra diagnostic iteration follows timing and forces full GC before initial statistics, even with GcForce(false). InvocationCount 1 × OperationsPerInvoke 1,000 gives the observed TotalOperations 1,000 denominator. Native JSON rounds bytes per operation; raw //GC totals remain in full logs. The engine clamps negative allocation deltas to zero. Native collection counts cover only that diagnostic interval.

Large native memory anomalies occur in unchanged ordinary paths and one C8 single case. All raw values remain published. Some coincide with generation 1/2 collections, but the cause is unknown; that co-occurrence is not causal evidence. No allocation reduction is inferred by dropping anomalous observations.

The separate controlled allocation attempt is preserved in ALLOCATION-ATTEMPT.md and results/allocation-attempt. Baseline nine regions passed. C6 ordinary three regions matched baseline exactly at 25,696 B/op; one valid single region measured 30,319.584 B/op versus baseline 30,415.088–30,415.552. The next C6 region failed an exact calibration guard by 4,656 process bytes and halted. No C8 controlled region or second allocation control ran. Native C8 batch bytes are repeatably lower, but complete calibrated corroboration is unavailable. No allocation retry is planned.

Fixed measurement iterations are shorter than BDN’s recommended 100 ms; native warnings, all block trends, overhead and statistics are retained in analysis.json. The seven launches provide two candidate observations and three controls; they do not establish universal CPU behavior.

## Disposition

Both candidates are ABANDONED after independent review and root acceptance. C6 has a material async-single elapsed increase for small allocation savings; the existing evidence does not establish a worthwhile net benefit. C8 has repeatable native batch savings of about 190–198 B/op, but variable affected timing and unexplained ordinary/single native memory totals leave complete net benefit unproven. Allocation-only gains would be acceptable; absence of a speedup is not the rejection reason. Neither disposition asserts a universal CPU regression. No HTTP/VU, benchmark retry, diagnostic retry or implementation PR will follow.
