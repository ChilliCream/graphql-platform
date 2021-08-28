using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
    public class IntegrationTests : IClassFixture<RedisResource>
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _database;

        public IntegrationTests(RedisResource redisResource)
        {
            _multiplexer = redisResource.GetConnection();
            _database = _multiplexer.GetDatabase();
        }

        [Fact]
        public async Task ExecutePersistedQuery()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            var storage = new RedisQueryStorage(_database);
            await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    .AddRedisQueryStorage(s => _database)
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
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecutePersistedQuery_ApplicationDI()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            var storage = new RedisQueryStorage(_database);
            await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

            IRequestExecutor executor =
                await new ServiceCollection()
                    // we register the multiplexer on the application services
                    .AddSingleton(_multiplexer)
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    // and in the redis storage setup refer to that instance.
                    .AddRedisQueryStorage(sp => sp.GetRequiredService<IConnectionMultiplexer>())
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
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecutePersistedQuery_ApplicationDI_Default()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            var storage = new RedisQueryStorage(_database);
            await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

            IRequestExecutor executor =
                await new ServiceCollection()
                    // we register the multiplexer on the application services
                    .AddSingleton(_multiplexer)
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    // and in the redis storage setup refer to that instance.
                    .AddRedisQueryStorage()
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
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecutePersistedQuery_NotFound()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            var storage = new RedisQueryStorage(_database);
            await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    .AddRedisQueryStorage(s => _database)
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
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: "does_not_exist"));

            // assert
            result.MatchSnapshot();
        }
    }
}
