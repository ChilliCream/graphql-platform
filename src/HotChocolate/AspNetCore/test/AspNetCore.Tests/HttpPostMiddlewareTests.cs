using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.AspNetCore.Instrumentation;
using System;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.AspNetCore.Extensions;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore;

public class HttpPostMiddlewareTests : ServerTestBase
{
    public HttpPostMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Simple_IsAlive_Test()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapGraphQLHttp_Simple_IsAlive_Test()
    {
        // arrange
        TestServer server = CreateServer(endpoint => endpoint.MapGraphQLHttp());

        // act
        ClientQueryResult result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Include_Query_Plan()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            includeQueryPlan: true);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_Payload_With_Whitespaces()
    {
        // arrange
        TestServer server = CreateStarWarsServer(
            configureServices: sc => sc.AddHttpResultSerializer(indented: true));

        // act
        ClientRawResult result = await server.PostRawAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_Payload_Without_Extra_Whitespaces()
    {
        // arrange
        TestServer server = CreateStarWarsServer(
            configureServices: sc => sc.AddHttpResultSerializer(indented: false));

        // act
        ClientRawResult result = await server.PostRawAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
    {
        // arrange
        TestServer server = CreateStarWarsServer("/foo");

        // act
        ClientQueryResult result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            "/foo");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero {
                            name
                        }
                    }"
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName_Casing_Is_Preserved()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        HERO: hero {
                            name
                        }
                    }"
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName_With_EnumVariable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHumanName_With_StringVariable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }",
                Variables = new Dictionary<string, object> { { "id", "1000" } }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Defer_Results()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientRawResult result =
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"")
                            {
                                id
                            }
                        }
                    }"
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Single_Diagnostic_Listener_Is_Triggered()
    {
        // arrange
        var listenerA = new TestListener();

        TestServer server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                    .AddDiagnosticEventListener(sp => listenerA));

        // act
        await server.PostRawAsync(new ClientQueryRequest
        {
            Query = @"
                {
                    hero(episode: NEW_HOPE)
                    {
                        name
                        ... on Droid @defer(label: ""my_id"")
                        {
                            id
                        }
                    }
                }"
        });

        // assert
        Assert.True(listenerA.Triggered);
    }

    [Fact]
    public async Task Aggregate_Diagnostic_All_Listeners_Are_Triggered()
    {
        // arrange
        var listenerA = new TestListener();
        var listenerB = new TestListener();

        TestServer server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                    .AddDiagnosticEventListener(sp => listenerA)
                    .AddDiagnosticEventListener(sp => listenerB));

        // act
        await server.PostRawAsync(new ClientQueryRequest
        {
            Query = @"
                {
                    hero(episode: NEW_HOPE)
                    {
                        name
                        ... on Droid @defer(label: ""my_id"")
                        {
                            id
                        }
                    }
                }"
        });

        // assert
        Assert.True(listenerA.Triggered);
        Assert.True(listenerB.Triggered);
    }

    [Fact]
    public async Task Ensure_Multipart_Format_Is_Correct_With_Defer()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientRawResult result =
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"")
                            {
                                id
                            }
                        }
                    }"
            });

        // assert
        result.Content.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Multipart_Format_Is_Correct_With_Stream()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientRawResult result =
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero(episode: NEW_HOPE)
                        {
                            name
                            friends {
                                nodes @stream(initialCount: 1 label: ""foo"") {
                                    name
                                }
                            }
                        }
                    }"
            });

        // assert
        result.Content.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_With_ObjectVariable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                Variables = new Dictionary<string, object>
                {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 }, { "commentary", "This is a great movie!" },
                            }
                        }
                }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Omit_NonNull_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                Variables = new Dictionary<string, object>
                {
                        {
                            "review",
                            new Dictionary<string, object>
                            {
                                { "stars", 5 }, { "commentary", "This is a great movie!" },
                            }
                        }
                }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Variables_In_ObjectValue()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                Variables = new Dictionary<string, object>
                {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Variables_Unused()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!
                        $foo: Float) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                Variables = new Dictionary<string, object>
                {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                }
            });

        // assert
        result.MatchSnapshot();
    }

    [InlineData("a")]
    [InlineData("b")]
    [Theory]
    public async Task SingleRequest_Execute_Specific_Operation(
        string operationName)
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }

                    query b {
                        b: hero {
                            name
                        }
                    }",
                OperationName = operationName
            });

        // assert
        result.MatchSnapshot(new SnapshotNameExtension(operationName));
    }

    [Fact]
    public async Task SingleRequest_ValidationError()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_SyntaxError()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        ähero {
                            name
                        }
                    }"
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                Variables = new Dictionary<string, object> { { "d", 1.539 } }
            },
                "/arguments");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Max_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                Variables = new Dictionary<string, object> { { "d", double.MaxValue } }
            },
                "/arguments");

        // assert
        new { double.MaxValue, result }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Min_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                Variables = new Dictionary<string, object> { { "d", double.MinValue } }
            },
                "/arguments");

        // assert
        new { double.MinValue, result }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Max_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }",
                Variables = new Dictionary<string, object> { { "d", decimal.MaxValue } }
            },
                "/arguments");

        // assert
        new { decimal.MaxValue, result }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Min_Variable()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }",
                Variables = new Dictionary<string, object> { { "d", decimal.MinValue } }
            },
                "/arguments");

        // assert
        new { decimal.MinValue, result }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Incomplete()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync("{ \"query\":    ");

        // assert
        result.MatchSnapshot();
    }

    [InlineData("{}", 1)]
    [InlineData("{ }", 2)]
    [InlineData("{\n}", 3)]
    [InlineData("{\r\n}", 4)]
    [Theory]
    public async Task SingleRequest_Empty(string request, int id)
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync(request);

        // assert
        result.MatchSnapshot(new SnapshotNameExtension(id.ToString()));
    }

    [InlineData("[]", 1)]
    [InlineData("[ ]", 2)]
    [InlineData("[\n]", 3)]
    [InlineData("[\r\n]", 4)]
    [Theory]
    public async Task BatchRequest_Empty(string request, int id)
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync(request);

        // assert
        result.MatchSnapshot(new SnapshotNameExtension(id.ToString()));
    }

    [Fact]
    public async Task EmptyRequest()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        ClientQueryResult result = await server.PostAsync(string.Empty);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Middleware_Mapping()
    {
        // arrange
        TestServer server = CreateStarWarsServer("/foo");

        // act
        ClientQueryResult result = await server.PostAsync(string.Empty);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task BatchRequest_GetHero_And_GetHuman()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostAsync(new List<ClientQueryRequest>
            {
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }"
                    },
                    new ClientQueryRequest
                    {
                        Query = @"
                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                    }
            });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OperationBatchRequest_GetHero_And_GetHuman()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostOperationAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                },
                "getHero, getHuman");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OperationBatchRequest_Invalid_BatchingParameter_1()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostOperationAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }

                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }"
                },
                "getHero",
                createOperationParameter: s => "batchOperations=" + s);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OperationBatchRequest_Invalid_BatchingParameter_2()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostOperationAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                },
                "getHero, getHuman",
                createOperationParameter: s => "batchOperations=[" + s);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OperationBatchRequest_Invalid_BatchingParameter_3()
    {
        // arrange
        TestServer server = CreateStarWarsServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostOperationAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query getHero {
                                hero(episode: EMPIRE) {
                                    id @export
                                }
                            }

                            query getHuman {
                                human(id: $id) {
                                    name
                                }
                            }"
                },
                "getHero, getHuman",
                createOperationParameter: s => "batchOperations=" + s);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Throw_Custom_GraphQL_Error()
    {
        // arrange
        TestServer server = CreateStarWarsServer(
            configureServices: s => s.AddGraphQLServer()
                .AddHttpRequestInterceptor<ErrorRequestInterceptor>());

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                    {
                        hero {
                            name
                        }
                    }"
            });

        // assert
        result.MatchSnapshot();
    }

    public class ErrorRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            throw new GraphQLException("MyCustomError");
        }
    }

    public class TestListener : ServerDiagnosticEventListener
    {
        public bool Triggered { get; set; }

        public override IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
        {
            Triggered = true;
            return EmptyScope;
        }
    }


    /// <see cref = "BatchParallelExecution_Node" />
    private class BatchParallelExecution_GroupTracker : ExecutionDiagnosticEventListener
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _httpContextItemKey;

        public BatchParallelExecution_GroupTracker(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpContextItemKey = GetType().FullName + ".ItemKey";
        }

        public int Current
        {
            get
            {
                if (_httpContextAccessor.HttpContext.Items.TryGetValue(_httpContextItemKey, out object? currentGroup) )
                {
                    return (int) currentGroup;
                }
                else
                {
                    return 0;
                }
            }
        }

        public override IDisposable ExecuteBatchedQueryGroup()
        {
             _httpContextAccessor.HttpContext.Items[_httpContextItemKey] = Current + 1;
            return EmptyScope;
        }
    }

    /// <see cref = "CreateBatchParallelExecutionTestServer" />
    private class BatchParallelExecution_Node
    {
        private readonly BatchParallelExecution_GroupTracker _groupTracker;

        public BatchParallelExecution_Node(BatchParallelExecution_GroupTracker groupTracker)
        {
            _groupTracker = groupTracker ?? throw new ArgumentNullException(nameof(groupTracker));
        }

        public int Group() { return _groupTracker.Current; }
        public int Print(int i) { return i; }
        public int Add(int i1, int i2) { return i1 + i2; }
    }

    /// <summary>Server used to test parallel execution in batch requests</summary>
    protected virtual TestServer BatchParallelExecution_CreateServer(
        string pattern = "/graphql",
        Action<IServiceCollection> configureServices = default,
        Action<GraphQLEndpointConventionBuilder> configureConventions = default)
    {
        return ServerFactory.Create(
            services => services
                .AddSingleton<BatchParallelExecution_GroupTracker>()
                .AddSingleton<IExecutionDiagnosticEventListener>(sp => sp.GetRequiredService<BatchParallelExecution_GroupTracker>())
                .AddRouting()
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddGraphQLServer()
                .ModifyRequestOptions(o =>
                {
                    o.MaxConcurrentBatchQueries = 5;
                })
                .AddQueryType(d =>
                {
                    d.Name("Query")
                        .Field("node")
                        .Type<ObjectType<BatchParallelExecution_Node>>()
                        .Resolve(c =>
                        {
                            return ActivatorUtilities.CreateInstance<BatchParallelExecution_Node>(c.Services);
                        });
                })
                .AddMutationType(d =>
                {
                    d.Name("Mutation")
                        .Field("nop")
                        .Type<ObjectType<BatchParallelExecution_Node>>()
                        .Resolve(c =>
                        {
                            return ActivatorUtilities.CreateInstance<BatchParallelExecution_Node>(c.Services);
                        });
                })
                .AddExportDirectiveType()
            ,
            app => app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL("/graphql");
                })
            );
    }

    /// <summary>
    /// Basic success case, apollo style batches with only queries are executed in parallel
    /// </summary>
    [Fact]
    public async Task BatchParallelExecution_Basic()
    {
        // arrange
        TestServer server = BatchParallelExecution_CreateServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostApolloBatchAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query one { node { group } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query two { node { group } }"
                });

        // assert
        result.MatchSnapshot();
    }


    /// <summary>
    /// Ensure that when operationNames query parameter is passed, no queries are executed in parallel.
    /// (this is relay style workflow batch, not an apollo style batch)
    /// </summary>
    [Fact]
    public async Task BatchParallelExecution_OperationNames()
    {
        // arrange
        TestServer server = BatchParallelExecution_CreateServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostOperationAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query one { node { group } }
                          query two { node { group } }"
                },
                "one, two");

        // assert
        result.MatchSnapshot();
    }

    /// <summary>
    /// Ensure that the limit of concurrent requests for a batch requests is enforced properly
    /// </summary>
    [Fact]
    public async Task BatchParallelExecution_Limit()
    {
        // arrange
        TestServer server = BatchParallelExecution_CreateServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostApolloBatchAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:1) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:2) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:3) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:4) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:5) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:6) } }"
                });

        // assert
        result.MatchSnapshot();
    }


    /// <summary>
    /// Ensure that we do not run queries in parallel that are not idempotent.
    /// (edge case, apollo style batching shouldn't have this anyway)
    /// </summary>
    [Fact]
    public async Task BatchParallelExecution_Exports()
    {
        // arrange
        TestServer server = BatchParallelExecution_CreateServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostApolloBatchAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:1) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:2) @export(as: ""exported"") } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query ($exported: Int!) { node { group, add(i1:$exported, i2: 1) } }"
                });

        // assert
        result.MatchSnapshot();
    }

    /// <summary>
    /// Ensure that mutations are not executed in parallel with both other queries or mutations
    /// (edge case, apollo style batching shouldn't have this anyway)
    /// </summary>
    [Fact]
    public async Task BatchParallelExecution_Mutations()
    {
        // arrange
        TestServer server = BatchParallelExecution_CreateServer();

        // act
        IReadOnlyList<ClientQueryResult> result =
            await server.PostApolloBatchAsync(
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:1) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:2) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"mutation { nop { group, print(i:3) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:4) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:5) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"mutation { nop { group, print(i:6) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"mutation { nop { group, print(i:7) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:8) } }"
                },
                new ClientQueryRequest
                {
                    Query =
                        @"query { node { group, print(i:9) } }"
                });

        // assert
        result.MatchSnapshot();
    }
}
