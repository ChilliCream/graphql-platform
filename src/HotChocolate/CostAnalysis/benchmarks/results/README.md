# Cost analysis performance gates

The nightly gate reads `gates.json`, the BenchmarkDotNet micro reports, and the same-host head-to-head endpoint report. The thresholds were frozen against revision `6c9945cc29fff6b1541f9c71acd11fbbac588f1f` after the cold-plan optimization landed.

| Gate | Frozen maximum |
| --- | ---: |
| Typical cached-operation evaluate p50 | 1 us |
| Typical cached-operation allocation | 0 B |
| Cold compile and evaluate relative to Rust on the same endpoint and host | 2.0x |
| Correlated-Boolean exact-boundary and fallback p50 | < 1 ms |

## CaseBudget calibration

The adversarial operation has two mutually exclusive type regions. A full exact compilation with `K` independent Boolean variables spends `2 * (2^K - 1)` splits across both regions.

The post-optimization calibration used the provisional budget of 4096. It recorded the following BenchmarkDotNet p50 values before changing the default:

| Independent variables | Splits for both regions | P50 | Result |
| ---: | ---: | ---: | --- |
| 4 | 30 | 38.191 us | full exact |
| 8 | 510 | 512.271 us | full exact |
| 9 | 1,022 | 1.027 ms | full exact, above the gate |
| 10 | 2,046 | 2.270 ms | full exact, above the gate |
| 11 | 4,094 | 5.240 ms | full exact, above the gate |
| 12 | 8,190 | 5.498 ms | fallback after the first region |

The default `CaseBudget` is 510. Eight variables remain fully exact with 48.8 percent p50 headroom, while nine variables exceed the budget and take the bounded fallback. The committed micro report is generated again with the 510 default, and the nightly gate checks both the eight-variable exact boundary and the nine-variable fallback.

The committed sweep retains 13 and 20 variables as fallback stress points for wider operations. Their topology requires more than 510 splits, so the benchmark setup verifies that both plans report `HitCaseBudget` before measuring them.

## Frozen same-host results

The 10-replicate campaign for the optimized engine, recorded with provenance revision `1b258c0001ff7376c0980ad951ee027f7545b228`, produced these process-median p50 values:

| Objects | Spreads | Hot Chocolate cold | Rust | Ratio | Hot Chocolate warm |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1,024 | 8 | 76.693 us | 43.262 us | 1.77x | 0.290 us |
| 10,240 | 8 | 452.580 us | 474.439 us | 0.95x | 0.293 us |
| 1,024 | 80 | 819.234 us | 433.743 us | 1.89x | 2.332 us |

The typical-operation micro benchmark measured a 93.314 ns warm-evaluate p50 with zero allocated bytes at the frozen revision.
The 2.332 us warm result for the 1,024-object, 80-spread head-to-head endpoint is outside the typical cached-operation gate. The warm gate reads only `CostPlanBenchmark.WarmEvaluate`; the endpoint table records the separate scaling exhibit.

The nightly endpoint selection replaces the head-to-head axis files in its checkout. Its uploaded `cost-performance-gate-inputs` artifact contains the micro directory and the head-to-head directory produced by that run, not the full committed benchmark exhibit.

The gate accepts two provenance states. A current run records the checkout `HEAD` and may have working-tree changes, including untracked nonignored files, only in the generated micro and head-to-head result paths. A committed result has no tracked or untracked nonignored changes, records an ancestor of `HEAD`, and permits only generated result files to differ from that ancestor. The `frozenAt` revision must resolve and be an ancestor of the measured checkout.

To refresh committed results, commit the gate and engine changes as revision `H`, measure from the clean `H` checkout, verify the gate in current-run mode, and commit only the generated result files as revision `A`. The clean `A` checkout then verifies in committed-result mode.

Run the head-to-head endpoints, micro measurements, and gate from the repository root:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/head-to-head/run-head-to-head.sh --endpoints
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/run-micro.sh
dotnet run -c Release --project src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks -- gate
```
