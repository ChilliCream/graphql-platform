using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class RequestTimeoutMiddlewareTests
    {
        [Fact]
        public async Task InnerMiddlewareTimesOut()
        {
            // arrange
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.Options.ExecutionTimeout = TimeSpan.FromMilliseconds(10);
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("{ a }").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);

            var middleware = new RequestTimeoutMiddleware(
                c => Task.Delay(1000, c.RequestAborted));

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            await Assert.ThrowsAsync<TaskCanceledException>(func);
        }
    }
}
