using System.Threading.Tasks;
using ChilliCream.Testing;
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
            result.Snapshot();
        }
    }
}
