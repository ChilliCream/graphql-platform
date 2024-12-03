using CookieCrumble.Xunit;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars.Models;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using static HotChocolate.StarWars.Types.Subscriptions;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsOnReviewSubNoStore;

public class StarWarsOnReviewSubNoStoreTest : ServerTestBase
{
    public StarWarsOnReviewSubNoStoreTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [LocalFact]
    public async Task Watch_StarWarsOnReviewSubNoStore_NotifyCompletion()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        using var host = TestServerHelper.CreateServer(_ => { }, out var port);
        var ct = cts.Token;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsOnReviewSubNoStoreClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsOnReviewSubNoStoreClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsOnReviewSubNoStoreClient();
        await using var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsOnReviewSubNoStoreClient>();

        // act
        var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();
        const Episode topic = Episode.NewHope;

        var connectCompletionSource = new TaskCompletionSource();
        var subscribeCompletionSource = new TaskCompletionSource();
        using var session = client.OnReviewSub.Watch().Subscribe(
            _ => connectCompletionSource.TrySetResult(),
            () => subscribeCompletionSource.TrySetResult());

        // make sure the subscription connection is successful
        while (!connectCompletionSource.Task.IsCompleted)
        {
            await topicEventSender.SendAsync(
                $"{OnReview}_{topic}",
                new Review(stars: 1, commentary: "Commentary"),
                ct);
            await Task.Delay(1_000, ct);
        }

        // complete the topic of subscription from server
        await topicEventSender.CompleteAsync($"{OnReview}_{topic}");
        var completedTask = await Task.WhenAny(
            subscribeCompletionSource.Task,
            Task.Delay(Timeout.Infinite, ct));

        // assert
        Assert.True(subscribeCompletionSource.Task == completedTask);
    }
}
