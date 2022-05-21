using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Caching.Tests;

public abstract class CacheControlTestBase
{
    protected Mock<DefaultQueryCache> GetMockedCache()
    {
        return new Mock<DefaultQueryCache>() { CallBase = true };
    }

    protected void AssertNoWritesToCache(Mock<DefaultQueryCache> cacheMock)
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Never());
    }

    protected void AssertOneWriteToCache(Mock<DefaultQueryCache> cacheMock)
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    //public Task<ICacheControlResult> ValidateResultAsync(
    //    string query,
    //    Action<IRequestExecutorBuilder>? configureExecutor = null)
    //{
    //    return ValidateResultAsync(executor => executor.ExecuteAsync(query),
    //        configureExecutor);
    //}

    //public async Task<ICacheControlResult> ValidateResultAsync(
    //    Func<IRequestExecutor, Task<IExecutionResult>> executeRequest,
    //    Action<IRequestExecutorBuilder>? configureExecutor = null)
    //{
    //    var cache = new QueryCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryCache(_ => cache)
    //        .UseQueryCachePipeline()
    //        .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
    //        .UseField(_ => _ => default)
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    configureExecutor?.Invoke(builder);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executeRequest(executor);
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);
    //    Assert.NotEmpty(cache.Writes);

    //    return cache.Writes.First().Result;
    //}
}
