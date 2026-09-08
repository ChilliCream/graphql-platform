using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationVariableCoercionMiddlewareTests : FusionTestBase
{
    private const string SchemaText =
        """
        type Query {
          field(input: String!): String
        }
        """;

    private const string OperationText =
        "query test($input: String!) { field(input: $input) }";

    [Fact]
    public async Task Warmup_Request_Skips_Coercion_And_Does_Not_Throw_For_Missing_Required_Variable()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .MarkAsWarmupRequest()
            .Build();

        // act
        var result = await executor.ExecuteAsync(warmupRequest, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(result);
    }

    [Fact]
    public async Task Cost_Validation_Request_Without_Variables_Skips_Coercion()
    {
        // arrange
        IReadOnlyList<IVariableValueCollection>? capturedVariableValues = null;

        var executor = await CreateExecutorAsync(
            builder => builder.UseRequest(
                (_, _) => context =>
                {
                    // capture the variable values right after the coercion middleware ran, then
                    // short-circuit before planning reaches out to a (non-existent) source schema.
                    capturedVariableValues = context.VariableValues;
                    context.Result =
                        new OperationResult(ImmutableOrderedDictionary<string, object?>.Empty.Add("probe", true));
                    return ValueTask.CompletedTask;
                },
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true));

        // a required variable is declared but no value is supplied, which would normally
        // make coercion throw; here it must be skipped because the request only validates cost.
        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(capturedVariableValues);
        Assert.Empty(capturedVariableValues);
    }

    [Fact]
    public async Task Regular_Request_Missing_Required_Variable_Still_Throws()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        var request = OperationRequestBuilder.New()
            .SetDocument(OperationText)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.NotEmpty(operationResult.Errors ?? []);
    }

    private async Task<IRequestExecutor> CreateExecutorAsync(
        Func<IFusionGatewayBuilder, IFusionGatewayBuilder>? configure = null)
    {
        IFusionGatewayBuilder builder = new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline();

        if (configure is not null)
        {
            builder = configure(builder);
        }

        return await builder
            .AddInMemoryConfiguration(ComposeSchemaDocument(SchemaText))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
