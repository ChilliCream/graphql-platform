# Stopped first survivor HTTP baseline

No candidate HTTP process ran. The first shared baseline completed the original and entities phases, then the supplemental ordinary5000/s200VU phase dropped102 iterations and triggered the strict stop rule. No retry or continuation was performed.

The campaign ran18:19:32.490838–18:21:15.438117 UTC. Owned cleanup completed18:21:15.437421; independent cleanup18:21:35.860884 found all12 exact identities absent and the exclusive lock free. Raw and failed-phase evidence is retained losslessly; only layouts pointing to already frozen binaries are excluded from this artifact copy.

| Case | Successful requests | Drops | Gateway CPU ms/success | Gateway cores | Source combined cores | Generator cores | p99 ms | p99.9 ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| TestQuery | 118795 | 0 | 0.591594 | 2.3420 | 2.8748 | 0.8561 | 27.5362 | 40.6483 |
| entities9 | 174205 | 0 | 0.216088 | 1.8820 | 3.0115 | 1.0119 | 16.9530 | 28.7986 |
| ordinary-fixed | 99898 | 102 | 0.102293 | 0.5110 | 0.3512 | 0.6208 | 15.3793 | 28.1917 |

All three phases have exact native/summary successful requests=iterations=success observations, status200, no GraphQL/HTTP errors and exactly100setup requests excluded. Ordinary had99,898 completed+102dropped=100,000 scheduled iterations and is invalid as a zero-drop regression control. The two completed original/entity phases remain valid retained baseline observations. These are baseline-only measurements with no candidate/control comparison.

Gateway/source/generator affinities provide4/8/2 logical CPUs. Aggregate utilization does not show sustained saturation; it cannot exclude short single-core stalls. The parent monitor is pinned to0,8 but not individually sampled; countersCPU and aggregate systemCPU do not establish parent headroom. Maximum monitor spacing was105.6ms, near its100ms period. No per-core observations were captured.

Primary CPU windows remain earliest nonsetup HTTP end minus duration through final HTTP completion, using interpolated100ms process observations and10ms CPU ticks. Original/entity runtime CPU-rate checks differ from matching callback-window OS totals by−1.54%/−0.58%; ordinary differs by+9.44%. Counter callback timing and prior-phase interval leakage remain limitations; no shifted window replaces primary CPU. The stopped counter JSON has the established incomplete container; raw bytes are preserved and only the complete-event prefix is parsed.

The bounded setup capture returned seven HTTP1.1 chunked GraphQL JSON bodies with no Content-Length or Content-Encoding:285,355,220,74030,187,134,178 bytes. This directly observed urllib transport supports unknown-length source response applicability, but does not establish exact gateway protocol/framing or physical arena adjacency. Source-response bodies, hashes and whitelisted framing headers are retained.

All individual latency observations, rank neighbors and one-second tail clusters remain in the native samples and per-case analysis. No tail trimming, candidate CPU benefit, allocation/request extrapolation or common-cause attribution is made. Independent ordinary scheduling diagnosis is retained separately when available. Root must approve any revised continuation; the original nine-launch execution is not automatically resumed.

Independent ordinary scheduling review locates all102drops in one approximately22ms episode following a33.695ms inferred iteration-start gap. Reconstructed concurrent iteration occupancy reaches200; sparse VU telemetry misses that peak. This supports a scheduler catch-up/available-VU limit, without attributing the pause itself to any unobserved cause. No candidate ran, and no reserve increase or workload retry is authorized. Independent completed-phase review confirms exact counts, phase non-overlap, affinities and native CPU windows for both retained phases.
