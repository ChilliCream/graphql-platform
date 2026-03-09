using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IssueAnyTypeDoubleEscapingReproTests
{
    [Fact]
    public async Task AnyType_Output_Should_Not_Double_Escape_String_Escape_Sequences()
    {
        // arrange
        // act
        var result = await new ServiceCollection()
            .AddGraphQLServer()
            .AddJsonTypeConverter()
            .AddQueryType<Query>()
            .ExecuteRequestAsync(
                """
                {
                  foo
                }
                """);

        // assert
        using var json = JsonDocument.Parse(result.ToJson());
        var description = json.RootElement
            .GetProperty("data")
            .GetProperty("foo")
            .GetProperty("description")
            .GetString();

        Assert.Equal("Special char: ü", description);
    }

    public class Query
    {
        [GraphQLType<AnyType>]
        public object Foo => new FooObject("Special char: ü");
    }

    public record FooObject(string Description);
}
