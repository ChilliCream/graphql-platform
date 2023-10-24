using System;
using System.IO;
using System.Text;
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
    public async Task GetSchemaFeatures()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        // act
        var features = await IntrospectionClient.GetSchemaFeaturesAsync(client);

        // assert
        Assert.True(features.HasDirectiveLocations);
        Assert.True(features.HasRepeatableDirectives);
        Assert.True(features.HasSubscriptionSupport);
    }

    [Fact]
    public async Task GetSchemaFeatures_HttpClient_Is_Null()
    {
        // arrange
        // act
        Task Error() => IntrospectionClient.GetSchemaFeaturesAsync(((HttpClient)null)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task Download_Schema_AST()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        // act
        var schema = await IntrospectionClient.DownloadSchemaAsync(client);

        // assert
        schema.ToString(true).MatchSnapshot();
    }

    [Fact]
    public async Task Download_Schema_AST_HttpClient_Is_Null()
    {
        // arrange
        // act
        Task Error() => IntrospectionClient.DownloadSchemaAsync(((HttpClient)null)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task Download_Schema_SDL()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");
        
        using var stream = new MemoryStream();

        // act
        await IntrospectionClient.DownloadSchemaAsync(client, stream);

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    [Fact]
    public async Task Download_Schema_SDL_HttpClient_Is_Null()
    {
        // arrange
        using var stream = new MemoryStream();

        // act
        Task Error() => IntrospectionClient.DownloadSchemaAsync(((HttpClient)null)!, stream);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task Download_Schema_SDL_Stream_Is_Null()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        Task Error() => IntrospectionClient.DownloadSchemaAsync(server.CreateClient(), null!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Error);
    }
}

public class IntrospectionQueryBuilderTests
{
    [Fact]
    public void Create_Default_Query()
    {
        var features = new SchemaFeatures();
        var options = new IntrospectionOptions();

        IntrospectionQueryBuilder.CreateQuery(features, options);
    }
}