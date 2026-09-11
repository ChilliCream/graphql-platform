using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using HotChocolate.CostAnalysis;

var config = DefaultConfig.Instance
    .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

if (HeadToHeadPointRunner.TryRun(args, out var headToHeadExitCode))
{
    Environment.ExitCode = headToHeadExitCode;
    return;
}

if (args.Length > 0 && args[0].Equals("gate", StringComparison.OrdinalIgnoreCase))
{
    Environment.ExitCode = GateCommand.Run();
    return;
}

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, config);
