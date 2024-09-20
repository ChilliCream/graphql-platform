using HotChocolate.AspNetCore.Tests.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriendsDeferred;

public class StarWarsGetFriendsDeferredTest : ServerTestBase
{
    public StarWarsGetFriendsDeferredTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    /*
    [Fact]
    public async Task Execute_StarWarsGetFriendsDeferred_Test()
    {
        // arrange
        CancellationToken ct = new CancellationTokenSource(20_000).Token;
        using IWebHost host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsGetFriendsDeferredClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsGetFriendsDeferredClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsGetFriendsDeferredClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        StarWarsGetFriendsDeferredClient client = services.GetRequiredService<StarWarsGetFriendsDeferredClient>();

        // act

        // assert

    }
    */
}
