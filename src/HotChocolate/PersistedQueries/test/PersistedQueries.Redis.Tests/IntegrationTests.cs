using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedQueries.Redis;

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

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisQueryStorage(_ => _database)
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
        var result = await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedQuery_After_Expiration()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisQueryStorage(_ => _database, TimeSpan.FromMilliseconds(10))
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

        // ... write query to cache
        var cache = executor.Services.GetRequiredService<IWriteStoredQueries>();
        await cache.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

        // ... wait for query to expire
        await Task.Delay(100).ConfigureAwait(false);

        // act
        var result = await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

        // assert
        Assert.Collection(
            result.ExpectQueryResult().Errors!,
            error =>
            {
                Assert.Equal("The query request contains no document.", error.Message);
                Assert.Equal("HC0015", error.Code);
            });
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedQuery_Before_Expiration()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");
        var storage = new RedisQueryStorage(_database, TimeSpan.FromMilliseconds(10000));
        await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisQueryStorage(_ => _database)
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
        var result = await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

        // assert
        Assert.Null(result.ExpectQueryResult().Errors);
        result.MatchSnapshot();

    }

    [Fact]
    public async Task ExecutePersistedQuery_ApplicationDI()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");
        var storage = new RedisQueryStorage(_database);
        await storage.WriteQueryAsync(queryId, new QuerySourceText("{ __typename }"));

        var executor =
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
        var result =
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

        var executor =
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
        var result =
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

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisQueryStorage(_ => _database)
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
        var result =
            await executor.ExecuteAsync(new QueryRequest(queryId: "does_not_exist"));

        // assert
        result.MatchSnapshot();
    }
}
