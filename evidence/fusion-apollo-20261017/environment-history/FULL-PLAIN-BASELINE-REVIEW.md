# Independent plain baseline review

Review UTC 2026-10-17T16:05:59.003733+00:00. Ten successful plain cases reviewed; failed focused setup attempt excluded and preserved.

The baseline is suitable for discovery and as retained workload evidence. It is not a single stable performance number. Original TestQuery delay-zero throughput changed from 4,071.99 to 2,936.12 requests/s between launches (−27.90%); gateway CPU/request changed from 0.584605 to 0.604489 ms (+3.40%). Future candidate effects require the already-required bounded matched/interleaved controls in the candidate campaign. Do not discard either valid baseline or retry to narrow the envelope.

The ordinary query saturates the two assigned generator CPUs: 1.907 and 1.911 average cores (95.3–95.6%), with one-second peaks 1.974 and 1.980. Its throughput cannot establish gateway capacity. A sensitive ordinary HTTP regression gate requires an explicitly different headroom configuration with its own planned controls; existing ordinary measurements remain valid observations and are not rerun. Original TestQuery, entity-nine and burst generator averages are at most 1.079 cores.

## Accounting and independent checks

Reviewed baseline owner analyze.py. Its native sample filter excludes setup and requires a scenario tag. All ten cases have native completed/latency/iterations/success counts agreeing with k6 summaries, zero failed/status errors, and total summary HTTP requests equal scenario counts plus setup. Original smoke and plain phases validate the original benchmark structure once per VU and status/error absence each iteration; this is not a full response oracle on every request.

Independent raw reparsing of second TestQuery delay-zero and second burst matches primary request counts, CPU/request and p99.9 exactly. The reviewer script also retains five-second blocks and p99.9 neighborhoods. Full source was reused for timing/interpolation from the prior partial reviewer; the primary owner parser supplies full campaign data. A superseded draft full-review invocation stopped at the retained failed setup artifact and was narrowed to these two raw spotchecks. No workloads were repeated.

CPU uses minimum native completion-minus-duration through final completion, with interpolated 100 ms /proc observations and 10 ms CPU ticks. HTTP duration excludes blocked/connect/TLS. Generator launch identity supplies a start point where ordinary traffic starts before its first 100 ms sample; gateway/source brackets are unchanged. Iteration-window sensitivity is recorded by the primary analyzer.

Counter JSON containers are incomplete after non-TTY SIGINT, but each ends after a complete event plus comma. Parsing repairs only the container in memory; raw files remain unchanged. dotnet.process.cpu.time has counterType Rate and cpu.mode=user/system, so values are summed over intervals after the first boundary, never differenced as cumulative totals. Runtime sums are 0.729–2.698% below matching /proc callback-window CPU. Callback timing, interval alignment and sampling limit this corroboration; this is not exact agreement or a full-phase uncertainty bound. Counter windows cover about 90.0–98.3% of traffic windows.

## Resource, tail and memory interpretation

Gateway average CPU is 1.058–2.381 of four assigned CPUs; all source processes share eight CPUs and combined averages are 1.160–3.278. Aggregate samples have headroom except the ordinary generator, but per-core utilization and non-CPU bottlenecks were not captured. Do not infer that these are unconstrained peak-capacity workloads. All five plain sessions preserve identical recorded nondelay environment and affinity per role.

p99.9 exceeds about 48 observations in the smallest original delay-four case and about 455 in the largest ordinary case. Neighboring ranks are retained. Tails cluster, so these are not independent rare events. In the second burst, 89 of 116 p99.9 exceedances occur at 45–50 seconds and another 27 at 50–55 seconds, near peak concurrency. Candidate burst comparisons must align stages. First TestQuery delay-zero has 56 of 123 exceedances in its first five seconds; the second has 65 of 89. Five-second windows are dependent observations within a launch.

GC pause and generation collection counters are preserved as interval sums, excluding partial edge intervals. These are diagnostic windows, not exact whole-phase totals. No process allocation-byte claim is supported by the unvalidated default-policy allocation counter. RSS minima/maxima and thread peaks are preserved; short throughput phases do not establish retained heap size, leak freedom or a general memory improvement.

| Session | Workload | Requests | req/s | CPU ms/request | p95 ms | p99 ms | p99.9 ms | Generator cores/2 |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| baseline-r1-delay0 | TestQuery | 122191 | 4071.99 | 0.584605 | 21.675 | 28.961 | 42.084 | 0.857 |
| baseline-r1-delay0-continuation | entities9 | 184452 | 9222.35 | 0.210591 | 11.031 | 17.380 | 30.080 | 1.009 |
| baseline-r1-delay0-continuation | ordinary | 445636 | 22275.89 | 0.078898 | 5.774 | 15.150 | 23.102 | 1.907 |
| baseline-r1-delay4 | TestQuery | 47652 | 1587.06 | 0.674346 | 39.071 | 43.048 | 67.664 | 0.375 |
| baseline-r1-delay4 | burst | 115003 | 1915.85 | 0.662617 | 236.196 | 254.004 | 269.959 | 0.506 |
| baseline-r2-delay0 | TestQuery | 88126 | 2936.12 | 0.604489 | 27.672 | 33.656 | 46.554 | 0.639 |
| baseline-r2-delay0 | entities9 | 196599 | 9829.19 | 0.211996 | 10.095 | 15.549 | 25.282 | 1.079 |
| baseline-r2-delay0 | ordinary | 454811 | 22739.97 | 0.078495 | 5.581 | 14.203 | 22.013 | 1.911 |
| baseline-r2-delay4 | TestQuery | 47963 | 1597.37 | 0.662598 | 38.861 | 43.213 | 61.762 | 0.376 |
| baseline-r2-delay4 | burst | 115971 | 1932.18 | 0.654803 | 237.268 | 256.415 | 270.486 | 0.511 |

Primary per-case outputs and lossless samples: `perf/fusion-apollo-baseline-20261017b/baseline-r*/<case>/`. Independent source: `review_campaign.py`; reviewed summaries: `full-plain-review/spotchecks.json`. Native caches are derived convenience files and are not necessary publication artifacts.
