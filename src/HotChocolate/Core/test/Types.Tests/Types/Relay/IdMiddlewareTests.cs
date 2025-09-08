using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class IdMiddlewareTests
{
    [Fact]
    public async Task ExecuteQueryThatReturnsId_IdShouldBeOpaque()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<SomeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync("{ id string }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_ParameterWithoutExplicitType_Should_NotBeValidated()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<SomeQuery>()
            .AddMutationType<Mutation>()
            .AddGlobalObjectIdentification(false)
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation {
                do(input: { id: "RXhhbXBsZTp0ZXN0" }) {
                    string
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    public class SomeQuery
    {
        [ID]
        public string GetId() => "Hello";

        public string GetString() => "Hello";
    }

    public class Mutation
    {
        public string Do([ID] string id) => id;
    }
}
