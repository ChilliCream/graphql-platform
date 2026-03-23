using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithToolchain(
        CsProjCoreToolchain.From(
            new NetCoreAppSettings("net9.0", null, ".NET 9.0"))));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
