# BenchmarkDotNet outcome

C2 is abandoned because its ordinary execution-cost increase remains unresolved. The separately validated allocation savings are real, but this bounded BenchmarkDotNet campaign does not establish a worthwhile tradeoff without added execution cost. This is not a claim of a universal process-CPU regression. No further VU tests or BDN repeats are planned for C2.

C3 is also abandoned for insufficient net-benefit proof. Independent review found repeated unaffected batch9 increases of 3.18–5.72% against adjacent controls and the ordinary-cost concern. Its duplicate-path allocation case shows no adverse elapsed pattern, but that benefit does not establish an acceptable overall tradeoff. The unstable final ordinary control does not excuse the adverse batch pattern. No further VU tests, repeats, or production PR are planned for either candidate.

The seven fresh B/C2/C3/B/C3/C2/B launches ran from 17:19:03.911376 UTC to a complete final BDN report written at 17:20:23.025422 UTC on 2026-10-17. All 252 WorkloadActual and 252 WorkloadResult observations are retained. Every case contains twelve observations of 1,000 fully awaited real executor operations, including response disposal. Runtime, engine identity, real Apollo dispatch, and complete data oracles passed. The job uses default server GC and no outlier removal.

Arithmetic mean elapsed time per operation, microseconds:

| Launch | Arm | Ordinary | Batch9 | Duplicate96-to1 |
|---|---|---:|---:|---:|
| 1 | B | 11.9779 | 34.3334 | 55.7623 |
| 2 | C2 | 13.0388 | 35.5073 | 57.8738 |
| 3 | C3 | 11.8348 | 36.0991 | 56.3325 |
| 4 | B | 11.9049 | 34.9859 | 57.3379 |
| 5 | C3 | 12.7163 | 36.9862 | 55.6924 |
| 6 | C2 | 12.7283 | 33.7394 | 57.4550 |
| 7 | B | 15.9618 | 35.6805 | 57.3881 |

Candidate mean changes against the applicable preceding and following B controls:

| Candidate observation | Ordinary | Batch9 | Duplicate96-to1 |
|---|---:|---:|---:|
| C2 first | +8.86% / +9.52% | +3.42% / +1.49% | +3.79% / +0.93% |
| C2 second | +6.92% / -20.26% | -3.56% / -5.44% | +0.20% / +0.12% |
| C3 first | -1.19% / -0.59% | +5.14% / +3.18% | +1.02% / -1.75% |
| C3 second | +6.82% / -20.33% | +5.72% / +3.66% | -2.87% / -2.95% |

The registered repeated-greater-than-five-percent-against-both-controls rejection rule is not triggered twice for either arm. That absence does not establish acceptable cost. C2 ordinary is higher in both candidate launches than the first two stable controls, and the final ordinary control spans 12.85–24.03 microseconds. C2 has no ordinary allocation saving. C3 batch9 is higher against every applicable control. All block trends, standard deviations, native confidence intervals, overhead observations, and raw actual/result measurements are in `results/bdn-campaign/analysis.json` and the original BDN exports.

BDN reports elapsed time, not process CPU, and this campaign does not directly measure allocations. The prior calibrated production-path allocation evidence remains separate: C2 batch9 saves 768 bytes and duplicate96-to1 saves 64 bytes; C3 duplicate96-to1 saves 784 bytes. Ordinary saves zero. Existing zero-delay HTTP CPU comparisons do not show repeated harm for either candidate, but they do not resolve the ordinary BDN result or C3's batch pattern. Complete HTTP evidence is published at [b0b775c](https://github.com/ChilliCream/graphql-platform/blob/b0b775cffe53367f9b7ee6637308a50f148fe3ed/performance/fusion-apollo-duplicate-paths-20261017b/HTTP-STOPPED-REVIEW.md).

The fixed 1,000-operation iterations are shorter than BDN's recommended 100 ms minimum; native warnings and broad confidence intervals remain in the evidence. No observations were trimmed or rerun to improve the result. MemoryDiagnoser was omitted for the documented counter/GC validity reasons in [BDN-METHOD.md](BDN-METHOD.md).

The user turn interruption removed the outer tool session before it saved launch 7's final metadata. The original `launches.json` remains unchanged and still says running. The child independently produced a complete three-case BDN report, normal end and artifact-cleanup markers, and the expected exact runtime/engine identity. `interruption-recovery.json` validates all final samples without inventing an OS exit code or exact process-exit time. Owner cleanup verified all seven PID/start identities absent and lock free at 17:22:28.831820 UTC; independent verification followed at 17:22:49.239061 UTC. A later worker checkout has filesystem bounds 17:22:16.438–17:22:20.176 UTC, after the final report; its exact command-start time was not recorded.

Preparation and frozen identity manifest are immutable at [a59c7ac](https://github.com/ChilliCream/graphql-platform/blob/a59c7ac49408e02bd60e53704ae1f7d37a5af6db/experiments/fusion-apollo-error-vars-20261017b/results/bdn-preparation/manifest.json). Reproduce the analysis with `python3 analyze_bdn.py results/bdn-campaign`. Production source and the separately calibrated allocation results remain in [REPORT.md](REPORT.md).
