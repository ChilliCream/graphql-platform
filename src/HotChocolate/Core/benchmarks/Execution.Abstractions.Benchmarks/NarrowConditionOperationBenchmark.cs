using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NarrowConditionOperationBenchmark
{
    private const int ConditionCount = 48;

    private static readonly string s_documentText = CreateDocument();
    private static readonly IReadOnlyDictionary<string, object?> s_variables = CreateVariables();

    private Schema _schema = null!;
    private DocumentNode _document = null!;
    private IRequestExecutor _executor = null!;
    private IOperationRequest _request = null!;

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
    public async Task SetupAsync()
    {
        _schema = CreateSchema();
        _document = Utf8GraphQLParser.Parse(s_documentText);
        _executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                descriptor => descriptor
                    .Name("Query")
                    .Field("value")
                    .Type<NonNullType<StringType>>()
                    .Resolve("value"))
            .BuildRequestExecutorAsync();
        _request = OperationRequestBuilder.New()
            .SetDocument(s_documentText)
            .SetVariableValues(s_variables)
            .Build();

        await VerifyCachedExecutionAsync();
    }

    [Benchmark(Baseline = true)]
    public Operation Compile_Narrow()
        => OperationCompiler.Compile("benchmark", _document, _schema);

    [Benchmark]
    public Operation Compile_Narrow_BaselineCopy()
        => OperationCompiler.Compile("benchmark", _document, _schema);

    private async Task VerifyCachedExecutionAsync()
    {
        if (await ExecuteAsync() is not ExecutionResultKind.SingleResult
            || await ExecuteAsync() is not ExecutionResultKind.SingleResult)
        {
            throw new InvalidOperationException("The narrow request did not return an operation result.");
        }
    }

    private async Task<ExecutionResultKind> ExecuteAsync()
    {
        await using var result = await _executor.ExecuteAsync(_request);
        return result.Kind;
    }

    private static Schema CreateSchema()
        => SchemaBuilder.New()
            .AddQueryType(
                descriptor => descriptor
                    .Name("Query")
                    .Field("value")
                    .Type<NonNullType<StringType>>()
                    .Resolve("value"))
            .Create();

    private static IReadOnlyDictionary<string, object?> CreateVariables()
    {
        var variables = new Dictionary<string, object?>(ConditionCount);

        for (var i = 0; i < ConditionCount; i++)
        {
            variables.Add($"v{i}", true);
        }

        return variables;
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
