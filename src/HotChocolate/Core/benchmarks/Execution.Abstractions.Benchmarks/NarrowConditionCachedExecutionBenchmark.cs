using System.Collections.Generic;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class NarrowConditionCachedExecutionBenchmark
{
    private const int ConditionCount = 48;

    private static readonly string s_documentText = CreateDocument();
    private static readonly string s_expectedResult = CreateExpectedResult();
    private static readonly IReadOnlyDictionary<string, object?> s_variables = CreateVariables();
    private readonly CacheDiagnosticListener _diagnosticListener = new();

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
        _executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDiagnosticEventListener(_ => _diagnosticListener)
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
    public Task<ExecutionResultKind> Execute_Cached_Narrow()
        => ExecuteAsync();

    [Benchmark]
    public Task<ExecutionResultKind> Execute_Cached_Narrow_BaselineCopy()
        => ExecuteAsync();


    private async Task VerifyCachedExecutionAsync()
    {
        _diagnosticListener.Reset();
        await VerifyExpectedResultAsync();

        if (_diagnosticListener.RetrievedOperationCount != 0)
        {
            throw new InvalidOperationException("The first narrow request unexpectedly retrieved a compiled operation.");
        }

        await VerifyExpectedResultAsync();

        if (_diagnosticListener.RetrievedOperationCount != 1)
        {
            throw new InvalidOperationException("The second narrow request did not retrieve the compiled operation.");
        }
    }

    private async Task VerifyExpectedResultAsync()
    {
        await using var result = await _executor.ExecuteAsync(_request);

        if (result is not OperationResult operationResult
            || operationResult.Errors.Count != 0
            || !string.Equals(
                result.ToJson(withIndentations: false),
                s_expectedResult,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The narrow request did not return the expected 48-field payload.");
        }
    }
    private async Task<ExecutionResultKind> ExecuteAsync()
    {
        await using var result = await _executor.ExecuteAsync(_request);
        return result.Kind;
    }


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
    private static string CreateExpectedResult()
    {
        var builder = new StringBuilder("{\"data\":{");

        for (var i = 0; i < ConditionCount; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append($"\"f{i}\":\"value\"");
        }

        builder.Append("}}");
        return builder.ToString();
    }

    private sealed class CacheDiagnosticListener : ExecutionDiagnosticEventListener
    {
        private int _retrievedOperationCount;

        public int RetrievedOperationCount => Volatile.Read(ref _retrievedOperationCount);

        public override void RetrievedOperationFromCache(RequestContext context)
            => Interlocked.Increment(ref _retrievedOperationCount);

        public void Reset()
            => Interlocked.Exchange(ref _retrievedOperationCount, 0);
    }
}
