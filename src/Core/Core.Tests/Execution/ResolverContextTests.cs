using System.Threading.Tasks;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Execution
{
    public class ResolverContextTests
    {
        [Fact]
        public async Task AccessVariables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo(bar: String) : String }")
                .AddResolver("Query", "foo", ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query abc($abc: String){ foo(bar: $abc) }")
                .SetVariableValue("abc", "def")
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AccessVariables_Failes_When_Variable_Not_Exists()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo(bar: String) : String }")
                .AddResolver("Query", "foo", ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query abc($def: String){ foo(bar: $def) }")
                .SetVariableValue("def", "ghi")
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }
    }
}
