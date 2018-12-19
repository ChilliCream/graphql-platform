using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class ParseQueryMiddlewareTests
    {
        [Fact]
        public async Task ParseQueryMiddleware_ValidQuery_DocumentIsSet()
        {
            // arrange
            Schema schema = CreateSchema();

            var request = new QueryRequest("{ a }").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask, null, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Document);
            context.Document.Snapshot();
        }

        [Fact]
        public Task ParseQueryMiddleware_InvalidQuery_DocumentNull()
        {
            // arrange
            Schema schema = CreateSchema();

            var request = new QueryRequest("{").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask, null, null);

            // act
            Func<Task> invoke = () => middleware.InvokeAsync(context);

            // assert
            return Assert.ThrowsAsync<SyntaxException>(invoke);
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });
        }
    }
}
