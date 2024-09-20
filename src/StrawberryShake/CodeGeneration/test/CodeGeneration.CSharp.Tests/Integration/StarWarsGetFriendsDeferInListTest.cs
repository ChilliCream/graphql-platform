using HotChocolate.AspNetCore.Tests.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetFriendsDeferInList;

public class StarWarsGetFriendsDeferInListTest : ServerTestBase
{
    public StarWarsGetFriendsDeferInListTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    /*
    [Fact]
    public async Task Execute_StarWarsGetFriendsDeferInList_Test()
    {
        // arrange
        CancellationToken ct = new CancellationTokenSource(20_000).Token;
        using IWebHost host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsGetFriendsDeferInListClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsGetFriendsDeferInListClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsGetFriendsDeferInListClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        StarWarsGetFriendsDeferInListClient client = services.GetRequiredService<StarWarsGetFriendsDeferInListClient>();

        // act

        // assert

    }
    */
}
