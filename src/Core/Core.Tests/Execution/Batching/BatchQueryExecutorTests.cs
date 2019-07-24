using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutorTests
    {
        [Fact]
        public async Task ExecuteExportScalar()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddStarWarsRepositories()
                .AddSingleton<IBatchQueryExecutor, BatchQueryExecutor>();

            QueryExecutionBuilder.BuildDefault(serviceCollection);

            serviceCollection.AddSingleton<ISchema>(sp =>
                SchemaBuilder.New()
                    .AddStarWarsTypes()
                    .AddDirectiveType<ExportDirectiveType>()
                    .AddServices(sp)
                    .Create());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var executor = services.GetService<IBatchQueryExecutor>();

            // act
            var batch = new List<IReadOnlyQueryRequest>
            {
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }")
                    .Create(),
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }")
                    .Create()
            };

            IResponseStream stream =
                await executor.ExecuteAsync(batch, CancellationToken.None);

            var results = new List<IReadOnlyQueryResult>();
            while (!stream.IsCompleted)
            {
                IReadOnlyQueryResult result = await stream.ReadAsync();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            Assert.Collection(results,
                r => r.MatchSnapshot(new SnapshotNameExtension("1")),
                r => r.MatchSnapshot(new SnapshotNameExtension("2")));
        }
    }
}
