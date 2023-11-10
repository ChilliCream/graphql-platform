using System;
using System.Threading.Tasks;
using System.Net.Http;
using Snapshooter.Xunit;
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
        Task Error() => IntrospectionClient.InspectServerAsync(((HttpClient)null)!);

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
        Task Error() => IntrospectionClient.IntrospectServerAsync(((HttpClient)null)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }
}