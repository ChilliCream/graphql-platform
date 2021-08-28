using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScopedContextDataTests
    {
        [Fact]
        public async Task ScopedContextDataIsPassedAlongCorrectly()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        root: Level1
                    }

                    type Level1 {
                        a: Level2
                        b: Level2
                    }

                    type Level2
                    {
                        foo: String
                    }")
                .Use(_ => context =>
                {
                    if (context.ScopedContextData.TryGetValue("field", out var o) &&
                        o is string s)
                    {
                        s += "/" + context.Selection.Field.Name;
                    }
                    else
                    {
                        s = "./" + context.Selection.Field.Name;
                    }

                    context.ScopedContextData = context.ScopedContextData
                        .SetItem("field", s);

                    context.Result = s;

                    return default;
                })
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ root { a { foo } b { foo } } }");

            // assert
            result.MatchSnapshot();
        }
    }
}
