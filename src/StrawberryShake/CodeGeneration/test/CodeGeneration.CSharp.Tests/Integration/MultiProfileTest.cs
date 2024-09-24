using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.MultiProfile;

public class MultiProfileTest : ServerTestBase
{
    public MultiProfileTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public void Execute_MultiProfile_Test()
    {
        // arrange
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            MultiProfileClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            MultiProfileClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        // act
        serviceCollection.AddMultiProfileClient(
            profile: MultiProfileClientProfileKind.Default);

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<MultiProfileClient>();

        // assert
        Assert.NotNull(client);
    }
}
