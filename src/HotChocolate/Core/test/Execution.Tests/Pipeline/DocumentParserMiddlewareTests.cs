using System.Net;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class DocumentParserMiddlewareTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ProposeBadRequest_When_DocumentCannotBeParsed()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ foo", TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        var error = Assert.Single(operationResult.Errors);
        Assert.Equal("HC0014", error.Code);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            operationResult.ContextData[ExecutionContextData.HttpStatusCode]);
    }
}
