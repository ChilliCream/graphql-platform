using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Relay;

public class IdMiddlewareTests
{
    [Fact]
    public async Task ExecuteQueryThatReturnsId_IdShouldBeOpaque()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<SomeQuery>()
            .AddGlobalObjectIdentification(false)
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ id string }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class SomeQuery
    {
        [ID]
        public string GetId() => "Hello";

        public string GetString() => "Hello";
    }
}
