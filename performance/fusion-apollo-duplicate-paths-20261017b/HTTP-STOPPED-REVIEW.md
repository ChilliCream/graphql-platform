User-directed stopped HTTP continuation, existing evidence only

Started2026-10-17T17:01:09.139829Z; userSIGTERM17:12:01.487914Z; graceful end17:12:01.727923Z.
Nine complete launches; tenth ordinary control interrupted during warmup. No4ms launches. All86 exact owned identities absent, independent lockfree17:12:34.862298Z. No automatic continuation/retry.

Decision: neither C2 nor C3 shows a repeated meaningful CPU increase against both adjacent zero-delay Apollo controls. This is not a CPU improvement claim or final acceptance. Both require the user-requested BenchmarkDotNet gate. Reliable allocation savings qualify; meaningful added CPU cost disqualifies.

TestQuery controls drift0.577764→0.594572→0.616085ms/success and4114→4008→3005success/s. The first control is retained from16:51, with an explicit interruption gap. All controls and adverse observations retained.

| Launch | Workload | CPU ms/success | vs before | vs after | p99 ms | p99.9 ms |
|---|---|---:|---:|---:|---:|---:|
| delay0-01-C3 | TestQuery | 0.588037 | +1.78% | -1.10% | 27.916 | 41.717 |
| delay0-01-C3 | entities9 | 0.214364 | -0.28% | -2.36% | 17.044 | 27.926 |
| delay0-02-C2 | TestQuery | 0.591778 | +2.43% | -0.47% | 27.990 | 40.404 |
| delay0-02-C2 | entities9 | 0.217997 | +1.41% | -0.70% | 17.262 | 29.337 |
| delay0-04-C2 | TestQuery | 0.598687 | +0.69% | -2.82% | 28.922 | 42.303 |
| delay0-04-C2 | entities9 | 0.215014 | -2.06% | -1.06% | 16.205 | 27.553 |
| delay0-05-C3 | TestQuery | 0.619656 | +4.22% | +0.58% | 34.081 | 44.961 |
| delay0-05-C3 | entities9 | 0.219437 | -0.05% | +0.97% | 16.173 | 25.648 |

All17 completed workload phases pass exact nativeHTTP=iterations=success observations, status200, zero errors/drops, and summaryHTTP=native+100setup accounting. Data includes2retained original/entities phases; completed continuation adds12Apollo phases and3ordinary phases.
Gateway1sCPUmax≤2.631/4 assigned cores; generator1smax≤1.176/2; source mean≤3.351/8. Aggregate capacity is available, but per-core concentration and external scheduler effects are not excluded. No general throughput claim: late C3TestQuery and finalcontrol both decline markedly.
C3lateTestQuery p99+10.06% versus preceding control,+0.24% versus following; p99.9+1.40%/−2.30%. Sparsep99.9 observations are clustered, commonly45–70 in one second for original traffic; the full neighbor samples/clusters are retained in analysis-completed.json. Do not trim first measured seconds or adverse observations.
RuntimeCPU counters are interval Rate values, not cumulative totals. Callback-window runtime/OS differences range−1.90% to+1.14% here; endpoints are not internal instrument boundaries or fullphase uncertainty bounds. PrimaryCPU remains interpolated /proc totals over nativeHTTP windows,100ms samples/10ms ticks. No counter-derived exact allocation bytes.
Corrected ordinary200VU: B100002,C399998,C299994 completed, zero drops/errors and100setup each. CPU0.098023/0.098552/0.095235ms/success. These lack a following ordinary control and cannot establish regression acceptance. Nominal100000 is descriptive, time-basedscheduleractual counts retained.
Burst/20s idle retention gates remain unmeasured. RSS cannot establish retained managed heap. User-directed stop, not benchmark failure or candidate rejection. Partialwarmup raw/logs retained.

Offline source: http/review_stopped.py imports unchanged frozen analyzer; comparisons only complete Apollo0 cohort. analysis-omitted.json explicitly lists missing phases. Raw source binaries remain the exact frozen plan references; evidence copy omits layouts symlinks, not process/native/runtime records.
