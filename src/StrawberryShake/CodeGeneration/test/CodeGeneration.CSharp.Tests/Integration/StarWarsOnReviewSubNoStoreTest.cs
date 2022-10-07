using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsOnReviewSubNoStore;

public class StarWarsOnReviewSubNoStoreTest : ServerTestBase
{
    public StarWarsOnReviewSubNoStoreTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Watch_StarWarsOnReviewSubNoStore_NotifyCompletion()
    {
        // arrange
        CancellationToken ct = new CancellationTokenSource(20_000).Token;
        using IWebHost host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsOnReviewSubNoStoreClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsOnReviewSubNoStoreClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsOnReviewSubNoStoreClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        StarWarsOnReviewSubNoStoreClient client = services.GetRequiredService<StarWarsOnReviewSubNoStoreClient>();

        // act


        // assert

    }
}
