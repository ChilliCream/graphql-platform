using HotChocolate.Execution.Pipeline;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public sealed class OperationIdAccessorTests
{
    [Fact]
    public async Task GetOperationId_Before_Variable_Coercion_Stage_Is_Reused_By_Later_Stages()
    {
        // arrange
        string? idBeforeCoercion = null;
        string? idAfterOperationCache = null;
        string? compiledOperationId = null;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    // Resolving the normalized document here runs before variable coercion,
                    // the operation cache and the operation compiler stages have had a
                    // chance to compute and store the operation id. Getting the normalized
                    // document must succeed rather than throw.
                    context.GetNormalizedDocument();
                    idBeforeCoercion = context.GetOperationId();
                    await next(context);
                },
                key: "BeforeCoercion",
                before: WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => async context =>
                {
                    idAfterOperationCache = context.GetOperationId();
                    await next(context);
                },
                key: "AfterOperationCache",
                before: WellKnownRequestMiddleware.OperationCompilerMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => async context =>
                {
                    compiledOperationId = context.GetOperation().Id;
                    await next(context);
                },
                key: "AfterOperationCompiler",
                before: WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.NotNull(idBeforeCoercion);
        Assert.Equal(idBeforeCoercion, idAfterOperationCache);
        Assert.Equal(idBeforeCoercion, compiledOperationId);
    }
}
