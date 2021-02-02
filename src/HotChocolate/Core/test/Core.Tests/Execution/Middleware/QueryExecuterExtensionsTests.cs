using System.Collections.Generic;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class MiddlewareExtensionTests
    {
        [Fact]
        public async Task UseDelegateMiddleware()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            IQueryExecutor executor = QueryExecutionBuilder
                .New()
                .Use(next => context =>
                {
                    var queryResult =
                        QueryResultBuilder.New()
                            .SetData(new Dictionary<string, object> { { "done", true } })
                            .Create();
                    context.Result = queryResult;

                    return next(context);
                })
                .Build(schema);

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UseClassMiddleware()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            IQueryExecutor executor = QueryExecutionBuilder
                .New()
                .Use<TestMiddleware>()
                .Build(schema);

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UseClassMiddlewareWithFactory()
        {
            // arrange
            var schema = Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });

            IQueryExecutor executor = QueryExecutionBuilder
                .New()
                .Use((services, next) => new TestMiddleware(next))
                .Build(schema);

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        public class TestMiddleware
        {
            private readonly QueryDelegate _next;

            public TestMiddleware(QueryDelegate next)
            {
                _next = next;
            }

            public Task InvokeAsync(IQueryContext context)
            {
                context.Result = QueryResultBuilder.New()
                    .SetData(new Dictionary<string, object> { { "done", true } })
                    .Create();

                return _next(context);
            }
        }
    }
}
