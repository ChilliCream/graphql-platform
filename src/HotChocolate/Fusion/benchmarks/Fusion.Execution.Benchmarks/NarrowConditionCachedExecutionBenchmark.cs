using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NarrowConditionCachedExecutionBenchmark
{
    private const int ConditionCount = 48;

    private static readonly string s_documentText = CreateDocument();
    private static readonly IReadOnlyDictionary<string, object> s_variables = CreateVariables();

    private FusionSchemaDefinition _schema = null!;
    private OperationDefinitionNode _operationDefinition = null!;
    private OperationCompiler _compiler = null!;
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
        _operationDefinition = Utf8GraphQLParser.Parse(s_documentText).Definitions.OfType<OperationDefinitionNode>().First();
        _compiler = new OperationCompiler(
            _schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        _executor = await CreateExecutorAsync();
        _request = OperationRequestBuilder.New()
            .SetDocument(s_documentText)
            .SetVariableValues(s_variables)
            .Build();

        await VerifyCachedExecutionAsync();
    }


    [Benchmark(Baseline = true)]
    public Task<ExecutionResultKind> Execute_Cached_Narrow()
        => ExecuteAsync();

    [Benchmark]
    public Task<ExecutionResultKind> Execute_Cached_Narrow_BaselineCopy()
        => ExecuteAsync();

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

    private static async Task<IRequestExecutor> CreateExecutorAsync()
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

        return await services.BuildGatewayAsync();
    }

    private static FusionSchemaDefinition CreateSchema()
    {
        var result = new SchemaComposer(
            [
                new SourceSchemaText(
                    "source",
                    """
                    type Query {
                      value: String!
                    }
                    """)
            ],
            new SchemaComposerOptions(),
            new CompositionLog()).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private static IReadOnlyDictionary<string, object> CreateVariables()
    {
        var variables = new Dictionary<string, object>(ConditionCount);

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
