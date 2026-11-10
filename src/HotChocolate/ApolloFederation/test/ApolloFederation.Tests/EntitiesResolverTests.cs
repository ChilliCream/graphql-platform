using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
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

    [Fact]
    public async Task ResolveAsync_Should_ReturnError_When_RepresentationsExceedLimit()
    {
        // arrange
        var state = new CountedEntityState();

        var executor = await new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddApolloFederation(o => o.MaxAllowedRepresentations = 2)
            .AddType<CountedEntity>()
            .AddQueryType()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
                _entities(representations: [
                    { id: "1", __typename: "CountedEntity" }
                    { id: "2", __typename: "CountedEntity" }
                    { id: "3", __typename: "CountedEntity" }
                ]) {
                    ... on CountedEntity { id }
                }
            }
            """);

        // assert
        Assert.Equal(0, state.Calls);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The maximum number of representations that can be resolved in a single request is 2.",
                  "extensions": {
                    "code": "HC0140"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnError_When_RepresentationsExceedDefaultLimit()
    {
        // arrange
        var state = new CountedEntityState();

        var executor = await new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddApolloFederation()
            .AddType<CountedEntity>()
            .AddQueryType()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            CreateCountedEntitiesRequest(representationCount: 10_001));

        // assert
        var error = Assert.Single(result.ExpectQueryResult().Errors!);
        Assert.Equal(
            "The maximum number of representations that can be resolved in a single request "
            + "is 10,000.",
            error.Message);
        Assert.Equal(0, state.Calls);
    }

    [Fact]
    public async Task ResolveAsync_Should_ResolveRepresentations_When_RepresentationsEqualLimit()
    {
        // arrange
        var state = new CountedEntityState();

        var executor = await new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddApolloFederation(o => o.MaxAllowedRepresentations = 3)
            .AddType<CountedEntity>()
            .AddQueryType()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            CreateCountedEntitiesRequest(representationCount: 3));

        // assert
        Assert.Equal(3, state.Calls);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "_entities": [
                  {
                    "id": "1"
                  },
                  {
                    "id": "2"
                  },
                  {
                    "id": "3"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveAsync_Should_ResolveOneSelection_When_SelectionsExceedLimit()
    {
        // arrange
        var state = new CountedEntityState();

        var executor = await new ServiceCollection()
            .AddSingleton(state)
            .AddGraphQL()
            .AddApolloFederation(o => o.MaxAllowedRepresentations = 3)
            .AddType<CountedEntity>()
            .AddQueryType()
            .BuildRequestExecutorAsync();

        var request = QueryRequestBuilder.New()
            .SetQuery(
                """
                query($representations: [_Any!]!) {
                    a: _entities(representations: $representations) {
                        ... on CountedEntity { id }
                    }
                    b: _entities(representations: $representations) {
                        ... on CountedEntity { id }
                    }
                }
                """)
            .SetVariableValues(
                new Dictionary<string, object?>
                {
                    ["representations"] = CreateRepresentations(1, 2)
                })
            .Create();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        var error = Assert.Single(result.ExpectQueryResult().Errors!);
        Assert.Equal("HC0140", error.Code);
        Assert.Equal(2, state.Calls);
    }

    private static IQueryRequest CreateCountedEntitiesRequest(int representationCount)
        => QueryRequestBuilder.New()
            .SetQuery(
                """
                query($representations: [_Any!]!) {
                    _entities(representations: $representations) {
                        ... on CountedEntity { id }
                    }
                }
                """)
            .SetVariableValues(
                new Dictionary<string, object?>
                {
                    ["representations"] = CreateRepresentations(1, representationCount)
                })
            .Create();

    private static ListValueNode CreateRepresentations(int first, int last)
        => new(
            Enumerable
                .Range(first, last - first + 1)
                .Select(
                    i => new ObjectValueNode(
                        new ObjectFieldNode("__typename", "CountedEntity"),
                        new ObjectFieldNode("id", i.ToString())))
                .ToList<IValueNode>());

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

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public sealed class CountedEntity
    {
        [Key]
        public string Id { get; set; } = null!;

        public static CountedEntity Get(string id, [Service] CountedEntityState state)
        {
            state.Increment();
            return new CountedEntity { Id = id };
        }
    }

    public sealed class CountedEntityState
    {
        private int _calls;

        public int Calls => _calls;

        public void Increment() => Interlocked.Increment(ref _calls);
    }
}
