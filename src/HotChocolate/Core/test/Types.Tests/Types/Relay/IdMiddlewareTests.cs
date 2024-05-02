using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
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
        var result = await executor.ExecuteAsync("""
            mutation {
                do(input: { id: "RXhhbXBsZTp0ZXN0" }) {
                    string
                }
            }
        """);

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class SomeQuery
    {
        [ID]
        public string GetId() => "Hello";

        public string GetString() => "Hello";
    }

    public class Mutation
    {
        [UseMutationConvention]
        public string Do([ID] string id) => id;
    }
}
