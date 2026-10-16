# Fusion performance investigation fixture

The gateway uses project references into an explicitly selected source worktree. Four
cached subgraphs use HotChocolate 16.6.8 packages and a four-millisecond delay per HTTP
request. Preparation exports all schemas, composes a new archive, and freezes independent
copies of the Release binaries before measurement.

The comparison has five cases: HTTP requests with 50, 100, and 200 supplied variables,
and direct coercion of 200 nullable definitions with either one or all 200 supplied.
Every HTTP request has the same two aliases on two subgraphs and forwards every value.
All three HTTP rates are 100 requests/second. A validation-only omission control forwards
one supplied value with 199 omitted nullable variables. Each primary HTTP case has two
30-second warmups and three 60-second measured repetitions.

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
```

`--gc fixed` sets `DOTNET_GCDynamicAdaptationMode=0` for the gateway. It also rejects
observed decreases in the cumulative allocation counter. The report documents the default
GC counter discontinuity and the separate fixed-heap measurement configuration.
`confirm --gc fixed --case variables-200 --label confirm-fixed-1 --repetitions 1`
validates every response, performs the same two warmups, and measures one repetition.
Interleave source variants and use unique labels for confirmation.

BenchmarkDotNet uses three launches, five warmups, fifteen measured iterations, server GC,
and .NET 10. It calibrates invocation counts automatically. Both cases use the frozen
composed schema, parsed definitions and JSON from setup, then run the actual coercion
helper. Each worker verifies the frozen Fusion assembly hash before measuring.

The runners stop their recorded service processes and join their monitors on exit.
`lifecycle.py stop` can stop the currently recorded services after an interrupted session;
it checks executable ownership before signaling them.

After all measurement sessions stop, copy the current analysis helpers into each evidence
directory and run `analyze_profiles.py` there. `analyze.py BASELINE CANDIDATE --output DIR`
reduces primary HTTP results. Add `--phase 'confirm-fixed-*'` when there are three
confirmation repetitions per variant. Raw profiles, process samples, interval boundaries, responses,
patches, and binary hashes remain in the evidence directories.
