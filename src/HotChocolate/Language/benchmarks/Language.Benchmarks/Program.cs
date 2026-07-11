using BenchmarkDotNet.Running;
using HotChocolate.Language.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ReaderBenchmarks).Assembly).Run(args);
