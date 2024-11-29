using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedOperations.Redis;

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
    public async Task ExecutePersistedOperation()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);

        await storage.SaveAsync(
            documentId,
            new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisOperationDocumentStorage(_ => _database)
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_After_Expiration()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisOperationDocumentStorage(_ => _database, TimeSpan.FromMilliseconds(10))
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // ... write document to cache
        var cache = executor.Services.GetRequiredService<IOperationDocumentStorage>();
        await cache.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        // ... wait for document to expire
        await Task.Delay(100);

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        Assert.Collection(
            result.ExpectOperationResult().Errors!,
            error =>
            {
                Assert.Equal("The specified persisted operation key is invalid.", error.Message);
                Assert.Equal("HC0020", error.Code);
            });
    }

    [Fact]
    public async Task ExecutePersistedOperation_Before_Expiration()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database, TimeSpan.FromMilliseconds(10000));
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisOperationDocumentStorage(_ => _database)
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        Assert.Null(result.ExpectOperationResult().Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_ApplicationDI()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                // we register the multiplexer on the application services
                .AddSingleton(_multiplexer)
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                // and in the redis storage setup refer to that instance.
                .AddRedisOperationDocumentStorage(sp => sp.GetRequiredService<IConnectionMultiplexer>())
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_ApplicationDI_Default()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                // we register the multiplexer on the application services
                .AddSingleton(_multiplexer)
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                // and in the redis storage setup refer to that instance.
                .AddRedisOperationDocumentStorage()
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_NotFound()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new RedisOperationDocumentStorage(_database);
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddRedisOperationDocumentStorage(_ => _database)
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        result.MatchSnapshot();
    }
}
