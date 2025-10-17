using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Fusion.Execution.Benchmarks;

var config = DefaultConfig.Instance
    .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

BenchmarkRunner.Run<OperationPlannerBenchmark>(config);
