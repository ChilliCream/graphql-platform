**Terminal decision: ABANDONED (2026-10-17).** The validated scoped allocation savings remain real within their measured scope. Genuine BenchmarkDotNet found the unaffected batch case consistently 3.18–5.72% slower than adjacent controls across both candidate launches, with an additional ordinary-case concern and noisy final control. Existing zero-delay HTTP CPU observations do not resolve that cost, and delayed/burst retention remains unmeasured. Net benefit is unproven; no further C3 BDN/VU runs or implementation PR will be created. This is a bounded decision, not a universal regression claim.

See [complete BDN evidence](https://github.com/ChilliCream/graphql-platform/tree/4125af97231cff75e8b62a53b6f9d29d677f48b7/experiments/fusion-apollo-error-vars-20261017b) and [retained stopped HTTP evidence](https://github.com/ChilliCream/graphql-platform/blob/b0b775cffe53367f9b7ee6637308a50f148fe3ed/performance/fusion-apollo-duplicate-paths-20261017b/HTTP-STOPPED-REVIEW.md). Earlier gate states below describe the historical investigation before this terminal decision.

# C3 duplicate representation path backing

Status: QUEUED_FOR_HTTP. The corrected micro gate validates a scoped allocation saving. It does not establish a gateway improvement or qualify this prototype for a PR.

Production prototype, correctness evidence and initial frozen identities: `049e0d587fbb9d9304a13954136fe9886f141845`. Base source: `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`. Fresh benchmark baseline: [ef9fc1159a5a7a980754ebd42b511030320482cd](https://github.com/ChilliCream/graphql-platform/tree/ef9fc1159a5a7a980754ebd42b511030320482cd/experiments/fusion-apollo-baseline-20261017b). Shared C1/C3 campaign is also independently published at [fcbd128aea42f06be400d1cbaf8597f76eccb599](https://github.com/ChilliCream/graphql-platform/tree/fcbd128aea42f06be400d1cbaf8597f76eccb599/performance/fusion-apollo-scratch-20261017b).

## Change and correctness

The request store owns rented outer `CompactPath[]` buffers for duplicate entity targets until Clean, subscription Reset or Dispose. The immutable `EntityResultPath[]` result and underlying integer path pools are unchanged. Requests with no duplicate paths do not allocate an ownership list. Buffers requested above 1,024 paths remain exact allocations; ownership-list capacities above 16 are discarded after cleanup. Every returned outer buffer is cleared. See PLAN.md for the complete lifetime analysis and exclusions.

Release build and all 70 focused tests pass, including concurrent pending fetches, duplicate source cancellation, post-yield transport failure with both error paths, detached diagnostics after Clean/Reset/Dispose, and oversized fallback. Root and C1 independently reviewed ownership; no new public retaining escape was identified. Tests do not turn an internal request-lifetime view into an indefinitely retained public result buffer.

## Corrected measurement and allocation validation

The first campaign used unsealed arena disposal and is invalid for a normal request lifecycle. All raw adverse and favorable observations remain in `raw/invalid-first/`, with independent analysis in INVALID-FIRST-MICRO.md. The correction seals both arenas inside the measured Clean method. No production binary or case dimensions changed.

The corrected campaign ran once with order B,C1,C3,B,C3,C1,B,C1,C3,B, first for allocation and then for default timing. Each arm uses identical frozen harness/dependencies except its Fusion execution DLL/PDB. Harness SHA-256 `c79928c6ed41efdaf380be70f575e38e67bf8ef00b2fb51ebc5e4edfe1c8a9b6`; baseline engine `9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b`; C3 engine `359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf`. Runtime .NET10.0.12, server GC, affinity 2,3,10,11. Allocation used 64 warmups and 4 observations × 16 requests/case. Timing used 512 warmups and 20 observations × 128 requests/case. Blocks within a launch are not independent launches.

Scoped allocations measure only production representation creation and Clean on the current thread. Prepare and final arena disposal are outside the byte interval. Each allocation region passed exact 523240-byte known-array calibration before and after measurement, had unchanged gen0/gen1/gen2 counts, remained in NoGCRegion and ended successfully. All four baseline launches and all three C3 launches produced identical bytes in every observation. These are controlled scoped bytes, not default total-process allocation bytes.

| Case | Baseline bytes | C3 bytes | Saved bytes |
|---|---:|---:|---:|
| unique1 | 256 | 256 | 0 |
| unique5 | 352 | 352 | 0 |
| unique6 | 376 | 376 | 0 |
| unique9 | 448 | 448 | 0 |
| duplicate96 | 1,040 | 256 | 784 |
| duplicate1026 | 8,480 | 8,480 | 0 |
| unique96 | 2,536 | 2,536 | 0 |
| unique1024 | 24,808 | 24,808 | 0 |
| mixed96 | 1,152 | 424 | 728 |
| testquery_sequence | 5,808 | 3,128 | 2,680 |
| large_then_small | 26,456 | 25,672 | 784 |
| snapshot96 | 1,152 | 424 | 728 |
| null96 | 208 | 208 | 0 |

The original-query dimension sequence has eleven representation builds, 335 targets and 24 unique representations, including eight duplicate-bearing builds. Its 2,680-byte saving exactly matches the eight removed exact outer-array allocations. This is 46.14% of that scoped micro operation's 5,808 baseline bytes; it is not 46.14% of an HTTP request's total allocation.

## All default-timing launches

Values below are complete-launch arithmetic means in microseconds per micro request. Every block is included; no outlier trimming, favored control group or timing rerun was used.

| Case | B0 / B3 / B6 / B9 | C3-2 / C3-4 / C3-8 |
|---|---|---|
| unique1 | 2.199 / 2.155 / 2.616 / 2.190 | 2.209 / 2.498 / 2.198 |
| unique5 | 5.488 / 5.524 / 5.509 / 5.517 | 5.693 / 5.625 / 5.534 |
| unique6 | 6.185 / 6.315 / 6.364 / 6.272 | 6.271 / 6.369 / 6.277 |
| unique9 | 8.448 / 8.825 / 8.631 / 8.509 | 8.518 / 8.711 / 8.562 |
| duplicate96 | 77.018 / 91.118 / 85.692 / 84.049 | 85.378 / 96.885 / 74.600 |
| duplicate1026 | 134.552 / 129.995 / 128.870 / 126.114 | 124.973 / 122.993 / 137.877 |
| unique96 | 12.392 / 12.362 / 12.323 / 12.557 | 12.407 / 12.589 / 13.029 |
| unique1024 | 140.539 / 139.309 / 141.186 / 145.523 | 141.827 / 147.557 / 141.300 |
| mixed96 | 11.579 / 11.497 / 11.553 / 11.586 | 11.742 / 11.714 / 11.534 |
| testquery_sequence | 44.269 / 46.020 / 46.728 / 42.890 | 43.783 / 44.022 / 44.248 |
| large_then_small | 152.379 / 151.479 / 155.196 / 157.635 | 154.077 / 154.614 / 156.260 |
| snapshot96 | 27.124 / 24.816 / 26.141 / 25.109 | 25.891 / 25.555 / 30.503 |
| null96 | 5.532 / 4.991 / 5.012 / 5.180 | 4.996 / 4.989 / 5.062 |

The primary sequence lies within the full control envelope. C3 comparisons with both adjacent controls are −1.10%/−4.86%, −4.34%/−5.79%, and −5.31%/+3.16%. That supports no speedup claim. Ordinary 1/5/6/9 cases show no repeated worsening above 5% against both adjacent controls. Unique5 is slightly slower in all three C3 means (+0.32% to +3.72%, about 18–204 ns); that adverse pattern remains visible and must not be dismissed. Unique96's third launch is+5.73%/+3.76%, while earlier launches are+0.11%/+0.36% and+1.83%/+2.16%.

Duplicate96 has substantial within-case transitions in both arms, including baseline first-five/last-five means 88.76/34.96 us and C3 third-launch 98.45/32.27 us. Its second C3 launch is+6.33%/+13.06%, while the third is−12.94%/−11.24%. Timing does not isolate a stable duplicate96 effect. The final snapshot96 C3 launch is persistently 30.50 us, +16.69%/+21.48% against adjacent controls; its other two launches have mixed control comparisons. This isolated adverse launch is retained as an unresolved limitation, not normalized away. Mixed96's first two C3 launches are+1.39% to +2.12%, with the third slightly faster. Detailed early/late blocks, full envelopes, all adjacent ratios and GC counts are in `results/corrected-c3-review.json`.

The root accepted advancement to HTTP for the demonstrated scoped allocation saving, with no micro speedup claim. Acceptance still requires relevant HTTP evidence without an unresolved repeated adverse gateway pattern. The initial ordinary HTTP baseline was generator-saturated, so a headroom-safe ordinary gate needs its own explicitly changed configuration and matching controls.

## Memory and HTTP freeze

The optimization exchanges allocation for pooled references held until request completion. Rented arrays can have spare capacity, and the ownership list itself persists when small. The source caps the requested pooled-array length and retained list capacity, but scoped allocation measurements do not establish process retained memory. Active and post-load RSS/runtime observations belong in the HTTP gate; RSS alone cannot prove heap retention or a leak.

The isolated C3 gateway is copied from all 106 published baseline gateway files and overlays only the measured execution DLL/PDB. `results/gateway-freeze-manifest.json` verifies every other hash is identical, including the host, FAR, deps and runtimeconfig. Runtime remains net10.0/10.0.12 with RollForward Disable and server GC. Original source fixtures, schema, headers, connection limits, thread minima, security policy and logging configuration are inherited unchanged. Actual loaded runtime/assembly identities and response fidelity remain to be observed during the shared HTTP campaign.

## Reproduction

`Micro/` preserves the invalid fixture; `MicroCorrected/` is the corrected fixture. Exact frozen manifests, commands, PID/start identities, UTC windows, stderr allocation validation and raw per-block observations are in `raw/`. `run_joint_micro.py` and `run_joint_micro_corrected.py` preserve the bounded schedules. `review_micro.py` independently computes full C3 comparisons; `review_allocations.py` checks every C3/control allocation observation and validation record. `freeze_gateway.py` recreates the isolated candidate overlay and rejects unrelated binary differences. `results/raw-micro-sha256.json` fingerprints all copied raw artifacts.
