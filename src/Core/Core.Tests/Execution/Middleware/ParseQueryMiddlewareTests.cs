using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Runtime;
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

            IReadOnlyQueryRequest request = new QueryRequest("{ a }")
                .ToReadOnly();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            );

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask,
                new DefaultQueryParser(),
                new Cache<DocumentNode>(10));

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

            IReadOnlyQueryRequest request = new QueryRequest("{")
                .ToReadOnly();

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                fs => fs.Field.Middleware
            );

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask,
                new DefaultQueryParser(),
                new Cache<DocumentNode>(10));

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
