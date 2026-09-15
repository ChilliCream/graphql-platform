# Cost analysis benchmarks

`CostAnalysis.Core.Benchmarks` holds the micro benchmarks (BenchmarkDotNet)
and the head-to-head comparison against the Rust oracle (see
[`CostAnalysis.Core.Benchmarks/head-to-head/README.md`](CostAnalysis.Core.Benchmarks/head-to-head/README.md)).

## Results

Both `run-micro.sh` and `head-to-head/run-head-to-head.sh` write their output
under `results/`. That directory is generated and git-ignored: benchmark
results are machine dependent, so comparing two versions of the engine, or
the engine against the Rust oracle, means running both sides head to head on
the same machine and reading the freshly generated output. No result is
committed to the repository.

Run the micro benchmarks:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/run-micro.sh
```

Run the head-to-head endpoints:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/head-to-head/run-head-to-head.sh --endpoints
```
