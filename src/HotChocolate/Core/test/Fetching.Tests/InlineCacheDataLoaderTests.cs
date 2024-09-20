using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

public class InlineCacheDataLoaderTests
{
    [Fact]
    public async Task LoadWithDifferentDataLoader()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result1 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();
        var result2 = await executor.ExecuteAsync("{ byKey(key: \"def\") }").ToJsonAsync();

        // assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public async Task LoadWithDifferentDataLoader_ShortHand()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildRequestExecutorAsync();

        // act
        var result1 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();
        var result2 = await executor.ExecuteAsync("{ byKey(key: \"def\") }").ToJsonAsync();

        // assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public async Task LoadWithSingleKeyDataLoader()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result1 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();
        var result2 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();

        // assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task LoadWithSingleKeyDataLoader_ShortHand()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildRequestExecutorAsync();

        // act
        var result1 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();
        var result2 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();

        // assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task CacheOnce()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query3>()
                .BuildRequestExecutorAsync();

        // act
        var result1 = await executor.ExecuteAsync("{ byKey(key: \"abc\") }").ToJsonAsync();
        var result2 = await executor.ExecuteAsync("{ byKey(key: \"def\") }").ToJsonAsync();

        // assert
        Assert.NotEqual(result1, result2);
    }

    public class Query
    {
        public async Task<string?> GetByKey(string key, IResolverContext context)
        {
            return await context
                .CacheDataLoader<string, string>(
                    (_, _) => Task.FromResult(key),
                    key)
                .LoadAsync("abc", context.RequestAborted);
        }
    }

    public class Query2
    {
        public async Task<string?> GetByKey(string key, IResolverContext context)
        {
            return await context.CacheAsync(FetchAsync, key);

            Task<string> FetchAsync(string id, CancellationToken cancellationToken)
                => Task.FromResult(key);
        }
    }

    public class Query3
    {
        public async Task<string?> GetByKey(string key, IResolverContext context)
        {
            return await context.CacheAsync(FetchAsync, key);

            Task<string> FetchAsync(CancellationToken cancellationToken)
                => Task.FromResult(key);
        }
    }
}
