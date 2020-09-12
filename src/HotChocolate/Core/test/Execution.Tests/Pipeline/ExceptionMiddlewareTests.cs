using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Options;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class ExceptionMiddlewareTests
    {
        [Fact]
        public async Task Unexpected_Error()
        {
            // arrange
            var errorHandler = new DefaultErrorHandler(
                Array.Empty<IErrorFilter>(),
                new RequestExecutorOptions());

            var middleware = new ExceptionMiddleware(
                context => throw new Exception("Something is wrong."),
                errorHandler);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupProperty(t => t.Result);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            requestContext.Object.Result.ToJson().MatchSnapshot();
        }

         [Fact]
        public async Task GraphQL_Error()
        {
            // arrange
            var errorHandler = new DefaultErrorHandler(
                Array.Empty<IErrorFilter>(),
                new RequestExecutorOptions());

            var middleware = new ExceptionMiddleware(
                context => throw new GraphQLException("Something is wrong."),
                errorHandler);

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .SetQueryId("a")
                .Create();

            var requestContext = new Mock<IRequestContext>();
            requestContext.SetupProperty(t => t.Result);

            // act
            await middleware.InvokeAsync(requestContext.Object);

            // assert
            requestContext.Object.Result.ToJson().MatchSnapshot();
        }
    }
}
