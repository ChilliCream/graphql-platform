using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class EntitiesResolverTests
{
    [Fact]
    public async Task ResolveAsync_Should_AwaitStartedEntityResolvers_When_LaterRepresentationHasNoResolver()
    {
        // arrange
        var state = new SlowEntityState();

        var executor = await new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddApolloFederation()
            .AddType<SlowEntity>()
            .AddQueryType()
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(
            """
            query {
                _entities(representations: [
                    { id: "1", __typename: "SlowEntity" }
                    { id: "2", __typename: "Unknown" }
                ]) {
                    ... on SlowEntity { id }
                }
            }
            """);

        // assert
        Assert.True(state.Completed);
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public sealed class SlowEntity
    {
        [Key]
        public string Id { get; set; } = null!;

        public static async Task<SlowEntity> Get(string id, [Service] SlowEntityState state)
        {
            await Task.Delay(250);
            state.Completed = true;
            return new SlowEntity { Id = id };
        }
    }

    public sealed class SlowEntityState
    {
        public bool Completed { get; set; }
    }
}
