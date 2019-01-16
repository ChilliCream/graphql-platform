using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Execution
{
    public class ContextDataTests
    {
        [Fact]
        public async Task ContextDataIsPassedAllongCorrectly()
        {
            // arrange
            bool allDataIsPassedAlong = false;

            ISchema schema = Schema.Create(
                "type Query { foo: String }",
                c => c.Use(next => context =>
                {
                    context.ContextData["field"] = "abc";
                    context.Result = context.ContextData["request"];
                    return Task.CompletedTask;
                }));

            IQueryExecutor executor = schema.MakeExecutable(
                b => b.UseDefaultPipeline()
                    .Use(next => context =>
                    {
                        if (context.ContextData.ContainsKey("request")
                            && context.ContextData.ContainsKey("field"))
                        {
                            allDataIsPassedAlong = true;
                        }
                        return Task.CompletedTask;
                    }));

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest("{ foo }")
                {
                    Properties = new Dictionary<string, object>
                    {
                        { "request", "123" }
                    }
                });

            // assert
            Assert.True(allDataIsPassedAlong);
            result.Snapshot();
        }
    }
}
