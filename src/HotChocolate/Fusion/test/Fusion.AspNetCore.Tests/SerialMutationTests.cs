using System.Net.Http.Headers;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class SerialMutationTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_CompleteEachRootWithItsEntityLookup_When_ApolloRootsBumpOneCounter()
    {
        // arrange
        var ledger = new CounterLedger();
        using var gateway = await CreateApolloGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "value": 1
                },
                "b": {
                  "value": 11
                }
              }
            }
            """);
        ledger.Events.MatchInlineSnapshots(
            [
                "ledger: bump(serial, 1)",
                "reader: value(serial) = 1",
                "ledger: bump(serial, 10)",
                "reader: value(serial) = 11"
            ]);
    }

    [Fact]
    public async Task Execute_Should_CompleteEachRootWithItsLookup_When_CompositeRootsBumpOneCounter()
    {
        // arrange
        var ledger = new CounterLedger();
        using var gateway = await CreateCompositeGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "value": 1
                },
                "b": {
                  "value": 11
                }
              }
            }
            """);
        ledger.Events.MatchInlineSnapshots(
            [
                "ledger: bump(serial, 1)",
                "reader: value(serial) = 1",
                "ledger: bump(serial, 10)",
                "reader: value(serial) = 11"
            ]);
    }

    [Theory]
    [InlineData(
        true,
        """
        {
          "data": {
            "a": {
              "value": 1
            },
            "b": {
              "value": 11
            }
          }
        }
        """,
        new[]
        {
            "ledger: bump(included, 1)",
            "reader: value(included) = 1",
            "ledger: bump(included, 10)",
            "reader: value(included) = 11"
        })]
    [InlineData(
        false,
        """
        {
          "data": {
            "a": {
              "value": 11
            },
            "b": {
              "value": 10
            }
          }
        }
        """,
        new[]
        {
            "ledger: bump(included, 10)",
            "reader: value(included) = 10",
            "ledger: bump(included, 1)",
            "reader: value(included) = 11"
        })]
    public async Task Execute_Should_RunMergedRootOnceInFirstIncludedOrder_When_EarlierOccurrenceIsConditioned(
        bool x,
        string expectedResponse,
        string[] expectedEvents)
    {
        // arrange
        var ledger = new CounterLedger();
        using var gateway = await CreateApolloGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            mutation Included($x: Boolean!) {
              a: bump(counter: "included", by: 1) @include(if: $x) {
                value
              }
              b: bump(counter: "included", by: 10) {
                value
              }
              a: bump(counter: "included", by: 1) {
                value
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["x"] = x });

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(expectedResponse);
        ledger.Events.MatchInlineSnapshots(expectedEvents);
    }

    [Theory]
    [InlineData(
        true,
        new[]
        {
            """{"data":{"a":{"id":"s"}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    [InlineData(
        false,
        new[]
        {
            """{"data":{"a":{}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    public async Task Execute_Should_DeliverDeferredFragmentOnce_When_LaterOccurrenceDefersUnderConditionedOne(
        bool x,
        string[] expectedPayloads,
        string[] expectedEvents)
    {
        // arrange
        var ledger = new CounterLedger();
        using var gateway = await CreateCompositeGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            mutation M($x: Boolean!) {
              a: bump(counter: "s", by: 1) @include(if: $x) {
                id
              }
              a: bump(counter: "s", by: 1) {
                ... @defer(label: "d") {
                  value
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["x"] = x });

        // act
        var payloads = await PostAndReadPayloadsAsync(client, request);

        // assert
        payloads.MatchInlineSnapshots(expectedPayloads);
        ledger.Events.MatchInlineSnapshots(expectedEvents);
    }

    [Theory]
    [InlineData(
        true,
        true,
        new[]
        {
            """{"data":{"a":{"id":"s"}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    [InlineData(
        true,
        false,
        new[]
        {
            """{"data":{"a":{}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    [InlineData(
        false,
        true,
        new[]
        {
            """{"data":{"a":{}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    [InlineData(
        false,
        false,
        new[]
        {
            """{"data":{"a":{}},"pending":[{"id":"0","path":["a"],"label":"d"}],"hasNext":true}""",
            """{"incremental":[{"id":"0","data":{"value":1}}],"completed":[{"id":"0"}],"hasNext":false}"""
        },
        new[] { "ledger: bump(s, 1)", "reader: value(s) = 1" })]
    public async Task Execute_Should_DeliverDeferredFragmentOnce_When_EarlierOccurrenceHasTwoConditions(
        bool x,
        bool y,
        string[] expectedPayloads,
        string[] expectedEvents)
    {
        // arrange
        var ledger = new CounterLedger();
        using var gateway = await CreateCompositeGatewayAsync(ledger);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            mutation M($x: Boolean!, $y: Boolean!) {
              ... @include(if: $x) {
                a: bump(counter: "s", by: 1) @include(if: $y) {
                  id
                }
              }
              a: bump(counter: "s", by: 1) {
                ... @defer(label: "d") {
                  value
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["x"] = x, ["y"] = y });

        // act
        var payloads = await PostAndReadPayloadsAsync(client, request);

        // assert
        payloads.MatchInlineSnapshots(expectedPayloads);
        ledger.Events.MatchInlineSnapshots(expectedEvents);
    }

    private static async Task<string[]> PostAndReadPayloadsAsync(
        GraphQLHttpClient client,
        OperationRequest request)
    {
        using var response = await client.SendAsync(
            new GraphQLHttpRequest(request, new Uri("http://localhost:5000/graphql"))
            {
                Accept = [new MediaTypeWithQualityHeaderValue("application/jsonl")]
            },
            TestContext.Current.CancellationToken);
        var body = await response.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        return body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private Task<Gateway> CreateApolloGatewayAsync(CounterLedger ledger)
    {
        var ledgerSchema = CreateSourceSchema(
            "ledger",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ApolloLedger.Query>()
                .AddMutationType<ApolloLedger.Mutation>(),
            services => services.AddSingleton(ledger));
        var readerSchema = CreateSourceSchema(
            "reader",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<ApolloReader.Counter>(),
            services => services.AddSingleton(ledger));

        return CreateCompositeSchemaAsync(
            [("ledger", ledgerSchema), ("reader", readerSchema)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
    }

    private Task<Gateway> CreateCompositeGatewayAsync(CounterLedger ledger)
    {
        var ledgerSchema = CreateSourceSchema(
            "ledger",
            builder => builder
                .AddQueryType<CompositeLedger.Query>()
                .AddMutationType<CompositeLedger.Mutation>(),
            services => services.AddSingleton(ledger));
        var readerSchema = CreateSourceSchema(
            "reader",
            builder => builder.AddQueryType<CompositeReader.Query>(),
            services => services.AddSingleton(ledger));

        return CreateCompositeSchemaAsync(
            [("ledger", ledgerSchema), ("reader", readerSchema)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
    }

    // Holds the counters both source schemas share and records every effect and read in order.
    public sealed class CounterLedger
    {
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private readonly Dictionary<string, int> _counters = [];
        private readonly List<string> _events = [];

        public IReadOnlyList<string> Events
        {
            get
            {
                lock (_sync)
                {
                    return [.. _events];
                }
            }
        }

        public void Bump(string counter, int by)
        {
            lock (_sync)
            {
                _counters[counter] = _counters.GetValueOrDefault(counter) + by;
                _events.Add($"ledger: bump({counter}, {by})");
            }
        }

        public int Read(string counter)
        {
            lock (_sync)
            {
                var value = _counters.GetValueOrDefault(counter);
                _events.Add($"reader: value({counter}) = {value}");
                return value;
            }
        }
    }

    public static class ApolloLedger
    {
        public sealed class Query
        {
            public string? Ping() => "pong";
        }

        public sealed class Mutation
        {
            public Counter? Bump(
                [GraphQLType(typeof(NonNullType<IdType>))] string counter,
                int by,
                [Service] CounterLedger ledger)
            {
                ledger.Bump(counter, by);
                return new Counter(counter);
            }
        }

        [Key("id")]
        public sealed class Counter(string id)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;

            [ReferenceResolver]
            public static Counter ResolveReference(string id) => new(id);
        }
    }

    public static class ApolloReader
    {
        [Key("id")]
        public sealed class Counter(string id)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;

            public int? GetValue([Service] CounterLedger ledger) => ledger.Read(Id);

            [ReferenceResolver]
            public static Counter ResolveReference(string id) => new(id);
        }
    }

    public static class CompositeLedger
    {
        public sealed class Query
        {
            public string? Ping() => "pong";
        }

        public sealed class Mutation
        {
            public Counter? Bump(
                [GraphQLType(typeof(NonNullType<IdType>))] string counter,
                int by,
                [Service] CounterLedger ledger)
            {
                ledger.Bump(counter, by);
                return new Counter(counter);
            }
        }

        [EntityKey("id")]
        public sealed class Counter(string id)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;
        }
    }

    public static class CompositeReader
    {
        public sealed class Query
        {
            [Lookup]
            public Counter? GetCounterById([GraphQLType(typeof(NonNullType<IdType>))] string id) => new(id);
        }

        [EntityKey("id")]
        public sealed class Counter(string id)
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; } = id;

            public int? GetValue([Service] CounterLedger ledger) => ledger.Read(Id);
        }
    }
}
