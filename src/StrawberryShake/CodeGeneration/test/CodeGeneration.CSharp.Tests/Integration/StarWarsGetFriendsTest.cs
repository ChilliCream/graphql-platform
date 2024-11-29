using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriends;

public class StarWarsGetFriendsTest : ServerTestBase
{
    public StarWarsGetFriendsTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsGetFriends_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetFriendsClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client =
            services.GetRequiredService<IStarWarsGetFriendsClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.Equal("R2-D2", result.Data?.Hero?.Name);
        Assert.Collection(
            result.Data!.Hero!.Friends!.Nodes!,
            item => Assert.Equal("Luke Skywalker", item?.Name),
            item => Assert.Equal("Han Solo", item?.Name),
            item => Assert.Equal("Leia Organa", item?.Name));
    }

    [Fact]
    public async Task Execute_StarWarsGetFriends_Test_404()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetFriendsClient()
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql1"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<IStarWarsGetFriendsClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.NotNull(result.Errors);
        Assert.Collection(result.Errors,
            error => Assert.Equal(
                "Response status code does not indicate success: 404 (Not Found).",
                error.Message));
    }

    [Fact]
    public async Task Execute_StarWarsGetFriends_Test_403_And_Success_Content_1()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetFriendsClient()
            .ConfigureHttpClient(
                c =>
                {
                    c.BaseAddress = new Uri("http://localhost:" + port + "/graphql");
                    c.DefaultRequestHeaders.Add("sendErrorStatusCode", "1");
                    c.DefaultRequestHeaders.Add("sendError", "1");
                })
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client =
            services.GetRequiredService<IStarWarsGetFriendsClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.Equal("R2-D2", result.Data?.Hero?.Name);
        Assert.Collection(
            result.Data!.Hero!.Friends!.Nodes!,
            item => Assert.Equal("Luke Skywalker", item?.Name),
            item => Assert.Equal("Han Solo", item?.Name),
            item => Assert.Equal("Leia Organa", item?.Name));
        Assert.NotNull(result.Errors);
        Assert.Collection(result.Errors,
            error => Assert.Equal(
                "Some error!",
                error.Message));
    }

    [Fact]
    public async Task Execute_StarWarsGetFriends_Test_403_And_Success_Content_2()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddStarWarsGetFriendsClient()
            .ConfigureHttpClient(
                c =>
                {
                    c.BaseAddress = new Uri("http://localhost:" + port + "/graphql");
                    c.DefaultRequestHeaders.Add("sendErrorStatusCode", "1");
                })
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client =
            services.GetRequiredService<IStarWarsGetFriendsClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.Equal("R2-D2", result.Data?.Hero?.Name);
        Assert.Collection(
            result.Data!.Hero!.Friends!.Nodes!,
            item => Assert.Equal("Luke Skywalker", item?.Name),
            item => Assert.Equal("Han Solo", item?.Name),
            item => Assert.Equal("Leia Organa", item?.Name));
        Assert.NotNull(result.Errors);
        Assert.Collection(result.Errors,
            error =>
            {
                Assert.Equal(
                    "Response status code does not indicate success: 403 (Forbidden).",
                    error.Message);
                Assert.IsType<HttpRequestException>(error.Exception);
            });
    }
}
