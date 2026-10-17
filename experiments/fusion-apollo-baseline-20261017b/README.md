# Fresh Apollo Federation baseline, 17 October 2026

Platform base: `43a8dca2bd479ddc411d832ed97e42d9fcc8bd85`.
Benchmark fixture: `941599e4e27719084189e12d737e147069fdf794` from `graphql-federation-benchmarks`.

`fixture/subgraphs-net`, `fixture/k6`, `host/gateway.far`, host appsettings and the host startup policy come from that benchmark revision. The original checkout is unchanged. The gateway replaces four HotChocolate package references with source project references. `host/bench.props` supplies the install-generated `FUSION_16_7_OR_GREATER` define and pins runtime 10.0.12 with roll-forward disabled. The platform SDK is 11.0.100-rc.1.26425.128; referenced libraries target net10.0 and do not enable runtime-async. Subgraphs use their unchanged package references (16.6.8), SDK10.0.401 and runtime10.0.12.

The benchmark's current generated policy disables default gateway security. Production default security is not measured. Min worker/IO threads remain256; the fusion client retains Authorization header propagation, MaxConnectionsPerServer256 and RemoveAllLoggers. Logging remains None. GC retains the Web SDK server-GC default. `BENCH_DIAGNOSTIC_RUN=0` disables pool diagnostics during every measurement. Configuration, process environment allowlist and loaded mappings are retained with the results.

The host adds a diagnostic-only wire capture handler and permits requested operation plans when `BENCH_CAPTURE_PATH` is present. Only the separate fidelity session sets that variable. Timing and profiling sessions do not register this handler and leave operation-plan exposure disabled.

`fixture/k6/k6.js` is byte-for-byte unchanged, SHA256 `b967285db74541fd9b29ab68522b6f5902de4caf7e43946dab183205bc88ea6e`. The original setup makes2*VUs sequential requests; the timed executor then runs closed-loop constant VUs. `constant-latency` maps to `MODE=constant` and4ms artificial delay per source request. Burst retains the original10s at50,40s ramp to500,10s ramp to50 stages and5s graceful rampdown/stop. The shell's default nine repetitions are intentionally replaced by an explicit bounded campaign with two independent launches per source-delay configuration.

`focused.js` uses the same closed-loop HTTP request behavior for exact workloads in `workloads/`. The ordinary control selects `me{id name}`. `entities9` selects all nine existing products, their reviews/authors, and authors' reviews/products with variable `first=9`; it changes no fixture data/schema. Fidelity checks the product IDs and captures complete data oracles for per-VU response comparison. The original control uses its unchanged benchmark oracle. No synthetic source or altered database is used.

Preparation commands (shared lock):

```sh
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock dotnet build experiments/fusion-apollo-baseline-20261017b/host/eShop.Gateway.csproj -c Release --disable-build-servers --nologo -v minimal
cd experiments/fusion-apollo-baseline-20261017b/fixture
flock -s -o /workspaces/platform-1/perf/fusion-common-path.lock bash -c 'for project in subgraphs-net/eShop.*/*.csproj; do dotnet build "$project" -c Release --disable-build-servers --nologo -v minimal || exit; done'
```

`freeze.py` makes independent copies of output files under `/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b/frozen` and writes every binary/configuration SHA256. It refuses to overwrite frozen output. The initial unmeasured host output is retained under `superseded-unmeasured`, with its diagnostic plan-exposure correction reason.

Execution requires root's explicit measurement grant and an exclusive lock for startup through owned cleanup:

```sh
flock -x /workspaces/platform-1/perf/fusion-common-path.lock timeout 150s taskset -c 0,8 python3 experiments/fusion-apollo-baseline-20261017b/campaign.py fidelity
flock -x /workspaces/platform-1/perf/fusion-common-path.lock timeout 1000s taskset -c 0,8 python3 experiments/fusion-apollo-baseline-20261017b/campaign.py baseline
flock -x /workspaces/platform-1/perf/fusion-common-path.lock timeout 300s taskset -c 0,8 python3 experiments/fusion-apollo-baseline-20261017b/campaign.py profiles
```

The source gateway uses CPUs2-3,10-11; unchanged subgraphs share4-7,12-15; k6 uses1,9; the runner/observer/profilers use0,8. These are the benchmark's16-CPU allocations. Native timestamped k6 observations and100ms per-process CPU/RSS/thread samples are retained. CPU/request accounting excludes setup, uses matching request-completion windows and reports tick/interpolation limitations. These accounting limits are not an uncertainty bound for an entire phase. Sampled thread-time profiles are interpreted separately from process CPU; allocation ticks are sampled types/callers, not exact allocations/request.

Only process handles launched by the runner are cleaned up, after checking `/proc` start identities. Existing listeners cause a refusal to start. Final cleanup records every child return code.

Reproduce source builds in an isolated worktree whose HEAD remains the pinned platform base. Extract this evidence directory from its published commit into that worktree, leaving it untracked, before running preparation commands. This preserves the source revision consumed by source-link build metadata. `freeze.py` must run only after both gateway and subgraph builds; frozen files are ordinary independent copies, not hardlinks.

The first ordinary focused setup failed because `__ITER` is absent during k6 setup. Its scripts/logs are retained under `results/failed-focused-setup` and `results/baseline-r1-delay0/ordinary`. The preceding valid original run was retained. A corrected two-second focused smoke passed, then `resume-baseline` ran only unmeasured cases in an additional owned launch. `results/campaign-plain.py` is the exact resumed timing runner; the final source runner differs only by the documented four-component profile duration format. The initial source build left MSBuild servers inheriting the shared lock. Root verified and stopped those specific owned servers before measurement. The commands above use `flock -o` and `--disable-build-servers` to prevent inheritance.

Native data and compressed traces are under `results/`; `SHA256SUMS.json` identifies every published result. Decompressing each `.nettrace.gz` or `.speedscope.json.gz` reproduces the original byte stream. The uncompressed trace, binary copies and superseded unmeasured output remain in the local `perf/` directory. `analyze.py` regenerates the tables from native samples and process observations under a shared lock. `verify-fidelity.py` rechecks captured schemas, source requests and the unchanged query identity.
