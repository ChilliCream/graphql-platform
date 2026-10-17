using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApolloErrorVariablesMicro;
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

namespace ApolloExecutorBdn;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var enginePath = typeof(OperationPlanContext).Assembly.Location;
        var identity = new
        {
            runtime = RuntimeInformation.FrameworkDescription,
            memoryDiagnoserEnabled = Attribute.IsDefined(typeof(ExecutorBenchmarks), typeof(MemoryDiagnoserAttribute)),
            GCSettings.IsServerGC,
            gcConfiguration = GC.GetConfigurationVariables(),
            executionPath = enginePath,
            executionDllSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(enginePath)))
        };
        if (!identity.memoryDiagnoserEnabled || identity.runtime != ".NET 10.0.12" || !identity.IsServerGC
            || identity.gcConfiguration["GCDynamicAdaptationMode"] is not 1L)
        {
            throw new InvalidOperationException("Expected .NET 10.0.12 and default server GC adaptation.");
        }
        Directory.CreateDirectory(args[1]);
        File.WriteAllText(System.IO.Path.Combine(args[1], "runtime-identity.json"), JsonSerializer.Serialize(identity));
        if (args[0] == "verify")
        {
            foreach (var shape in new[] { "ordinary", "batch9", "duplicate96-to1" })
            {
                var fixture = new ExecutorBenchmarks { Shape = shape };
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
        var summary = BenchmarkRunner.Run<ExecutorBenchmarks>(config);
        return summary.HasCriticalValidationErrors || summary.Reports.Length != 3
            || summary.Reports.Any(report => !report.Success) ? 1 : 0;
    }
}

[MemoryDiagnoser]
public class ExecutorBenchmarks
{
    private ServiceProvider? _provider;
    private IRequestExecutor _executor = null!;
    private IOperationRequest _request = null!;
    private long _completed;

    [Params("ordinary", "batch9", "duplicate96-to1")]
    public string Shape { get; set; } = "ordinary";

    [GlobalSetup]
    public Task SetupAsync() => InitializeAsync(warm: true);

    public async Task InitializeAsync(bool warm)
    {
        var shape = Shape switch
        {
            "ordinary" => new Case(Shape, 1, 1, false, "success", true),
            "batch9" => new Case(Shape, 9, 9, true, "success"),
            "duplicate96-to1" => new Case(Shape, 96, 1, false, "success"),
            _ => throw new InvalidOperationException("Unexpected benchmark shape.")
        };
        var client = new FixtureClient(shape);
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway()
            .AddInMemoryConfiguration(SchemaFixture.ComposeApolloSchema());
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new ClientFactory(client));
        FusionSetupUtilities.Configure(builder, setup =>
        {
            setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("a"));
            setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("b"));
        });
        _provider = services.BuildServiceProvider();
        _executor = await _provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        _request = OperationRequestBuilder.New().SetDocument(shape.Ordinary
            ? "{ products { id } }"
            : shape.Batch ? "{ products { name } categories { title } }" : "{ products { name } }").Build();
        var initial = await _executor.ExecuteAsync(_request);
        string oracle;
        try
        {
            using var buffer = new PooledArrayWriter();
            JsonResultFormatter.Default.Format(initial.ExpectOperationResult(), buffer);
            oracle = Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        finally
        {
            await initial.DisposeAsync();
        }
        client.Capturing = false;
        if (client.SingleCalls != (shape.Ordinary || shape.Batch ? 0 : 1)
            || client.BatchCalls != (shape.Batch ? 1 : 0)
            || client.BatchRequests != (shape.Batch ? 2 : 0))
        {
            throw new InvalidOperationException($"Unexpected dispatch for {Shape}.");
        }
        using (var document = JsonDocument.Parse(oracle))
        {
            var products = document.RootElement.GetProperty("data").GetProperty("products");
            if (products.GetArrayLength() != shape.Count || document.RootElement.TryGetProperty("errors", out _))
            {
                throw new InvalidOperationException($"Invalid result for {Shape}.");
            }
            for (var i = 0; i < shape.Count; i++)
            {
                var id = (i % shape.Unique + 1).ToString();
                if (products[i].GetProperty(shape.Ordinary ? "id" : "name").GetString()
                    != (shape.Ordinary ? id : "Name" + id)
                    || (shape.Batch && document.RootElement.GetProperty("data").GetProperty("categories")[i]
                        .GetProperty("title").GetString() != "Name" + id))
                {
                    throw new InvalidOperationException($"Invalid value for {Shape}[{i}].");
                }
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Shape, oracle, client.SingleCalls, client.BatchCalls, client.BatchRequests }));
        if (warm)
        {
            var start = Stopwatch.GetTimestamp();
            do
            {
                await ExecuteOneAsync();
            }
            while (Stopwatch.GetElapsedTime(start).TotalSeconds < 3);
        }
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public async Task ExecuteAsync()
    {
        var before = _completed;
        for (var i = 0; i < 1000; i++)
        {
            await ExecuteOneAsync();
        }
        if (_completed - before != 1000)
        {
            throw new InvalidOperationException("Incomplete benchmark invocation.");
        }
    }

    private async ValueTask ExecuteOneAsync()
    {
        var result = await _executor.ExecuteAsync(_request).ConfigureAwait(false);
        try
        {
            if (result.ExpectOperationResult().Errors.Count != 0)
            {
                throw new InvalidOperationException("Unexpected execution error.");
            }
        }
        finally
        {
            await result.DisposeAsync().ConfigureAwait(false);
        }
        _completed++;
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }
}
