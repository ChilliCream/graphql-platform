C7/C10 shared response-reader micro results

Decision: both candidates passed independent review and root accepted PASS_FOR_VU. No HTTP or process CPU result is claimed. The independent review and extraction are retained in results/independent-review.

One serialized campaign completed all 11 processes in 102.5 seconds. Four separately calibrated allocation processes preceded seven native BenchmarkDotNet processes with MemoryDiagnoser. All runtime, binary, operation-count, no-GC, exact calibration, native output and cleanup guards passed. No retries or removed observations.

Elapsed means in nanoseconds per complete response operation:

| Arm | known128 | unknown4096 | unknown32768 |
| --- | ---: | ---: | ---: |
| 05-benchmark-B | 753.143 | 1209.322 | 3297.711 |
| 06-benchmark-C7 | 728.625 | 841.581 | 2367.495 |
| 07-benchmark-C10 | 744.886 | 935.613 | 3021.974 |
| 08-benchmark-B | 752.021 | 999.273 | 3258.409 |
| 09-benchmark-C10 | 783.153 | 928.279 | 3023.720 |
| 10-benchmark-C7 | 756.166 | 845.418 | 2312.362 |
| 11-benchmark-B | 749.959 | 988.419 | 3326.704 |

Native MemoryDiagnoser allocated bytes per operation (raw JSON values):

| Arm | known128 | unknown4096 | unknown32768 |
| --- | ---: | ---: | ---: |
| 05-benchmark-B | 1192 | 1281 | 1427 |
| 06-benchmark-C7 | 1192 | 955 | 1099 |
| 07-benchmark-C10 | 1192 | 1139 | 1139 |
| 08-benchmark-B | 1192 | 1280 | 1427 |
| 09-benchmark-C10 | 1192 | 1139 | 1139 |
| 10-benchmark-C7 | 1192 | 952 | 1099 |
| 11-benchmark-B | 1192 | 1280 | 1427 |

All native Gen0/Gen1/Gen2 raw collection counts are zero and TotalOperations is 1000 for every case. This describes the extra diagnostic workload, not the entire process lifetime.

Separately calibrated scoped allocation bytes per operation (all five blocks identical in each cell):

| Arm | known128 | unknown4096 | unknown32768 |
| --- | ---: | ---: | ---: |
| 01-allocate-B | 1192.0 | 1280.0 | 1424.0 |
| 02-allocate-C7 | 1192.0 | 952.0 | 1096.0 |
| 03-allocate-C10 | 1192.0 | 1136.0 | 1136.0 |
| 04-allocate-B | 1192.0 | 1280.0 | 1424.0 |

Exact scoped savings are 328 B per affected response for C7, and 144 B/288 B at 4 KB/32 KB for C10. The known-length 128 B case is unchanged. Native MemoryDiagnoser corroborates these deltas within 0-3 B per-operation diagnostic overhead.

Every affected candidate mean is lower than both adjacent baseline means in both rounds. C7 improves 14.5-30.5%; C10 improves 6.1-22.6% at 4 KB and 7.2-9.1% at 32 KB. These are elapsed reductions, not CPU measurements.

The known128 control is noisy: C10 second-round mean is 4.1-4.4% higher, while its first-round mean is about 1% lower. The 4 KB first baseline is slower than the later baselines. Both caveats remain in the full raw distribution and all adjacent-control comparisons; no rerun or trimming was used.

The fixture uses the actual production reader, fully consumes and disposes results, and seals/disposes each arena. Memory-backed streams complete synchronously. Applicable real HTTP CPU, asynchronous read cost, tails and workload coverage remain acceptance gates. C10 benefit depends on physical arena chunk adjacency; its fallback preserves noncontiguous chunks.

Exact MemoryDiagnoser scope is documented at https://github.com/ChilliCream/graphql-platform/blob/1ef3921f5754001f2a88af30be361a381acc400a/evidence/fusion-apollo-20261017/method-review/memory-diagnoser-scope.txt . Native timing/diagnostic runs use default adaptive server GC; the separately labelled scoped allocation runs use fixed four heaps and guarded 512 MiB no-GC regions with zero collections and exact 523240 B before/after calibrations.

Raw logs, native reports, all measurements, runtime identities, launch records and independent cleanup are in results/bdn-shared-campaign. Machine-readable comparisons and untouched measurement arrays are in results/micro-summary.json. Frozen inputs and source identities are recorded in READY.txt and results/final-preparation.
