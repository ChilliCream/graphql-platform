using BenchmarkDotNet.Running;
using HotChocolate.Validation.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(OverlappingFieldsMergedBenchmark).Assembly).Run(args);
