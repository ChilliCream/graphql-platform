using System.Net;
using Xunit;
using HotChocolate.AspNetCore.Tests.Utilities;

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
        var features = await IntrospectionClient.InspectServerAsync(client);

        // assert
        Assert.True(features.HasArgumentDeprecation);
        Assert.True(features.HasDirectiveLocations);
        Assert.True(features.HasSubscriptionSupport);
        Assert.True(features.HasSchemaDescription);
        Assert.True(features.HasRepeatableDirectives);
        Assert.True(features.HasDeferSupport);
        Assert.True(features.HasStreamSupport);
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
        var schema = await IntrospectionClient.IntrospectServerAsync(client);

        // assert
        schema.ToString(true).MatchSnapshot();
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
