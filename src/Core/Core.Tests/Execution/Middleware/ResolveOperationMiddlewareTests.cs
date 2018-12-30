using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace Core.Tests.Execution.Middleware
{
    public class ResolveOperationMiddlewareTests
    {
        [Fact]
        public async Task OperationIsResolved()
        {
            // arrange
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);
            context.Document = Parser.Default.Parse(request.Query);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            await middleware.InvokeAsync(context);

            // assert
            Assert.NotNull(context.Operation);
            Assert.Equal("a", context.Operation.Name);
        }

        [Fact]
        public async Task TwoOperations_ShortHand_QueryException()
        {
            // arrange
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest("{ a } query a { a }").ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);
            context.Document = Parser.Default.Parse(request.Query);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            QueryException exception =
                await Assert.ThrowsAsync<QueryException>(func);
            Assert.Equal(
                "Only queries that contain one operation can be executed " +
                "without specifying the opartion name.",
                exception.Message);
        }

        [Fact]
        public async Task TwoOperations_WrongOperationName_QueryException()
        {
            // arrange
            Schema schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            var request = new QueryRequest(
                "query a { a } query b { a }", "c")
                .ToReadOnly();

            var context = new QueryContext(
                schema, new EmptyServiceProvider(), request);
            context.Document = Parser.Default.Parse(request.Query);

            var middleware = new ResolveOperationMiddleware(
                c => Task.CompletedTask, null);

            // act
            Func<Task> func = () => middleware.InvokeAsync(context);

            // assert
            QueryException exception =
                await Assert.ThrowsAsync<QueryException>(func);
            Assert.Equal(
                "The specified operation `c` does not exist.",
                exception.Message);
        }
    }
}
