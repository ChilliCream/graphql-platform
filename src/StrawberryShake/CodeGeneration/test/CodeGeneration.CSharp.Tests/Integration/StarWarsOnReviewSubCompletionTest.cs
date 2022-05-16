using System;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars.Models;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsOnReviewSubCompletion
{
    public class StarWarsOnReviewSubCompletionTest : ServerTestBase
    {
        public StarWarsOnReviewSubCompletionTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Watch_StarWarsOnReviewSubCompletion_Test()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddStarWarsOnReviewSubCompletionClient(
                profile: StarWarsOnReviewSubCompletionClientProfileKind.Default)
                .ConfigureHttpClient(
                    c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
                .ConfigureWebSocketClient(
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

            // act
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            IStarWarsOnReviewSubCompletionClient client = services.GetRequiredService<IStarWarsOnReviewSubCompletionClient>();

            string? commentary = null;
            bool completionTriggered = false;

            var sub = client.OnReviewSub.Watch();
            var session = sub.Subscribe(
                result => commentary = result.Data?.OnReview?.Commentary,
                () => completionTriggered = true);

            var topic = Episode.NewHope;

            // try to send message 10 times
            // make sure the subscription connection is successful
            for(int times = 0; commentary is null && times < 10; times++)
            {
                await topicEventSender.SendAsync(topic, new Review { Stars = 1, Commentary = "Commentary" });
                await Task.Delay(1_000);
            }

            // complete the topic of subscription from server
            await topicEventSender.CompleteAsync(topic);

            // waiting for completion message sent
            for (int times = 0; !completionTriggered && times < 10; times++)
            {
                await Task.Delay(1_000);
            }

            session.Dispose();

            // assert
            Assert.True(commentary is not null && completionTriggered);
        }
    }
}
