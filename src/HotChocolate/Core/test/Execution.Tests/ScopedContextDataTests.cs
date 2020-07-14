using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScopedContextDataTests
    {
        [Fact]
        public async Task ScopedContextDataIsPassedAllongCorrectly()
        {
            // arrange
            ISchema schema = Schema.Create(
                @"
                type Query {
                    root: Level1
                }

                type Level1 {
                    a: Level2
                    b: Level2
                }

                type Level2
                {
                    foo: String
                }
                ",
                c => c.Use(next => context =>
                {
                    if (context.ScopedContextData
                        .TryGetValue("field", out object o)
                        && o is string s)
                    {
                        s += "/" + context.Field.Name;
                    }
                    else
                    {
                        s = "./" + context.Field.Name;
                    }

                    context.ScopedContextData = context.ScopedContextData
                        .SetItem("field", s);

                    context.Result = s;

                    return default(ValueTask);
                }));

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ root { a { foo } b { foo } } }");

            // assert
            result.MatchSnapshot();
        }
    }
}
