using System.Net;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class ExceptionMiddlewareTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ProposeInternalServerError_When_MiddlewareThrows()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseExceptions()
            .UseRequest(_ => context => throw new InvalidOperationException("Unexpected."))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Single(operationResult.Errors);
        Assert.Equal(
            HttpStatusCode.InternalServerError,
            operationResult.ContextData[ExecutionContextData.HttpStatusCode]);
    }
}
