using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Perfolizer.Mathematics.OutlierDetection;

namespace ApolloAliasBdn;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var enginePath = typeof(OperationPlanContext).Assembly.Location;
        var identity = new
        {
            runtime = RuntimeInformation.FrameworkDescription,
            GCSettings.IsServerGC,
            gcConfiguration = GC.GetConfigurationVariables(),
            executionPath = enginePath,
            executionDllSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(enginePath)))
        };
        if (identity.runtime != ".NET 10.0.12" || !identity.IsServerGC
            || (args[0] is "allocation" or "verify-fixed"
                ? identity.gcConfiguration["GCDynamicAdaptationMode"] is not 0L
                    || identity.gcConfiguration["HeapCount"] is not 4L
                : identity.gcConfiguration["GCDynamicAdaptationMode"] is not 1L))
        {
            throw new InvalidOperationException("Expected .NET 10.0.12 and default server GC adaptation.");
        }
        Directory.CreateDirectory(args[1]);
        File.WriteAllText(System.IO.Path.Combine(args[1], "runtime-identity.json"), JsonSerializer.Serialize(identity));
        if (args[0] == "allocation")
        {
            await AllocationValidation.RunAsync(args[1]);
            return 0;
        }
        if (args[0] is "verify" or "verify-fixed")
        {
            foreach (var shape in new[] { "ordinary", "alias2", "alias-sequence10" })
            {
                var fixture = new AliasBenchmarks { Shape = shape };
                try
                {
                    await fixture.InitializeAsync(warm: false);
                }
                finally
                {
                    await fixture.CleanupAsync();
                }
            }
            return 0;
        }
        if (args[0] != "benchmark")
        {
            throw new ArgumentException("Expected verify or benchmark.");
        }
        var job = Job.Default
            .WithToolchain(new InProcessEmitToolchain(TimeSpan.FromSeconds(60), logOutput: true))
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(12)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithGcForce(false)
            .WithOutlierMode(OutlierMode.DontRemove)
            .WithId("DefaultGC-1000AwaitedOperations");
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(job)
            .AddExporter(JsonExporter.Full)
            .WithArtifactsPath(args[1]);
        var summary = BenchmarkRunner.Run<AliasBenchmarks>(config);
        return summary.HasCriticalValidationErrors || summary.Reports.Length != 3
            || summary.Reports.Any(report => !report.Success) ? 1 : 0;
    }
}
