# Cost analysis benchmarks

`CostAnalysis.Core.Benchmarks` holds the micro benchmarks (BenchmarkDotNet)
and the head-to-head comparison against the Rust oracle (see
[`CostAnalysis.Core.Benchmarks/head-to-head/README.md`](CostAnalysis.Core.Benchmarks/head-to-head/README.md)).

## Results

Both `run-micro.sh` and `head-to-head/run-head-to-head.sh` write their output
under `results/`. Results are machine dependent. Run both versions or
implementations on the same machine for each comparison.

Run the micro benchmarks:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/run-micro.sh
```

Run the head-to-head endpoints:

```bash
bash src/HotChocolate/CostAnalysis/benchmarks/CostAnalysis.Core.Benchmarks/head-to-head/run-head-to-head.sh --endpoints
```
