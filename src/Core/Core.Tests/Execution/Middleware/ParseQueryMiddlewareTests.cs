using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Instrumentation;
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

            IReadOnlyQueryRequest request = new QueryRequest("{ a }");

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            var diagnostics = new QueryExecutionDiagnostics(
                new DiagnosticListener("Foo"),
                new IDiagnosticObserver[0]);

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask,
                new DefaultQueryParser(),
                new Cache<DocumentNode>(10),
                diagnostics);

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

            IReadOnlyQueryRequest request = new QueryRequest("{");

            var context = new QueryContext
            (
                schema,
                MiddlewareTools.CreateEmptyRequestServiceScope(),
                request,
                (f, s) => f.Middleware
            );

            var diagnostics = new QueryExecutionDiagnostics(
                new DiagnosticListener("Foo"),
                new IDiagnosticObserver[0]);

            var middleware = new ParseQueryMiddleware(
                c => Task.CompletedTask,
                new DefaultQueryParser(),
                new Cache<DocumentNode>(10),
                diagnostics);

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
