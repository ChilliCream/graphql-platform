using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Fusion.Execution.Benchmarks;

var config = DefaultConfig.Instance
    .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

if (args.Length > 0 && args[0] == "probe")
{
    NetflixPlanningProbe.Run(args);
}
else if (args.Length == 0)
{
    BenchmarkRunner.Run<GraphQLQueryBenchmark>(config);
}
else
{
    BenchmarkSwitcher
        .FromAssembly(typeof(GraphQLQueryBenchmark).Assembly)
        .Run(args, config);
}
