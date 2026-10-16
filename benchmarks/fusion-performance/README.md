# Fusion performance investigation fixture

The gateway uses project references into an explicitly selected source worktree. Four
cached subgraphs use HotChocolate 16.6.8 packages and a four-millisecond delay per HTTP
request. Preparation exports all schemas, composes a new archive, and freezes independent
copies of the Release binaries before measurement.

The five cases are HTTP plain lists with 1,000, 5,000 and 20,000 rows at 100/s, 50/s
and 20/s, and direct completion with 1,000 or 20,000 rows. HTTP cases select
`upc name price weight`. Nested and deferred queries are validation-only controls.
Each HTTP case uses two 30-second warmups and three 60-second measured repetitions.

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
Run six fresh `confirm --gc fixed --case list-20000-plain --label confirm-fixed-N
--repetitions 1` sessions in order baseline-1, candidate-1, candidate-2, baseline-2,
baseline-3, candidate-3. Each validates every response before two 30-second warmups and a
60-second measured interval. Add `micro.py BASELINE --label micro-confirm --prepare-only`
and `--label micro-confirm --run-only` after HTTP confirmation to repeat both baseline
micro cases in a separate evidence directory. Build that project before starting its run.

BenchmarkDotNet uses three launches, five warmups, fifteen measured iterations, server GC,
and .NET 10 with automatic invocation calibration. Every worker checks the frozen Fusion
assembly hash and uses the exact composed schema. Setup compiles the operation, parses
source JSON into a held sealed arena and validates every output field. Each measured call
initializes the real store over a fresh result arena, invokes real ValueCompletion.BuildResult,
checks result count/errors, cleans the store, seals and disposes the result arena. The held
source document's binding ID resets between sequential calls. This excludes source parsing,
HTTP transport and JSON formatting. Path creation is not forced by the successful scalar
fields. Memory diagnostics include actual store initialization/completion/cleanup allocations.

The runners stop their recorded service processes and join their monitors on exit.
`lifecycle.py stop` can stop the currently recorded services after an interrupted session;
it checks executable ownership before signaling them.

After all measurement sessions stop, copy the current analysis helpers into each evidence
directory and run `analyze_profiles.py` there. `analyze.py BASELINE CANDIDATE --output DIR`
reduces primary HTTP results. Add `--phase 'confirm-fixed-*'` for the three fresh
large-list repetitions per variant. Raw profiles, process samples, interval boundaries, responses,
patches, and binary hashes remain in the evidence directories.
