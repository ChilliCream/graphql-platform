using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.PersistedQueries.FileSystem
{
    public class IntegrationTests
    {
        [Fact]
        public async Task ExecutePersistedQuery()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            DocumentNode document = Utf8GraphQLParser.Parse("{ __typename }");

            IServiceProvider services =
                new ServiceCollection()
                    .AddMemoryCache()
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    .AddInMemoryQueryStorage()
                    .UseRequest(n => async c =>
                    {
                        await n(c);

                        if (c.IsPersistedDocument && c.Result is IQueryResult r)
                        {
                            c.Result = QueryResultBuilder
                                .FromResult(r)
                                .SetExtension("persistedDocument", true)
                                .Create();
                        }
                    })
                    .UsePersistedQueryPipeline()
                    .Services
                    .BuildServiceProvider();

            var cache = services.GetRequiredService<IMemoryCache>();
            IRequestExecutor executor = await services.GetRequestExecutorAsync();

            await cache.GetOrCreate(queryId, item => Task.FromResult(new QueryDocument(document)));

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecutePersistedQuery_NotFound()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            DocumentNode document = Utf8GraphQLParser.Parse("{ __typename }");

            IServiceProvider services =
                new ServiceCollection()
                    .AddMemoryCache()
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    .AddInMemoryQueryStorage()
                    .UseRequest(n => async c =>
                    {
                        await n(c);

                        if (c.IsPersistedDocument && c.Result is IQueryResult r)
                        {
                            c.Result = QueryResultBuilder
                                .FromResult(r)
                                .SetExtension("persistedDocument", true)
                                .Create();
                        }
                    })
                    .UsePersistedQueryPipeline()
                    .Services
                    .BuildServiceProvider();

            var cache = services.GetRequiredService<IMemoryCache>();
            IRequestExecutor executor = await services.GetRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: "does_not_exist"));

            // assert
            result.ToJson().MatchSnapshot();
        }
    }
}
