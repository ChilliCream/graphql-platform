using System;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars.Models;
using HotChocolate.Subscriptions;
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
        var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();
        var topic = Episode.NewHope;

        var connectCompletionSource = new TaskCompletionSource();
        var subscribeCompletionSource = new TaskCompletionSource();
        var session = client.OnReviewSub.Watch().Subscribe(
            _ => connectCompletionSource.TrySetResult(),
            () => subscribeCompletionSource.TrySetResult());

        // make sure the subscription connection is successful
        while (!connectCompletionSource.Task.IsCompleted) {
            await topicEventSender.SendAsync(topic, new Review { Stars = 1, Commentary = "Commentary" });
            await Task.Delay(1_000, ct);
        }

        // complete the topic of subscription from server
        await topicEventSender.CompleteAsync(topic);
        var completedTask = await Task.WhenAny(subscribeCompletionSource.Task, Task.Delay(Timeout.Infinite, ct));

        // assert
        Assert.True(subscribeCompletionSource.Task == completedTask);
    }
}
