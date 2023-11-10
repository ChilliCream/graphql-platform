using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHeroTraits;

public class StarWarsGetHeroTraitsTest : ServerTestBase
{
    public StarWarsGetHeroTraitsTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsGetHeroTraits_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsGetHeroTraitsClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsGetHeroTraitsClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsGetHeroTraitsClient();
        var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsGetHeroTraitsClient>();

        // act
        var result = await client.GetHero.ExecuteAsync(ct);

        // assert
        Assert.Equal("{\"rockets\":true}", result.Data?.Hero?.Traits?.GetRawText());
    }
}
