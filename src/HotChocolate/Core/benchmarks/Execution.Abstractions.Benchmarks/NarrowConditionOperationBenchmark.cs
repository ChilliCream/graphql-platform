using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NarrowConditionOperationBenchmark
{
    private const int ConditionCount = 48;

    private static readonly string s_documentText = CreateDocument();

    private Schema _schema = null!;
    private DocumentNode _document = null!;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _schema = CreateSchema();
        _document = Utf8GraphQLParser.Parse(s_documentText);
    }

    [Benchmark(Baseline = true)]
    public Operation Compile_Narrow()
        => OperationCompiler.Compile("benchmark", _document, _schema);

    [Benchmark]
    public Operation Compile_Narrow_BaselineCopy()
        => OperationCompiler.Compile("benchmark", _document, _schema);


    private static Schema CreateSchema()
        => SchemaBuilder.New()
            .AddQueryType(
                descriptor => descriptor
                    .Name("Query")
                    .Field("value")
                    .Type<NonNullType<StringType>>()
                    .Resolve("value"))
            .Create();


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
