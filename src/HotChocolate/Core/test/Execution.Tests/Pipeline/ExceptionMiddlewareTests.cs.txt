using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Options;
using Moq;

namespace HotChocolate.Execution.Pipeline;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task Unexpected_Error()
    {
        // arrange
        var errorHandler = new DefaultErrorHandler(
            Array.Empty<IErrorFilter>(),
            new RequestExecutorOptions());

        var middleware = ExceptionMiddleware.Create(
            _ => throw new Exception("Something is wrong."),
            errorHandler);

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupProperty(t => t.Result);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        requestContext.Object.Result!.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task GraphQL_Error()
    {
        // arrange
        var errorHandler = new DefaultErrorHandler(
            Array.Empty<IErrorFilter>(),
            new RequestExecutorOptions());

        var middleware = ExceptionMiddleware.Create(
            _ => throw new GraphQLException("Something is wrong."),
            errorHandler);

        var requestContext = new Mock<IRequestContext>();
        requestContext.SetupProperty(t => t.Result);

        // act
        await middleware.InvokeAsync(requestContext.Object);

        // assert
        requestContext.Object.Result!.ToJson().MatchSnapshot();
    }
}
