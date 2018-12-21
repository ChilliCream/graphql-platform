using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;
using Moq;
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
            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();
            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);
            var middleware = new RequestTimeoutMiddleware(
                c => Task.Delay(1000, c.RequestAborted),
                options.Object);

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            await Assert.ThrowsAsync<TaskCanceledException>(func);
        }
    }
}
