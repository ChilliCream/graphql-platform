using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration.CSharp.Integration.StarWars;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration
{
    public class IntegrationTests : ServerTestBase
    {
        public IntegrationTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact(Skip = "This test is flaky.")]
        public async Task Simple_Request()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                "StarWarsIntegrationClient",
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                "StarWarsIntegrationClient",
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            serviceCollection.AddStarWarsIntegrationClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();

            // act
            var list = new List<int>();
            services.GetRequiredService<StarWarsIntegrationClient>()
                .OnReviewSubscription
                .Watch()
                .Subscribe(result =>
                {
                    list.Add(result.Data!.OnReview.Stars);
                });

            await Task.Delay(150);

            await services
                .GetRequiredService<StarWarsIntegrationClient>()
                .CreateReviewMutation
                .ExecuteAsync(5, ct);

            await services
                .GetRequiredService<StarWarsIntegrationClient>()
                .CreateReviewMutation
                .ExecuteAsync(3, ct);

            await services
                .GetRequiredService<StarWarsIntegrationClient>()
                .CreateReviewMutation
                .ExecuteAsync(8, ct);

            await Task.Delay(500);

            // assert
            Assert.Collection(
                list,
                r => Assert.Equal(5, r),
                r => Assert.Equal(3, r),
                r => Assert.Equal(8, r));
        }
    }
}
