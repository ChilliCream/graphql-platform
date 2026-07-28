using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AccessToDisposedClosure

namespace HotChocolate.Utilities.Introspection;

public class IntrospectionClientTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task InspectServer()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        // act
        var features = await IntrospectionClient.InspectServerAsync(client, TestContext.Current.CancellationToken);

        // assert
        features.MatchInlineSnapshot(
            """
            {
              "HasDirectiveLocations": true,
              "HasRepeatableDirectives": true,
              "HasSubscriptionSupport": true,
              "HasDeferSupport": true,
              "HasStreamSupport": true,
              "HasArgumentDeprecation": true,
              "HasDirectiveDeprecation": true,
              "HasSchemaDescription": true
            }
            """);
    }

    [Fact]
    public async Task InspectServer_HttpClient_Is_Null()
    {
        // arrange
        // act
        Task Error() => IntrospectionClient.InspectServerAsync(((HttpClient?)null)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task IntrospectServer()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        // act
        var schema = await IntrospectionClient.IntrospectServerAsync(client, TestContext.Current.CancellationToken);

        // assert
        schema.ToString(true).MatchSnapshot();
    }

    [Fact]
    public async Task IntrospectServer_With_DeprecatedDirective()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: services => services
                .AddGraphQL()
                .AddDirectiveType<ObsoleteDirectiveType>());
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        // act
        var schema = await IntrospectionClient.IntrospectServerAsync(client, TestContext.Current.CancellationToken);

        // assert
        var directive = Assert.Single(
            schema.Definitions.OfType<DirectiveDefinitionNode>(),
            t => t.Name.Value.Equals("obsolete", StringComparison.Ordinal));
        directive.Print(indented: true).MatchInlineSnapshot(
            """
            directive @obsolete(
              obsoleteArg: String @deprecated(reason: "Argument no longer supported.")
            ) @deprecated(reason: "Directive no longer supported.") on FIELD
            """);
    }

    [Fact]
    public async Task IntrospectServer_HttpClient_Is_Null()
    {
        // arrange
        // act
        Task Error() => IntrospectionClient.IntrospectServerAsync(((HttpClient?)null)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task IntrospectServer_Http_200_Wrong_Content_Type()
    {
        // arrange
        var client = new HttpClient(new CustomHttpClientHandler(HttpStatusCode.OK));
        client.BaseAddress = new Uri("http://localhost:5000");
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        Task Error() => IntrospectionClient.IntrospectServerAsync(client);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal("Received a successful response with an unexpected content type.", exception.Message);
    }

    [Fact]
    public async Task IntrospectServer_Http_404_Wrong_Content_Type()
    {
        // arrange
        var client = new HttpClient(new CustomHttpClientHandler(HttpStatusCode.NotFound));
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        Task Error() => IntrospectionClient.IntrospectServerAsync(client);

        // assert
        await Assert.ThrowsAsync<HttpRequestException>(Error);
    }

    [Fact]
    public async Task IntrospectServer_Transport_Error()
    {
        // arrange
        var client = new HttpClient(new CustomHttpClientHandler());
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        Task Error() => IntrospectionClient.IntrospectServerAsync(client);

        // assert
        var exception = await Assert.ThrowsAsync<Exception>(Error);
        Assert.Equal("Something went wrong", exception.Message);
    }

    public class ObsoleteDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("obsolete")
                .Location(Types.DirectiveLocation.Field)
                .Deprecated("Directive no longer supported.");

            descriptor
                .Argument("obsoleteArg")
                .Type<StringType>()
                .Deprecated("Argument no longer supported.");
        }
    }

    private class CustomHttpClientHandler(HttpStatusCode? httpStatusCode = null) : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (httpStatusCode.HasValue)
            {
                return Task.FromResult(new HttpResponseMessage(httpStatusCode.Value));
            }

            throw new Exception("Something went wrong");
        }
    }
}
