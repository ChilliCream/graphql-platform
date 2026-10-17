# C15 private alias-result scratch microbenchmark result

Verdict: ABANDON, accepted by the orchestrator after independent review. No rerun or VU promotion.

The exact existing C12 actual-client fixture measured genuinely asynchronous two-item alias responses using independent baseline, C14 and C15 engines. Its setup proves suspension with a TCS gate; measured sends use Task.Yield followed by natural complete result enumeration and disposal. The unchanged ordinary, async-ordinary and async-custom cases are controls.

This independent review covers all five baseline/C15 launches, 20 case distributions and 240 untrimmed Workload/Result observations. Means are native BenchmarkDotNet Statistics.Mean, with overhead adjustment. Elapsed time is not process CPU.

| Launch | Case | Mean ns | Median ns | SD ns | Native allocated B/op |
|---|---|---:|---:|---:|---:|
| 01-benchmark-B | Shape=async-alias2 | 5186.345 | 5242.933 | 893.489 | 4536 |
| 01-benchmark-B | Shape=async-custom | 4000.388 | 4011.131 | 130.803 | 4030 |
| 01-benchmark-B | Shape=async-ordinary | 4594.163 | 4621.115 | 991.345 | 4024 |
| 01-benchmark-B | Shape=ordinary | 1591.470 | 1562.490 | 88.724 | 3464 |
| 03-benchmark-C15 | Shape=async-alias2 | 5383.359 | 5441.724 | 138.462 | 4496 |
| 03-benchmark-C15 | Shape=async-custom | 3808.120 | 3843.901 | 89.672 | 4030 |
| 03-benchmark-C15 | Shape=async-ordinary | 3811.968 | 3825.526 | 82.354 | 4024 |
| 03-benchmark-C15 | Shape=ordinary | 1680.332 | 1601.199 | 210.375 | 3464 |
| 04-benchmark-B | Shape=async-alias2 | 5136.018 | 5074.557 | 262.198 | 4536 |
| 04-benchmark-B | Shape=async-custom | 3730.321 | 3722.025 | 136.617 | 4024 |
| 04-benchmark-B | Shape=async-ordinary | 2868.644 | 2599.247 | 748.799 | 4030 |
| 04-benchmark-B | Shape=ordinary | 1621.071 | 1581.324 | 86.930 | 3470 |
| 05-benchmark-C15 | Shape=async-alias2 | 5393.294 | 5605.184 | 978.564 | 4500 |
| 05-benchmark-C15 | Shape=async-custom | 3681.967 | 3322.211 | 1425.248 | 4024 |
| 05-benchmark-C15 | Shape=async-ordinary | 3440.628 | 3607.213 | 549.340 | 4030 |
| 05-benchmark-C15 | Shape=ordinary | 1603.588 | 1608.220 | 93.368 | 3464 |
| 07-benchmark-B | Shape=async-alias2 | 5318.238 | 5218.599 | 327.999 | 4541 |
| 07-benchmark-B | Shape=async-custom | 3894.961 | 3992.424 | 695.741 | 4023 |
| 07-benchmark-B | Shape=async-ordinary | 3858.634 | 3826.943 | 182.781 | 4030 |
| 07-benchmark-B | Shape=ordinary | 1517.941 | 1482.073 | 64.862 | 3470 |

The affected async-alias2 mean is higher in all four adjacent comparisons: +3.80%, +4.82%, +5.01% and +1.41%, for 36–41 B/op of native allocation savings. The second affected arm has substantial within-launch variation, and unchanged controls also drift. All observations remain in the evidence. These data do not establish a causal CPU regression, but do not establish neutral timing for this small allocation benefit.

The native stage completed all seven launches. The supplemental stage completed baseline/C14 and six unaffected C15 regions, then its first affected C15 region failed the process calibration: 525544 B observed versus 523240 B expected (+2304 B). The current-thread calibration was exact, all 1000 operations completed, collection counts were zero and EndNoGCRegion succeeded. The source of the process excess is unknown. That region is invalid, no affected C15 supplemental result is claimed, and the final supplemental baseline never launched. The rejection is based on unresolved allocation-versus-time cost in the completed native gate, not on treating the calibration failure as a regression.

Scope remains the measured two-item asynchronous path. Neither a fixed thread-migration rate nor delayed consumer behavior is forced; retained scratch is capped at 152 B per participating thread, not globally. Native savings must not be multiplied into an actual original-query HTTP saving. The 16/17 boundary and lifetime tests establish correctness only.

Source/tests/reviews are at commit 5cff406be131b16e20c013ab79b89d988e9eb2cd; engine SHA-256 6a81410c3e046d87951316f28aa38ef4f43ed1268e06e052665b2b7c1e548f0b. Raw joint artifacts remain under perf/fusion-apollo-shared-variable-slots-20261017b/bdn-campaign and are published by the joint campaign owner. Independent cleanup passed 2026-10-17 18:17:13.897048 UTC.

The exact native MemoryDiagnoser scope is documented at evidence/fusion-apollo-20261017/method-review/memory-diagnoser-scope.txt in commit 1ef3921f5754001f2a88af30be361a381acc400a. All native records, samples, adjacent comparisons and original-report hashes are retained in results/independent-native-review.json.
