using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public abstract class CacheControlTestBase
{
    public Task<ICacheControlResult> ValidateResultAsync(
        string query,
        Action<IRequestExecutorBuilder>? configureExecutor = null)
    {
        return ValidateResultAsync(executor => executor.ExecuteAsync(query),
            configureExecutor);
    }

    public async Task<ICacheControlResult> ValidateResultAsync(
        Func<IRequestExecutor, Task<IExecutionResult>> executeRequest,
        Action<IRequestExecutorBuilder>? configureExecutor = null)
    {
        var cache = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        configureExecutor?.Invoke(builder);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executeRequest(executor);

        Assert.Null(result.Errors);
        Assert.NotEmpty(cache.Writes);

        return cache.Writes.First().Result;
    }

    public class QueryCache : DefaultQueryCache
    {
        public List<ReadArgs> Reads { get; } = new List<ReadArgs>();
        public List<WriteArgs> Writes { get; } = new List<WriteArgs>();
        public List<bool> ShouldReads { get; } = new List<bool>();
        public List<bool> ShouldWrites { get; } = new List<bool>();
        public bool ReturnResult { get; }
        public bool SkipWrite { get; }
        public bool SkipRead { get; }
        public bool ThrowInShouldRead { get; }
        public bool ThrowInShouldWrite { get; }
        public bool ThrowInRead { get; }
        public bool ThrowInWrite { get; }

        public QueryCache(bool returnResult = false, bool skipWrite = false, bool skipRead = false,
            bool throwInShouldRead = false, bool throwInShouldWrite = false,
            bool throwInRead = false, bool throwInWrite = false)
        {
            ReturnResult = returnResult;
            SkipWrite = skipWrite;
            SkipRead = skipRead;
            ThrowInShouldRead = throwInShouldRead;
            ThrowInShouldWrite = throwInShouldWrite;
            ThrowInRead = throwInRead;
            ThrowInWrite = throwInWrite;
        }

        public override bool ShouldReadResultFromCache(IRequestContext context)
        {
            if (ThrowInShouldRead)
            {
                throw new Exception();
            }

            bool result;

            if (SkipRead)
            {
                result = false;
            }

            else
            {
                result = base.ShouldReadResultFromCache(context);
            }

            ShouldReads.Add(result);

            return result;
        }

        public override bool ShouldWriteResultToCache(IRequestContext context)
        {
            if (ThrowInShouldWrite)
            {
                throw new Exception();
            }

            bool result;

            if (SkipWrite)
            {
                result = false;
            }
            else
            {
                result = base.ShouldWriteResultToCache(context);
            }

            ShouldWrites.Add(result);

            return result;
        }

        public override Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
            ICacheControlOptions options)
        {
            if (ThrowInRead)
            {
                throw new Exception();
            }

            Reads.Add(new(options));

            if (ReturnResult)
            {
                IQueryResult result = QueryResultBuilder.New()
                    .SetData(new Dictionary<string, object?>())
                    .Create();

                return Task.FromResult<IQueryResult?>(result);
            }

            return Task.FromResult<IQueryResult?>(null);
        }

        public override Task CacheQueryResultAsync(IRequestContext context, ICacheControlResult result,
            ICacheControlOptions options)
        {
            if (ThrowInWrite)
            {
                throw new Exception();
            }

            Writes.Add(new(result, options));

            return Task.CompletedTask;
        }

        public class ReadArgs
        {
            public ICacheControlOptions Options { get; }

            public ReadArgs(ICacheControlOptions options)
            {
                Options = options;
            }
        }

        public class WriteArgs
        {
            public ICacheControlResult Result { get; }

            public WriteArgs(ICacheControlResult result, ICacheControlOptions options)
            {
                Result = result;
                Options = options;
            }

            public ICacheControlOptions Options { get; }
        }
    }
}