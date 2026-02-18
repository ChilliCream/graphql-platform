using BenchmarkDotNet.Running;
using HotChocolate.Execution.Abstractions.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(PathBenchmark).Assembly).Run(args);
