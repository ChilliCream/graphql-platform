using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.CSharp.Integration.StarWars;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Protocol;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration
{
    public class IntegrationTests : ServerTestBase
    {
        public IntegrationTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
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
