# C4 result: abandoned

C4 is abandoned after independent review and root acceptance. The four-batch sequence improved elapsed time and reduced allocations, but the common two-item batch showed a small repeated elapsed increase relative to earlier controls, and one ordinary launch had a substantial unexplained increase. The complete evidence does not establish a worthwhile net benefit without an unresolved cost. This is not a universal CPU-regression claim. No VU campaign, retry or implementation PR will follow.

Five native BenchmarkDotNet launches completed successfully between 2026-10-17 17:57:11.698799 and17:58:01.699847 UTC. All fifteen native MemoryDiagnoser records and180 actual observations passed fixed accounting. Separate allocation then completed baseline and stopped on the candidate final region at17:58:23.482356. Independent cleanup at17:58:41.256401 found all seven exact target identities absent and the common lock free.

## All native means and bytes

Each elapsed entry is the arithmetic mean of all twelve native Workload/Result observations, in microseconds per operation. No outliers were removed. Native allocated bytes are from the separate process-wide MemoryDiagnoser diagnostic iteration, not an exact scoped array total.

| Launch | Ordinary µs / B | Alias2 µs / B | Alias sequence10 µs / B |
|---|---:|---:|---:|
| 01-benchmark-B | 1.545 / 3,470 | 2.701 / 3,902 | 10.842 / 14,628 |
| 02-benchmark-C4 | 1.523 / 3,470 | 2.777 / 3,742 | 10.567 / 219,926 |
| 03-benchmark-B | 1.508 / 3,470 | 2.701 / 3,902 | 11.161 / 14,624 |
| 04-benchmark-C4 | 2.473 / 3,470 | 2.772 / 3,742 | 10.739 / 13,840 |
| 05-benchmark-B | 1.592 / 3,470 | 2.782 / 3,896 | 11.083 / 14,628 |

| Candidate case / launch | Elapsed vs earlier / later control | Native bytes saved vs earlier / later |
|---|---:|---:|
| alias-sequence10 / 02-benchmark-C4 | -2.53% / -5.32% | -205298 / -205302 |
| alias2 / 02-benchmark-C4 | +2.83% / +2.82% | 160 / 160 |
| ordinary / 02-benchmark-C4 | -1.44% / +1.03% | 0 / 0 |
| alias-sequence10 / 04-benchmark-C4 | -3.78% / -3.10% | 784 / 788 |
| alias2 / 04-benchmark-C4 | +2.64% / -0.35% | 160 / 154 |
| ordinary / 04-benchmark-C4 | +64.03% / +55.34% | 0 / 0 |

## Controlled corroboration and failure

The baseline completed all nine controlled regions. Candidate ordinary and alias2 completed all three valid regions; sequence10 completed two valid regions. The final candidate region had process calibration527,896B versus expected523,240B, a4,656B excess, while thread calibration remained exact. It had zero collections and successful EndNoGCRegion, but the declared process-calibration guard correctly failed. The final allocation baseline was never launched. No failed region is relabeled valid.

| Shape | Baseline valid process mean B/op | Candidate valid process mean B/op | Counts B / C4 |
|---|---:|---:|---:|
| ordinary | 3468.120 | 3464.000 | 3 / 3 |
| alias2 | 3873.552 | 3712.000 | 3 / 3 |
| alias-sequence10 | 14528.000 | 13746.496 | 3 / 2 |

All recorded operations in this fixture completed synchronously on the same thread. Thread byte observations are preserved as corroborative data; process means above retain complete valid blocks including observed background variation, without subtracting idle samples. Modal scoped differences are160B for alias2 and784B for the sequence. Initial source estimates152/736B were preliminary and did not predict the measured whole-path delta; they are not validated upper bounds.

## Interpretation and limits

The sequence improves2.535–5.316% against every adjacent timing control. Its second native allocation value and valid controlled regions support a real allocation reduction. Its first native value219,926B/op is inconsistent with the other evidence and remains published with unknown cause.

Alias2 first costs2.829/2.820% against both controls; the second costs2.636% against the earlier control and improves0.351% against the later. The absolute increase over the earlier controls is about71–76ns per operation. Ordinary second mean2.473µs exceeds adjacent1.508/1.592µs; two large blocks and a broader shift are visible in the twelve retained observations. The source path is intended to be unaffected, but no observed cause explains this adverse result. Neither it nor native-memory anomalies are attributed to GC or the patch without evidence.

Native MemoryDiagnoser in BenchmarkDotNet0.15.8 uses precise process-wide allocation deltas on this runtime, after a forced collection and over one extra1,000-operation diagnostic iteration. Async/current-thread omissions are not an explanation here. Native rounded bytes and collection counts, raw //GC totals, all Workload/Actual/Result and overhead measurements are retained. Native default-GC elapsed does not measure process CPU. Controlled four-heap NoGC allocation is separate and cannot be presented as default total bytes or RSS.

The unchanged fixture uses the actual sealed default HTTP client and HttpSourceSchemaClient through a controlled in-memory handler, full request serialization/content consumption, response splitting and complete arena/result disposal. It measures no network capacity. Fixed short iterations trigger BDN minimum-duration warnings; these remain in raw logs. All frozen identities, exact commands and checks are in BDN-READY.json, BDN-METHOD.md and results/bdn-preparation. The earlier erroneous verify-fixed preparation attempt remains separately retained and excluded.

Reproduce analysis with analyze_bdn.py against results/bdn-campaign. The independent review is included at results/bdn-campaign/independent-execution-hotpaths-review.txt.
