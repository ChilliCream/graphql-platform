# Second ordinary fixed-rate control failure

The first stage-one baseline completed 118,795 successful unchanged TestQuery requests and 174,205 entities9 requests. Their native counts, response checks, setup exclusion and process CPU windows independently passed. The subsequent ordinary control completed 99,898 successful measured requests and dropped 102 arrivals. No candidate started. Every completed ordinary response passed the configured checks; this does not make its offered-load gate valid.

The ordinary driver requested 5,000 arrivals/s for 20 seconds with 200 preallocated and maximum VUs. Its 100 setup requests are excluded. Native requests, iterations and successes agree, and completed plus dropped arrivals equal 100,000 in this run. The earlier 50-VU failure produced 100,003 scheduled arrivals; neither result establishes an exact integer-count contract for the timer-driven executor.

## Native scheduling evidence

All 102 drops occur between inferred scenario times 18.974831 and 18.996879 seconds, within one approximately 22 ms episode. A 33.694983 ms gap between inferred iteration starts immediately precedes it, from 18:21:13.989059 to 18:21:14.022754 UTC. Only two inferred iteration intervals overlap the beginning of that gap. At 5,000 arrivals/s the gap corresponds to approximately 168 scheduled arrivals. Catch-up and continuing arrivals subsequently coincide with 200 overlapping iteration intervals.

Grouping adjacent drops separated by at most 1 ms yields five clusters containing 9, 73, 6, 6 and 8 drops. All fall in the same 100 ms bin, which contains 404 starts and 265 completions. The maximum iteration duration for the whole phase is 36.639365 ms. Sparse VU gauges peak at only three, so their one-second observations cannot describe this transient peak.

Inferred starts are native iteration-duration completion timestamps minus the reported duration. They are not exact VU acquisition callbacks. The executor can reserve a VU before its iteration begins; therefore some drop callbacks coincide with fewer than 200 inferred executing intervals. The full overlap histogram, timestamps, 100 ms bins and largest gaps are retained without trimming.

The pinned k6 v2.3.0 [constant-arrival executor](https://github.com/grafana/k6/blob/v2.3.0/lib/executor/constant_arrival_rate.go#L301) schedules against elapsed time and drops when no free VU accepts an iteration. The current data supports transient catch-up/reservation exhaustion. It does not identify the original scheduling pause as OS activity, Go runtime work, output backpressure, gateway behavior or another cause. The earlier 50-VU failure had many shorter episodes; the current 200-VU failure is not assumed to have an identical underlying cause.

## Resource and accounting context

Over the ordinary native window, gateway use is 0.51096 of four assigned cores, the generator 0.62081 of two cores, and sources together 0.35117 of eight shared cores. The observed system average is 11.07% busy. These values exclude aggregate sustained CPU saturation but cannot resolve sub-34 ms stalls or single-core scheduling. Parent observer CPU was not sampled individually. The longest process-observer sample gap is 105.112 ms.

Primary gateway CPU is 0.102293 ms per successful ordinary response. The runtime-counter callback-window cross-check is approximately 9.44% above matching OS interpolation, retained with the previously observed interval-lag limitation. It does not replace the native primary window and is not a phase-wide uncertainty bound.

The valid TestQuery phase spans 30.008381 seconds with 70.278360 gateway CPU-seconds, or 0.591594 ms per success. Entities9 spans 20.001456 seconds with 37.643570 CPU-seconds, or 0.216088 ms per success. Their source and generator resource observations show aggregate headroom. They remain applicable retained controls, with any later continuation gap disclosed.

## Proposed correction and limits

Increasing the VU reserve again would not establish a bound on future pauses. The proposed ordinary regression control instead reuses the already validated, unchanged focused driver at one closed-loop VU for 20 seconds, with its native two setup requests. It executes the same real HTTP query, full representative oracle, status/error checks and process CPU accounting. It has no timer-driven arrival backlog. Its observed throughput is descriptive and is not a maximum-capacity result; resource headroom must still be checked.

The unchanged original benchmark remains the primary concurrency/load control. A continuation can retain the two successful first-baseline phases, run the eight remaining Apollo launches, and run nine fresh identically conditioned ordinary-only launches. Each ordinary launch has the same 30-second original-query warmup before its 20-second ordinary phase. This avoids comparing a separately restarted baseline with differently conditioned candidates. No correction or new traffic is authorized by this report.

The campaign fail-stopped at 18:21:15.437929 UTC. Independent verification at 18:21:35.860884 found all twelve exact child and parent PID/start identities absent and the exclusive lock free. No process was terminated by that verification. The failed ordinary samples and successful preceding phases are preserved.

`analyze_http_drops_stage1.py` reads retained native samples only. It reuses the earlier parser, changing only the cluster minimum initialization from the old fixed 50 VUs to the recorded configured maximum. Run it under the shared lock with the failed ordinary case directory. The evidence manifest records the original raw file paths and SHA-256 identities; the HTTP owner publishes complete raw samples and observers separately.
