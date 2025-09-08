using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsTypeNameOnInterfaces;

public class StarWarsTypeNameOnInterfacesTest : ServerTestBase
{
    public StarWarsTypeNameOnInterfacesTest(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsTypeNameOnInterfaces_Test()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);

        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsTypeNameOnInterfacesClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsTypeNameOnInterfacesClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsTypeNameOnInterfacesClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client =
            services.GetRequiredService<StarWarsTypeNameOnInterfacesClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(cts.Token);

        // assert
        result.EnsureNoErrors();
        result.Data.MatchSnapshot();
    }
}
