using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScopedContextDataTests
    {
        [Fact]
        public async Task ScopedContextDataIsPassedAllongCorrectly()
        {
            // arrange
            bool allDataIsPassedAlong = false;

            ISchema schema = Schema.Create(
                "type Query { nested: Nested }  type Nested { foo: String }",
                c => c.Use(next => context =>
                {
                if (context.ScopedContextData.ContainsKey("field"))
                {
                    allDataIsPassedAlong = true;
                    context.Result = "123";
                } 
                else
                {
                    context.ScopedContextData = context.ScopedContextData.Add("field", "abc");
                    context.Result = new { foo = "123"};
                }
                    
                    return Task.CompletedTask;
                }));

            IQueryExecutor executor = schema.MakeExecutable(
                b => b.UseDefaultPipeline()
                   );

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest("{ nested { foo } }"));

            // assert
            Assert.True(allDataIsPassedAlong);
        }
    }
}
