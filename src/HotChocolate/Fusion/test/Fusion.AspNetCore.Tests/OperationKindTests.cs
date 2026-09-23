using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

// The gateway enforces the request flags the HTTP layer derives from the method and the Accept
// header, so an operation kind the method does not serve is refused before execution.
public class OperationKindTests : FusionTestBase
{
    private static readonly HttpMethod s_queryMethod = new("QUERY");

    [Fact]
    public async Task Query_Should_ReturnResult_When_QueryIsSent()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>().AddMutationType<Mutation>());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.EnableQueryRequests = true),
            includeOperationPlan: false);
        using var client = gateway.CreateClient();

        // act
        using var request = CreateRequest(s_queryMethod, """{ "query": "{ greeting }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            """{"data":{"greeting":"Hello"}}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Query_Should_ReturnUnprocessableContent_When_MutationIsSent()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>().AddMutationType<Mutation>());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.EnableQueryRequests = true),
            includeOperationPlan: false);
        using var client = gateway.CreateClient();

        // act
        using var request = CreateRequest(
            s_queryMethod,
            """{ "query": "mutation { setGreeting(greeting: \"Hi\") }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.UnprocessableContent, response.StatusCode);
        Assert.Equal(
            """{"errors":[{"message":"The specified operation kind is not allowed."}]}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_Should_ReturnMethodNotAllowed_When_MutationIsSent()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>().AddMutationType<Mutation>());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            includeOperationPlan: false);
        using var client = gateway.CreateClient();
        var query = Uri.EscapeDataString("mutation { setGreeting(greeting: \"Hi\") }");

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"http://localhost:5000/graphql?query={query}"));
        request.Headers.Add("Accept", "application/graphql-response+json");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(
            """{"errors":[{"message":"The specified operation kind is not allowed."}]}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Query_Should_ReturnNotAcceptable_When_DeferIsSentWithoutStreamingAccept()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Query>().AddMutationType<Mutation>());
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.EnableQueryRequests = true),
            includeOperationPlan: false);
        using var client = gateway.CreateClient();

        // act
        using var request = CreateRequest(
            s_queryMethod,
            """{ "query": "{ profile { name ... @defer { bio } } }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        Assert.Equal(
            """{"errors":[{"message":"The client does not accept a response content type that supports incremental delivery."}]}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string body)
    {
        var request = new HttpRequestMessage(method, new Uri("http://localhost:5000/graphql"))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Accept", "application/graphql-response+json");

        return request;
    }

    public class Query
    {
        public string Greeting() => "Hello";

        public Profile Profile() => new("Ada", "Writes GraphQL servers.");
    }

    public class Mutation
    {
        public string SetGreeting(string greeting) => greeting;
    }

    public record Profile(string Name, string Bio);
}
