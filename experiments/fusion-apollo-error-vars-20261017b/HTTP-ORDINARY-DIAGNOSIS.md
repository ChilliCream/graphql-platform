# First shared HTTP ordinary-control failure

The first baseline launch's ordinary supplemental control completed 99,481 successful measured iterations and dropped 522 scheduled iterations. All measured HTTP responses passed status, error and configured oracle checks; 100 setup requests are excluded. No candidate HTTP launch occurred. The preceding unchanged TestQuery (123,445 successes) and entities9 (186,638) phases completed and remain valid, retained controls.

This is a recorded generator/configuration failure, not evidence that either candidate regressed. No measurement was repeated to diagnose it.

## Native event evidence

The driver requested 5,000 arrivals/s for 20 seconds, with exactly 50 preallocated and maximum VUs. k6 warned that all 50 VUs were active and more could not be initialized. Native iteration intervals reconstructed from completion timestamps and `iteration_duration` reach 50 overlapping intervals. Sparse `vus` gauges peak at only 36, illustrating that one-second snapshots miss short peaks.

The 522 drops form 37 clusters when adjacent drop events are separated by at most 1 ms. The two largest clusters contain 55 drops each:

| Relative cluster start | Preceding gap between iteration starts | Drop cluster span | In-flight intervals before gap |
|---:|---:|---:|---:|
| 14.606039 s | 19.257545 ms | 1.441002 ms | 20 |
| 17.604138 s | 19.373655 ms | 1.142025 ms | 26 |

At the configured arrival rate, those gaps correspond to approximately 96 and 97 scheduled slots. The subsequent catch-up exceeds the reserve of 50 VUs. Other clusters follow similar gaps; all timestamps, inferred overlaps, 100 ms bins and the largest gaps are retained in `results/http-ordinary-first-control/drop-diagnosis.json`.

Inferred overlaps are not exact VU reservations. k6 can reserve a VU before its goroutine enters the measured iteration, and metric callback timestamps can differ from scheduler events. Thus some drops align with fewer inferred executing intervals even though no VU is available. The raw data does not identify whether the original gaps arose from OS scheduling, Go runtime activity, output backpressure or another generator pause.

Primary k6 `v2.3.0` [executor source](https://github.com/grafana/k6/blob/v2.3.0/lib/executor/constant_arrival_rate.go#L301) schedules against elapsed time, catches up after a delay and records a drop when `TryRunIteration` cannot obtain a free VU. The observed gaps and drop clusters support transient scheduling/catch-up exhaustion. They do not establish sustained Fusion or generator CPU saturation.

## Resource and accounting context

The HTTP owner's existing native-window analyzer reports ordinary generator use of 0.522 of two assigned CPU cores (maximum one-second observation 0.557); gateway use is 0.431 of four cores (maximum one-second 0.775); Accounts is 0.295 of eight shared source cores, with other sources approximately idle. These averages and one-second peaks cannot resolve sub-20 ms scheduling pauses. Median HTTP duration is approximately 0.340 ms, p99 5.652 ms, p99.9 13.192 ms and maximum 22.560 ms.

Process CPU is reconstructed from 100 ms `/proc` observations and ten-millisecond CPU ticks over native HTTP bounds. The ordinary runtime-counter callback cross-check differs by approximately +11.9% from the corresponding `/proc` interpolation. Original and entity phases differ by approximately -1.26% and -1.08%. The owner's interval review observes an approximately one-second sequence lag: the counter at 16:52:49 reports 1.354 CPU-s versus 0.387 OS CPU-s, and a later 0.769 counter observation follows a prior 0.765 OS interval. The preceding entity phase has substantially higher gateway CPU use. The original mismatch is retained; no shifted or trimmed window replaces primary full-phase CPU. Ten-millisecond ticks alone do not explain this discrepancy. It is not a phase-wide uncertainty bound.

Completed iterations plus drops equal 100,003 scheduled slots, not 100,000. The first inferred iteration starts at `2026-10-17T16:52:47.976489Z`, the last at `16:53:07.976577Z`, spanning 20.0000875 seconds; final completion is `16:53:07.976824Z`. The executor is timer-based and has no exact integer-count contract. The timer and duration-cancellation cases can race at the boundary. Requiring exactly `rate × duration` completions is a separate runner defect.

Correct accounting requires native HTTP completions = iteration completions = successful observations = measured summary count, total HTTP = measured count + exactly 100 setup requests, and zero native and summary drops. The immutable driver still specifies 5,000/s for 20 seconds. Actual scheduled counts, first/last native times and effective rate must be reported. Material achieved-rate or temporal departures remain validity/interpretation failures; no arbitrary count tolerance is introduced.

## Minimal correction and retained work

Preparation is authorized for raising only the ordinary supplemental driver's preallocated/max VUs from 50 to 200. Rate, duration, data, query, setup count, source delay, CPU affinity, gateway and source binaries stay fixed. Two hundred exceeds the observed catch-up backlog plus occupied VUs and leaves additional reserve; it is not a guarantee against larger future pauses. A subsequent zero-drop failure stops the campaign without an automatic retry.

The original TestQuery and entities9 measurements from this first baseline are retained. They must not be repeated as new measured controls merely to repair the failed ordinary phase. The approved continuation preparation separates all seven corrected ordinary cases into identically conditioned fresh gateway launches, plus the remaining six zero-delay original/entity launches and seven delayed original/burst launches. This is twenty launches and introduces no repeated valid measured original/entity phase. The failed ordinary phase is retained separately. No candidate result exists yet, and no new traffic is authorized by this report.

`analyze_http_drops.py` reads only retained native samples. Run it under the shared lock with the path to the failed `ordinary-fixed` directory. The first baseline's [complete raw native evidence and observers](https://github.com/ChilliCream/graphql-platform/tree/ad60368f0e309abc60d5c6e6fcc2cbf24946db38/performance/fusion-apollo-duplicate-paths-20261017b/results/http-initial-baseline) and [resource-accounting review](https://github.com/ChilliCream/graphql-platform/blob/ad60368f0e309abc60d5c6e6fcc2cbf24946db38/performance/fusion-apollo-duplicate-paths-20261017b/HTTP-FIRST-FAILURE.txt) are published by the shared HTTP worker. The copied diagnosis/summary and this script preserve this independent analysis without another load run.
