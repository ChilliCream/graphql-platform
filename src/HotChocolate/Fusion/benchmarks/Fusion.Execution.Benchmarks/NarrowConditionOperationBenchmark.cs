using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NarrowConditionOperationBenchmark
{
    private const int ConditionCount = 48;

    private static readonly string s_documentText = CreateDocument();

    private OperationDefinitionNode _operationDefinition = null!;
    private OperationCompiler _compiler = null!;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithInvocationCount(16384)
                    .WithUnrollFactor(1)
                    .WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _operationDefinition = Utf8GraphQLParser.Parse(s_documentText).Definitions.OfType<OperationDefinitionNode>().First();
        _compiler = await CreateCompilerAsync();
    }

    [Benchmark(Baseline = true)]
    public Operation Compile_Narrow()
        => _compiler.Compile("benchmark", "benchmark", "benchmark", _operationDefinition);

    [Benchmark]
    public Operation Compile_Narrow_BaselineCopy()
        => _compiler.Compile("benchmark", "benchmark", "benchmark", _operationDefinition);



    private static async Task<OperationCompiler> CreateCompilerAsync()
    {
        var services = new ServiceCollection();

        services
            .AddGraphQL("source")
            .AddQueryType(
                descriptor => descriptor
                    .Name("Query")
                    .Field("value")
                    .Type<NonNullType<StringType>>()
                    .Resolve("value"))
            .AddSourceSchemaDefaults();
        services
            .AddGraphQLGateway()
            .AddInMemorySchema("source");

        var executor = await services.BuildGatewayAsync();
        return executor.Schema.Services.GetRequiredService<OperationCompiler>();
    }



    private static string CreateDocument()
    {
        var builder = new StringBuilder("query(");

        for (var i = 0; i < ConditionCount; i++)
        {
            builder.Append($"$v{i}: Boolean! ");
        }

        builder.Append(") {");

        for (var i = 0; i < ConditionCount; i++)
        {
            builder.Append($" f{i}: value @include(if: $v{i})");
        }

        builder.Append(" }");
        return builder.ToString();
    }
}
