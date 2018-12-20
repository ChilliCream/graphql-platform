using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Resolvers;
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

            var executer = QueryExecutionBuilder
                .New()
                .Use(next => context =>
                {
                    var dict = new OrderedDictionary();
                    dict["done"] = true;

                    context.Result = new QueryResult(dict);

                    return next(context);
                })
                .Build(schema);

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ a }");

            // assert
            result.Snapshot();
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

            var executer = QueryExecutionBuilder
                .New()
                .Use<TestMiddleware>()
                .Build(schema);

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ a }");

            // assert
            result.Snapshot();
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

            var executer = QueryExecutionBuilder
                .New()
                .Use((services, next) => new TestMiddleware(next))
                .Build(schema);

            // act
            IExecutionResult result = await executer.ExecuteAsync("{ a }");

            // assert
            result.Snapshot();
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
                var dict = new OrderedDictionary();
                dict["done"] = true;

                context.Result = new QueryResult(dict);

                return _next(context);
            }
        }
    }
}
