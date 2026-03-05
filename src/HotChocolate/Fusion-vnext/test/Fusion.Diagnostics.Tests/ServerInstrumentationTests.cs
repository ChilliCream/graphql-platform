using HotChocolate.Diagnostics;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class ServerInstrumentationTests : FusionTestBase
{
    private static readonly Uri s_url = new("http://localhost:5000/graphql");

    [Fact]
    public async Task Http_Post_Single_Request_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation());

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Single_Request()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_Single_Request()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.GetAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Variables_Are_Not_Automatically_Added_To_Activities()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Add_Variables_To_Http_Activity()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.Default | RequestDetails.Variables;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Add_Query_To_Http_Activity()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.Default | RequestDetails.OperationName;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_With_Extensions_Map()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_SDL_Download()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            var httpClient = gateway.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/graphql?sdl");

            // act
            var response = await httpClient.SendAsync(request);

            // assert
            await response.Content.ReadAsStringAsync();
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "Not yet implemented")]
    public async Task Http_Post_Capture_Deferred_Response()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                """
                TODO
                """);

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Parser_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            // lang=text
            var request = new OperationRequest(
                """
                {
                    deep {
                        deeper {
                            1deeps {
                                name
                            }
                        }
                    }
                }
                """);

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Parsing_Error_When_Rename_Root_Is_Activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b
                    .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            // lang=text
            var request = new OperationRequest("{ 1 }");

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Validation_Error_When_Rename_Root_Is_Activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b
                    .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ abc }");

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_None_ExcludesAllDetails()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.None;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_All_IncludesAllDetails()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.All;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_DocumentOnly_IncludesDocumentTag()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.Document;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_Default_IncludesIdHashOperationNameExtensions()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting {
                        sayHello
                    }
                    """,
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_OperationNameInRequest_SetsActivityDisplayName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreetingByName($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
            using var result = await client.PostAsync(request, s_url);

            // assert
            activities.MatchSnapshot();
        }
    }

    public class Query
    {
        public string SayHello() => "hello";

        public string Greeting(string name) => $"Hello, {name}!";

        public string CauseFatalError() => throw new GraphQLException("fail");

        public Deep Deep() => new();
    }

    public class Deep
    {
        public string Name => "deep";

        public Deeper Deeper() => new();
    }

    public class Deeper
    {
        public string Name => "deeper";

        public Deep[] Deeps() => [new Deep()];
    }
}
