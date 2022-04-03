using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Fetching;

public class InlineBatchDataLoaderTests
{
    [Fact]
    public async Task LoadWithDifferentDataLoader()
    {
        // arrange
        IRequestExecutor executor =
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
    public async Task LoadWithSingleKeyDataLoader()
    {
        // arrange
        IRequestExecutor executor =
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

    public class Query
    {
        public async Task<string> GetByKey(string key, IResolverContext context)
        {
            return await context
                .BatchDataLoader<string, string>(
                    (keys, _) =>
                        Task.FromResult<IReadOnlyDictionary<string, string>>(
                            keys.ToDictionary(t => t, _ => key)),
                    key)
                .LoadAsync("abc", context.RequestAborted);
        }
    }
}
