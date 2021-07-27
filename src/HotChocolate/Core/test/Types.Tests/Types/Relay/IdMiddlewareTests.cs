using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class IdMiddlewareTests
    {
        [Fact]
        public async Task ExecuteQueryThatReturnsId_IdShouldBeOpaque()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<SomeQuery>()
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ id string }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class SomeQuery
        {
            [GlobalId]
            public string GetId() => "Hello";

            public string GetString() => "Hello";
        }
    }
}
