# Fusion performance investigation fixture

The gateway uses project references into an explicitly selected source worktree. Four
cached subgraphs use HotChocolate 16.6.8 packages and a four-millisecond delay per HTTP
request. Preparation exports all schemas, composes a new archive, and freezes independent
copies of the Release binaries before measurement.

The five cases are HTTP 1,000-entity lookup at 10/s, 5,000-entity lookup at 1/s,
plain 5,000-row list at 20/s, and BenchmarkDotNet arena runs with 2,048 or 5,000 records.
Each micro record rents one data table of minimum length 1 and one metadata table of
minimum length 9, held until sealed arena disposal. Nested and deferred queries are
validation-only controls. HTTP cases use two 30-second warmups and three 60-second
measured repetitions, with all fields and values validated before load.

Provide a tool directory containing `k6`, `nitro`, `dotnet-trace`, `dotnet-counters`, and
`dotnet-gcdump`. The investigation report records their versions and the measured source
identities. Run preparation from the repository root to select its SDK. Do all builds and
tests before starting a measurement command. Run only one measurement session at a time.

```bash
export FUSION_PERF_TOOLS=/workspaces/platform-1/perf/tools/bin
export DOTNET_LOCAL_DEV=1
export DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK=1

python3 benchmarks/fusion-performance/prepare.py \
    /path/to/exact-baseline-worktree /path/to/perf/baseline \
    --tools "$FUSION_PERF_TOOLS"
python3 benchmarks/fusion-performance/prepare.py \
    /path/to/candidate-worktree /path/to/perf/candidate \
    --tools "$FUSION_PERF_TOOLS"

python3 benchmarks/fusion-performance/micro.py /path/to/perf/baseline --prepare-only
python3 benchmarks/fusion-performance/micro.py /path/to/perf/candidate --prepare-only

python3 benchmarks/fusion-performance/micro.py /path/to/perf/baseline --run-only
python3 benchmarks/fusion-performance/micro.py /path/to/perf/candidate --run-only

python3 /path/to/perf/baseline/measure.py matrix --gc fixed
python3 /path/to/perf/candidate/measure.py matrix --gc fixed
python3 /path/to/perf/baseline/measure.py profile --gc fixed
python3 /path/to/perf/candidate/measure.py profile --gc fixed
python3 /path/to/perf/baseline/measure.py memory --gc fixed
python3 /path/to/perf/candidate/measure.py memory --gc fixed
```

`--gc fixed` sets `DOTNET_GCDynamicAdaptationMode=0` for the gateway. It also rejects
observed decreases in the cumulative allocation counter. The report documents the default
GC counter discontinuity and the separate fixed-heap measurement configuration.
`normal-memory` uses a fresh 1,000-entity process and captures a live heap. Its default GC
mode is suitable for retention confirmation; its allocation deltas require the same
counter checks as other default-GC runs.

`confirm --gc fixed --label confirm-fixed-1 --repetitions 1` validates the controls,
performs the same two warmups, and measures one 5,000-entity repetition. Use three unique
labels per variant and interleave source variants for confirmation. Add
`--case list-1000-lookup --label confirm-small-1` to repeat the small case. Labels also separate
repeated `normal-memory` sessions. The `memory` mode adds a 60-second 5,000-entity burst at
four closed-loop VUs, a 60-second 1,000-entity recovery, and three 30-second idle windows.
It captures heaps after normal load, after the burst, and after idle. Keep case definitions and settings identical between
variants.

BenchmarkDotNet uses three launches, five warmups, fifteen measured iterations, server GC,
and .NET 10 with automatic invocation calibration. Each worker checks the frozen buffers
assembly hash. Setup runs one complete arena operation before measurements. Every measured
operation constructs an arena, rents the two tables per record, checks their first slots,
seals the arena, and disposes it. JSON payload and document parsing are outside this micro.

The runners stop their recorded service processes and join their monitors on exit.
`lifecycle.py stop` can stop the currently recorded services after an interrupted session;
it checks executable ownership before signaling them.

After all measurement sessions stop, copy the current analysis helpers into each evidence
directory and run `analyze_profiles.py` there. `analyze.py BASELINE CANDIDATE --output DIR`
reduces primary HTTP results. Add `--phase 'confirm-fixed-*'` for the three interleaved
large-lookup repetitions. Raw profiles, process samples, interval boundaries, responses,
patches, and binary hashes remain in the evidence directories.
