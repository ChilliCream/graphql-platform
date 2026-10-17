# C13 compact incoming alias-root scratch microbenchmark result

Verdict: ABANDON. No rerun, VU validation or pull request is warranted.

The unchanged actual-production C4 client fixture measured independent baseline and C13 engines. The native stage completed all seven baseline/C11/C13 launches with MemoryDiagnoser. This independent review covers the five baseline/C13 launches, all 15 case distributions and all 180 untrimmed Workload/Result observations. Means below are native BenchmarkDotNet Statistics.Mean (overhead adjusted); elapsed time is not process CPU.

| Launch | Case | Mean ns | Median ns | SD ns | Native allocated B/op |
|---|---|---:|---:|---:|---:|
| 01-benchmark-B | Shape=alias-sequence10 | 10336.811 | 10216.383 | 472.675 | 14628 |
| 01-benchmark-B | Shape=alias2 | 2628.359 | 2653.206 | 105.243 | 3896 |
| 01-benchmark-B | Shape=ordinary | 1491.685 | 1499.282 | 89.882 | 3470 |
| 03-benchmark-C13 | Shape=alias-sequence10 | 10444.130 | 10395.779 | 239.473 | 14372 |
| 03-benchmark-C13 | Shape=alias2 | 2723.675 | 2746.769 | 60.354 | 3840 |
| 03-benchmark-C13 | Shape=ordinary | 1466.830 | 1501.386 | 93.605 | 3470 |
| 04-benchmark-B | Shape=alias-sequence10 | 10576.662 | 10458.884 | 323.534 | 14624 |
| 04-benchmark-B | Shape=alias2 | 2630.692 | 2648.143 | 74.818 | 3902 |
| 04-benchmark-B | Shape=ordinary | 1795.207 | 1662.595 | 296.345 | 3470 |
| 05-benchmark-C13 | Shape=alias-sequence10 | 12161.166 | 12664.878 | 970.655 | 14372 |
| 05-benchmark-C13 | Shape=alias2 | 2850.040 | 2909.645 | 169.394 | 3846 |
| 05-benchmark-C13 | Shape=ordinary | 1514.757 | 1553.093 | 99.950 | 3470 |
| 07-benchmark-B | Shape=alias-sequence10 | 10558.951 | 10513.885 | 256.320 | 14624 |
| 07-benchmark-B | Shape=alias2 | 2753.332 | 2772.061 | 62.106 | 3902 |
| 07-benchmark-B | Shape=ordinary | 1493.517 | 1446.301 | 94.719 | 3470 |

Both alias2 arms are at least 3.5% slower than both adjacent controls. Sequence10 is approximately neutral in the first arm and about 15% slower in the second. The second sequence arm rises during its 12 observations; the middle ordinary control is also unstable. All observations remain in the evidence. These data do not establish a CPU cause, but they do not establish the neutral timing needed to accept the small allocation savings.

Native MemoryDiagnoser repeatedly reports 252–256 B less per sequence10 operation and 50–62 B less per alias2 operation against adjacent controls. Ordinary allocation remains 3470 B. Native GC counts are retained in the JSON; the C13 sequence diagnostic reports Gen1/Gen2 collections, so no zero-GC claim applies.

The supplemental allocation stage stopped at a C11 calibration failure before C13 launched. C13 has no separately calibrated supplemental result. The native allocation evidence remains recorded, without replacing the missing corroboration or rerunning the failed stage.

Scope: alias2 is one two-item split; sequence10 is four separate splits of 2, 3, 2 and 3 items. Ordinary bypasses the changed splitter. The 256/257/1024 cases establish correctness only; no timing claim is made for the larger managed-array fallback.

Source and focused tests: commit 8291f82de4f371e913d6fdd443ceec262ed18105. Engine SHA-256 f06a867f5fbfa552edd2904691c74ea818b23635c537272aeec5cb2f4f254b14. Full joint raw artifacts are preserved at perf/fusion-apollo-shared-client-20261017b/bdn-campaign and are published by the joint campaign owner. Independent cleanup passed at 2026-10-17 18:03:58.799 UTC.

The exact native MemoryDiagnoser scope is documented at evidence/fusion-apollo-20261017/method-review/memory-diagnoser-scope.txt in commit 1ef3921f5754001f2a88af30be361a381acc400a. Native values and all adjacent comparisons are retained in results/independent-native-review.json.
