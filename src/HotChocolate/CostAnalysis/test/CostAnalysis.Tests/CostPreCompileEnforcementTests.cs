using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Cost analysis now runs before the operation cache and the operation compiler, so a
/// cost-rejected request must never coalesce into the compiler's single-flight and must
/// never create a prepared-operation cache entry.
/// </summary>
public sealed class CostPreCompileEnforcementTests
{
    private const string Schema =
        """
        type Query {
            examples(limit: Int! @cost(weight: "2.0")): [Example!]!
                @cost(weight: "3.0") @listSize(slicingArguments: ["limit"])
        }

        type Example @cost(weight: "4.0") {
            exampleField: Boolean!
        }
        """;

    private const string Operation =
        """
        query {
            examples(limit: 10) {
                exampleField
            }
        }
        """;

    [Fact]
    public async Task Rejected_Request_Leaves_No_PreparedOperationCache_Entry_And_Never_Compiles()
    {
        // arrange
        var compileCount = 0;

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxTypeCost = 1)
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var operationCache = requestExecutor.Schema.Services.GetRequiredService<IPreparedOperationCache>();
        var request = OperationRequestBuilder.New().SetDocument(Operation).Build();

        // act
        var result = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEmpty(result.ExpectOperationResult().Errors);
        Assert.Equal(0, operationCache.Count);
        Assert.Equal(0, Volatile.Read(ref compileCount));
    }

    [Fact]
    public async Task Concurrent_Rejected_Requests_Never_Coalesce_And_Never_Compile()
    {
        // arrange: a burst of identical, cost-rejected requests must not race into the
        // operation cache's single-flight leader/follower coalescing, because cost
        // enforcement rejects every one of them before the operation cache ever sees them.
        var compileCount = 0;

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxTypeCost = 1)
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var operationCache = requestExecutor.Schema.Services.GetRequiredService<IPreparedOperationCache>();

        // act
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8)
                .Select(_ => requestExecutor.ExecuteAsync(
                    Operation,
                    TestContext.Current.CancellationToken)));

        // assert
        Assert.All(results, r => Assert.NotEmpty(r.ExpectOperationResult().Errors));
        Assert.Equal(0, operationCache.Count);
        Assert.Equal(0, Volatile.Read(ref compileCount));
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "examples", _ => Array.Empty<object>())
            .AddResolver("Example", "exampleField", _ => false)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);

    private sealed class CompileCountListener(Action onCompile) : ExecutionDiagnosticEventListener
    {
        public override IDisposable CompileOperation(RequestContext context)
        {
            onCompile();
            return EmptyScope;
        }
    }
}
