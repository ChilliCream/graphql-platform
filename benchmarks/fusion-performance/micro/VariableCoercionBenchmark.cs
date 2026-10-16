using System.Security.Cryptography;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Performance;

[MemoryDiagnoser]
[Config(typeof(VariableCoercionConfig))]
public class VariableCoercionBenchmark
{
    private FusionSchemaDefinition _schema = null!;
    private IReadOnlyList<VariableDefinitionNode> _definitions = null!;
    private JsonDocument _values = null!;
    private readonly FeatureProvider _context = new();

    [Params(1, 200)]
    public int SuppliedVariables { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var assembly = typeof(VariableCoercionHelper).Assembly.Location;
        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly)));
        Console.WriteLine($"Measured Fusion assembly: {assembly}; SHA256={hash}");
        if (hash != Environment.GetEnvironmentVariable("FUSION_PERF_ASSEMBLY_SHA256"))
        {
            throw ThrowHelper.InvalidFixture();
        }

        _schema = FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(
            File.ReadAllText(Environment.GetEnvironmentVariable("FUSION_PERF_SCHEMA_PATH")!)));
        var definitions = string.Join(',', Enumerable.Range(0, 200).Select(i => $"$v{i}:String"));
        _definitions = Utf8GraphQLParser.Parse($"query Coerce({definitions}){{field}}")
            .Definitions.OfType<OperationDefinitionNode>().Single().VariableDefinitions;
        _values = JsonDocument.Parse(JsonSerializer.Serialize(
            Enumerable.Range(0, SuppliedVariables).ToDictionary(i => $"v{i}", i => $"value{i}")));
        var result = (Dictionary<string, VariableValue>)Coerce();
        if (result.Count != SuppliedVariables || ((StringValueNode)result["v0"].Value).Value != "value0")
        {
            throw ThrowHelper.InvalidFixture();
        }
    }

    [Benchmark]
    public object Coerce()
    {
        if (!VariableCoercionHelper.TryCoerceVariableValues(
            _context, _schema, _definitions, _values.RootElement, false, out var result, out _))
        {
            throw ThrowHelper.InvalidFixture();
        }

        return result;
    }

    [GlobalCleanup]
    public void Cleanup() => _values.Dispose();

    private sealed class FeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}

public sealed class VariableCoercionConfig : ManualConfig
{
    public VariableCoercionConfig()
    {
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core10_0)
            .WithGcServer(true)
            .WithGcConcurrent(true)
            .WithEnvironmentVariable("DOTNET_GCDynamicAdaptationMode", "0")
            .WithLaunchCount(3)
            .WithWarmupCount(5)
            .WithIterationCount(15));
    }
}

internal static class ThrowHelper
{
    internal static InvalidOperationException InvalidFixture()
        => new("The coercion benchmark must use the expected assembly and preserve every supplied value.");
}
