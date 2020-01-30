using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ExecutionFieldMiddleware
    {
        [Fact]
        public async Task ExecuteFieldWithExecutionMiddleware()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { a: String }",
                c => c.Use(next => context =>
                {
                    context.Result = "a";
                    return next.Invoke(context);
                }));

            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.UseDefaultPipeline()
                    .UseField(next => async context =>
                    {
                        await next.Invoke(context);

                        if (context.Result is string s)
                        {
                            context.Result = s.ToUpperInvariant();
                        }
                    }));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithExecutionClassMiddleware()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { a: String }",
                c => c.Use(next => context =>
                {
                    context.Result = "a";
                    return next.Invoke(context);
                }));

            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.UseDefaultPipeline().UseField<ToUpperMiddleware>());

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteFieldWithExecutionClassMiddlewareWithFactory()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { a: String }",
                c => c.Use(next => context =>
                {
                    context.Result = "a";
                    return next.Invoke(context);
                }));

            IQueryExecutor executor = schema.MakeExecutable(b =>
                b.UseDefaultPipeline()
                    .UseField((sp, next) => new ToUpperMiddleware(next)));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.MatchSnapshot();
        }

        public class ToUpperMiddleware
        {
            private readonly FieldDelegate _next;

            public ToUpperMiddleware(FieldDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(IMiddlewareContext context)
            {
                await _next.Invoke(context);

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            }
        }
    }
}
