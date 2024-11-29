using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars.Models;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using static HotChocolate.StarWars.Types.Subscriptions;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsOnReviewSubGraphQLSSE;

public class StarWarsOnReviewSubGraphQLSSETest : ServerTestBase
{
    public StarWarsOnReviewSubGraphQLSSETest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_StarWarsOnReviewSubGraphQLSSE_Test()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        var ct = cts.Token;
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            StarWarsOnReviewSubGraphQLSSEClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            StarWarsOnReviewSubGraphQLSSEClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddStarWarsOnReviewSubGraphQLSSEClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<StarWarsOnReviewSubGraphQLSSEClient>();

        // act
        var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();
        var topic = Episode.NewHope;

        var connectCompletionSource = new TaskCompletionSource();
        var subscribeCompletionSource = new TaskCompletionSource();
        var session = client.OnReviewSub.Watch()
            .Subscribe(
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
