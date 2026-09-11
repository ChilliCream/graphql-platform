using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using HotChocolate.Fusion.Execution.Benchmarks;

var config = DefaultConfig.Instance
    .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

if (args.Length > 0 && args[0] == "probe")
{
    CorpusPlanningProbe.Run(args);
}
else if (args.Length > 0 && args[0] == "materialization-probe")
{
    var count = args.Length > 1 ? System.Int32.Parse(args[1]) : 32;
    var responses = await DeferredPolicyMaterializationBenchmark.RunProbeAsync(count);
    System.Console.WriteLine($"users={count}; responses={responses}");
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
