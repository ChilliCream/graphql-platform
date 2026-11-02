using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class MutationRootOrderTests : FusionTestBase
{
    private const string ConditionedMutation =
        """
        mutation Conditioned($counter: ID!, $a: Boolean!, $b: Boolean!) {
          first: bump(counter: $counter, by: 1) @include(if: $a)
          second: add(counter: $counter, by: 10) @include(if: $b)
          ...Again @include(if: $b)
          last: bump(counter: $counter, by: 100)
        }

        fragment Again on Mutation {
          first: bump(counter: $counter, by: 1)
        }
        """;

    private const string ConditionedQuery =
        """
        query Conditioned($a: Boolean!, $b: Boolean!) {
          items {
            id
            name
            detail @include(if: $a) {
              summary
            }
            ...Rated @include(if: $b)
          }
        }

        fragment Rated on Item {
          detail {
            rating
          }
        }
        """;

    [Theory]
    [InlineData(
        true,
        true,
        """
        {
          "data": {
            "first": 1,
            "second": 11,
            "last": 111
          }
        }
        """,
        new[] { "catalog: bump(1) = 1", "details: add(10) = 11", "catalog: bump(100) = 111" })]
    [InlineData(
        true,
        false,
        """
        {
          "data": {
            "first": 1,
            "last": 101
          }
        }
        """,
        new[] { "catalog: bump(1) = 1", "catalog: bump(100) = 101" })]
    [InlineData(
        false,
        true,
        """
        {
          "data": {
            "first": 11,
            "second": 10,
            "last": 111
          }
        }
        """,
        new[] { "details: add(10) = 10", "catalog: bump(1) = 11", "catalog: bump(100) = 111" })]
    [InlineData(
        false,
        false,
        """
        {
          "data": {
            "last": 100
          }
        }
        """,
        new[] { "catalog: bump(100) = 100" })]
    public async Task Execute_Should_RunEachResponseNameOnceInFirstIncludedOrder_When_RootsAreConditioned(
        bool a,
        bool b,
        string expectedResponse,
        string[] expectedEffects)
    {
        // arrange
        var ledger = new SharedCounter();
        using var gateway = await CreateGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            ConditionedMutation,
            variables: new Dictionary<string, object?> { ["counter"] = "conditioned", ["a"] = a, ["b"] = b });

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(expectedResponse);
        ledger.Effects.MatchInlineSnapshots(expectedEffects);
    }

    [Theory]
    [InlineData(
        true,
        true,
        """
        {
          "data": {
            "items": [
              {
                "id": "i1",
                "name": "anvil",
                "detail": {
                  "summary": "forged steel",
                  "rating": 4
                }
              },
              {
                "id": "i2",
                "name": "bucket",
                "detail": {
                  "summary": "galvanized",
                  "rating": 3
                }
              }
            ]
          }
        }
        """)]
    [InlineData(
        true,
        false,
        """
        {
          "data": {
            "items": [
              {
                "id": "i1",
                "name": "anvil",
                "detail": {
                  "summary": "forged steel"
                }
              },
              {
                "id": "i2",
                "name": "bucket",
                "detail": {
                  "summary": "galvanized"
                }
              }
            ]
          }
        }
        """)]
    [InlineData(
        false,
        true,
        """
        {
          "data": {
            "items": [
              {
                "id": "i1",
                "name": "anvil",
                "detail": {
                  "rating": 4
                }
              },
              {
                "id": "i2",
                "name": "bucket",
                "detail": {
                  "rating": 3
                }
              }
            ]
          }
        }
        """)]
    [InlineData(
        false,
        false,
        """
        {
          "data": {
            "items": [
              {
                "id": "i1",
                "name": "anvil"
              },
              {
                "id": "i2",
                "name": "bucket"
              }
            ]
          }
        }
        """)]
    public async Task Execute_Should_MergeIncludedOccurrences_When_QueryRepeatsResponseNameUnderConditions(
        bool a,
        bool b,
        string expectedResponse)
    {
        // arrange
        using var gateway = await CreateGatewayAsync(new SharedCounter());
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            ConditionedQuery,
            variables: new Dictionary<string, object?> { ["a"] = a, ["b"] = b });

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(expectedResponse);
    }

    private Task<Gateway> CreateGatewayAsync(SharedCounter counter)
    {
        var catalog = CreateSourceSchema(
            "catalog",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<Catalog.Query>()
                .AddMutationType<Catalog.Mutation>(),
            services => services.AddSingleton(counter));
        var details = CreateSourceSchema(
            "details",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddMutationType<Details.Mutation>()
                .AddType<Details.Item>(),
            services => services.AddSingleton(counter));

        return CreateCompositeSchemaAsync(
            [("catalog", catalog), ("details", details)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
    }

    // Holds the counter both source schemas add to and records every effect in order.
    public sealed class SharedCounter
    {
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private readonly Dictionary<string, int> _counters = [];
        private readonly List<string> _effects = [];

        public IReadOnlyList<string> Effects
        {
            get
            {
                lock (_sync)
                {
                    return [.. _effects];
                }
            }
        }

        public int Add(string effect, string counter, int by)
        {
            lock (_sync)
            {
                var total = _counters.GetValueOrDefault(counter) + by;
                _counters[counter] = total;
                _effects.Add($"{effect}({by}) = {total}");
                return total;
            }
        }
    }

    public static class Catalog
    {
        public sealed class Query
        {
            public List<Item?>? GetItems() => [new Item("i1", "anvil"), new Item("i2", "bucket")];
        }

        public sealed class Mutation
        {
            public int? Bump(
                [GraphQLType(typeof(NonNullType<IdType>))] string counter,
                int by,
                [Service] SharedCounter shared)
                => shared.Add("catalog: bump", counter, by);
        }

        [Key("id")]
        public sealed class Item(string id, string? name)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;

            public string? Name { get; } = name;

            [ReferenceResolver]
            public static Item ResolveReference(string id) => new(id, id == "i1" ? "anvil" : "bucket");
        }
    }

    public static class Details
    {
        public sealed class Mutation
        {
            public int? Add(
                [GraphQLType(typeof(NonNullType<IdType>))] string counter,
                int by,
                [Service] SharedCounter shared)
                => shared.Add("details: add", counter, by);
        }

        [Key("id")]
        public sealed class Item(string id)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;

            public Detail? GetDetail()
                => Id == "i1" ? new Detail("forged steel", 4) : new Detail("galvanized", 3);

            [ReferenceResolver]
            public static Item ResolveReference(string id) => new(id);
        }

        public sealed record Detail(string? Summary, int? Rating);
    }
}
