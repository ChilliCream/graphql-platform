# Cost analysis head-to-head

`run-head-to-head.sh` generates the benchmark corpus from the pinned MIT-licensed Rust crate in a temporary directory, then runs the Rust estimator and HotChocolate on the same host.

Each point runs in a fresh process. The point runner performs two untimed warmups, doubles the iteration count until a sample lasts at least 100 ms, confirms the iteration count with a sample lasting at least 200 ms, and records five samples. If that set's median is below 100 ms, it doubles the iterations and replaces the full set. It reports the median sample total, and campaign order is randomized with seed 20260829.

Run the three comparison endpoints:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/head-to-head/run-head-to-head.sh --endpoints
```

Use `--all` to include the schema-size, query-size, and pathological-Boolean series. Rust stable 1.90 or later is required.

## Results

The script writes its CSVs and `provenance.json` under `../results/head-to-head/`. Results are machine dependent. Run both implementations on the same machine for each comparison.
