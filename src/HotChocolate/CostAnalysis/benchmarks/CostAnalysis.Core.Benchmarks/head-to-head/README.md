# Cost analysis head-to-head

`run-head-to-head.sh` regenerates the benchmark corpus from the pinned MIT-licensed Rust crate, then runs the Rust estimator and HotChocolate on the same host. The generated corpus is 16,299,915 bytes, so it exceeds the 5 MB commit limit and remains in a temporary directory.

Each point runs in a fresh process. The point runner performs two untimed warmups, doubles the iteration count until a sample lasts at least 100 ms, confirms the iteration count with a sample lasting at least 200 ms, and records five samples. If that set's median is below 100 ms, it doubles the iterations and replaces the full set. It reports the median sample total, and campaign order is randomized with seed 20260829.

Run the three comparison endpoints:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/head-to-head/run-head-to-head.sh --endpoints
```

Use `--all` to include the schema-size, query-size, and pathological-Boolean series. Rust stable 1.90 or later is required.
