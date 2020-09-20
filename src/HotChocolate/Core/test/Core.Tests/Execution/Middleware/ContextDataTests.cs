using System.Collections.Generic;
using System.Threading.Tasks;
using Snapshooter.Xunit;
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
                    return default(ValueTask);
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
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetProperty("request", "123")
                    .Create());

            // assert
            Assert.True(allDataIsPassedAlong);
            result.MatchSnapshot();
        }
    }
}
