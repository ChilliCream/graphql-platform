using System.Net;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class DocumentParserMiddlewareTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ExecuteSentDocument_When_DocumentIdIsReused()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("foo");
                d.Field("bar").Resolve("bar");
            })
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocumentId("abc")
                .SetDocument("{ foo }")
                .Build(),
            TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocumentId("abc")
                .SetDocument("{ bar }")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "bar": "bar"
              }
            }
            """);
    }

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
