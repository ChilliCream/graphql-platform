using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.MultiProfile
{
    public class MultiProfileTest : ServerTestBase
    {
        public MultiProfileTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_MultiProfile_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddGraphQLServer()
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();
            serviceCollection.AddInMemoryClient(MultiProfileClient.ClientName);
            serviceCollection.AddMultiProfileClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            MultiProfileClient client = services.GetRequiredService<MultiProfileClient>();

            // act
            IOperationResult<IGetHeroResult> result = await client.GetHero.ExecuteAsync(ct);

            // assert
            result.Data.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Mutation()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddGraphQLServer()
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();
            serviceCollection.AddInMemoryClient(MultiProfileClient.ClientName);
            serviceCollection.AddMultiProfileClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            MultiProfileClient client = services.GetRequiredService<MultiProfileClient>();

            // act
            IOperationResult<ICreateReviewMutResult> result =
                await client.CreateReviewMut
                    .ExecuteAsync(
                        Episode.Empire,
                        new ReviewInput { Commentary = "foo", Stars = 3 },
                        ct);

            // assert
            result.Data.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Subscription()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddGraphQLServer()
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();
            serviceCollection.AddInMemoryClient(MultiProfileClient.ClientName);
            serviceCollection.AddMultiProfileClient();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            MultiProfileClient client = services.GetRequiredService<MultiProfileClient>();

            // act
            List<IOnReviewSubResult> result = new();

            using IDisposable sub = client.OnReviewSub
                .Watch()
                .Subscribe(x =>
                {
                    result.Add(x.Data!);
                });

            await Task.Delay(1000, ct);

            var response = await client.CreateReviewMut
                .ExecuteAsync(
                    Episode.NewHope,
                    new ReviewInput { Commentary = "foo", Stars = 3 },
                    ct);

            // assert
            await Task.Delay(1000, ct);
            result.SingleOrDefault().MatchSnapshot();
        }
    }
}
