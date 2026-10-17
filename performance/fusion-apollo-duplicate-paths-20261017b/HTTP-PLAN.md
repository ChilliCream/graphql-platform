# Planned shared HTTP regression gate

Status: preparation only. The root accepted this bounded design, but no HTTP measurement is authorized until C2 reaches its micro decision and the root grants the final survivor schedule. C2 owns the common runner; C3 owns its frozen gateway and interpretation. No source, selected data, schema, delay implementation, security, logging or GC policy changes are part of the comparison.

## Launch order and cases

Use fresh processes for every arm/configuration launch and the existing frozen source fixtures. B means the published baseline gateway. With only C3 surviving, each delay configuration uses B,C3,B,C3,B. If C2 also survives, each uses B,C3,C2,B,C2,C3,B. Each candidate has two independent launches with controls on both sides; the latter order reverses candidate ordering. Controls are planned comparison controls in the same time window, not repeated discovery baselines. The initial valid baseline remains retained context.

| Source delay | Per-launch sequence |
|---|---|
| 0 ms | Original TestQuery warmup 30 s; original TestQuery constant 50 VU 30 s; entities9 constant 50 VU 20 s; ordinary fixed arrival rate 5,000 requests/s for 20 s |
| 4 ms | Original TestQuery warmup 30 s; original TestQuery constant 50 VU 30 s; unchanged original 60 s burst (10 s target 50, 40 s target 500, 10 s target 50), then 20 s observed idle |

Original `k6.js` remains byte-identical, including its setup and request/oracle semantics. The burst executes 1,000 sequential setup requests at 500 VU; setup is excluded from measured native counts and CPU windows, but adds approximately 25–35 s per 4 ms launch. Expected total campaign duration is approximately 23–25 min for C3-only (10 launches) or 32–35 min for both survivors (14 launches), including startup/setup/cleanup. Each child retains the existing explicit timeout and owned PID/start-identity cleanup.

The ordinary fixed-rate driver uses 50 preallocated and 50 maximum VUs, the unchanged ordinary query/oracle/headers, and requires zero dropped iterations. It is an explicitly different workload configuration from the initial ordinary 50 VU baseline, which saturated its two generator cores. Its 100,000 offered iterations support a headroom-safe regression check; fixed throughput is not a capacity measurement. Any failure to sustain the offered load must remain visible, rather than changing rate or limits inside the campaign.

## Frozen C3 identity and correctness

C3 directory: `/workspaces/platform-1/perf/fusion-apollo-duplicate-paths-20261017b/frozen/gateway`. Its 106 files are the exact published baseline gateway plus the measured C3 execution DLL/PDB. Execution DLL SHA-256: `359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf`. `results/gateway-freeze-manifest.json` records every hash. Host/FAR/dependencies/runtimeconfig remain identical; runtime is 10.0.12, net10.0, RollForward Disable and server GC. Baseline source is 43a8dca2; production prototype is preserved at 049e0d587fbb9d9304a13954136fe9886f141845.

Before warmup, compare complete returned data against the frozen oracle and reject GraphQL errors. Keep the original measured once-per-VU structure check and per-request status/error checks unchanged; focused drivers compare complete data once per VU. Record loaded module/runtime maps and process start identities. Timing must keep capture diagnostics and the diagnostic pool logger disabled. Header propagation, unique Authorization values, minimum threads 256, maximum connections 256, disabled default security and removed loggers remain those of the original benchmark host.

## Accounting, resource checks and retention

Keep validated timestamped native samples, summary counts, and 100 ms process CPU/RSS observations. Keep 1 s runtime counters throughout warmup, all timing cases and idle. Reconcile measured requests/iterations/successes/status with setup and summary totals. Match CPU to the earliest measured request completion minus duration through the final completion; preserve iteration-window sensitivity and CPU tick/interpolation limitations. Runtime CPU interval-rate sums are a separate callback-window cross-check, not cumulative totals or a whole-phase uncertainty bound.

Check gateway, all source processes and generator headroom for each case. Keep both adjacent controls, the full control envelope and time trend. For burst tails, compare equivalent stages and retain rank neighbors, exceedance counts and temporal clustering. Sparse p99.9 observations are not independent launches.

Mark the pre-burst-setup, final measured completion and idle boundaries. Report gateway RSS before the burst, at peak and over the final 5 s of the 20 s idle interval against both controls; retain raw heap/committed/runtime gauges with timestamps. Heap and committed gauges may describe the last GC and can remain stale during idle. Do not force a GC. RSS or scoped bytes cannot by themselves prove retained heap or absence of a leak.

Independent ownership/retention review: [072a1890c41e130401904307d77ce39ca66438d3](https://github.com/ChilliCream/graphql-platform/blob/072a1890c41e130401904307d77ce39ca66438d3/performance/fusion-apollo-scratch-20261017b/C3-HTTP-REVIEW.txt). The original-query static bound is 432 pooled outer slots (about 3,648 B) plus a capacity 8 ownership list (about 120 B/store), compared with 2,680 B of exact fresh outer arrays. With 500 overlapping original-query requests, that is an upper incremental outer capacity/bookkeeping estimate of about 544 KB, assuming all corresponding baseline arrays also overlap. It is not a global pool retention cap or measured heap size. A specific persistent unexplained memory excess could justify a targeted retention capture; no heap capture is planned preemptively.

## Decision

The only established benefit entering HTTP is the controlled scoped 2,680 B saving for the original representation sequence. Micro timing establishes no generic speedup. Preserve the small repeated unique5 timing increases and the isolated adverse snapshot96/duplicate96 launches in the final interpretation. A worthwhile allocation saving still needs the HTTP regression gate. Repeated adverse CPU/request or latency patterns, inadequate headroom, conflicting controls or otherwise inconclusive applicable evidence prevent success and a draft PR. Do not rerun valid measurements to seek a better outcome.
