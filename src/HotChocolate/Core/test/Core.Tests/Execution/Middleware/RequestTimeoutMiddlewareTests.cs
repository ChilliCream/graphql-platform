using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Configuration;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class RequestTimeoutMiddlewareTests
    {
        [Fact]
        public async Task InnerMiddlewareTimesOut()
        {
            // arrange
            var options = new Mock<IRequestTimeoutOptionsAccessor>();

            options
                .SetupGet(o => o.ExecutionTimeout)
                .Returns(TimeSpan.FromMilliseconds(10));

            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            var middleware = new RequestTimeoutMiddleware(
                c => Task.Delay(1000, c.RequestAborted),
                options.Object);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Result);
            Assert.IsType<TaskCanceledException>(context.Exception);
            context.Result.MatchSnapshot();
        }
    }
}
