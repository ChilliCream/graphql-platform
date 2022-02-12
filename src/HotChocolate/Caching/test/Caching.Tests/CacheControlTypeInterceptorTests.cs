using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlTypeInterceptorTests
{
    [Fact]
    public async Task CacheControlTypeInterceptor_ApplyDefaults()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddType<TextPost>()
            .AddType<PicturePost>()
            .AddCacheControl()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    // [Fact]
    // public async Task CacheControlTypeInterceptor_ApplyDefaults_DefaultMaxAge()
    // {
    //     await new ServiceCollection()
    //         .AddGraphQL()
    //         .AddQueryType<Query>()
    //         .AddCacheControl()
    //         .ModifyCacheControlOptions(o => o.DefaultMaxAge = 666)
    //         .BuildSchemaAsync()
    //         .MatchSnapshotAsync();
    // }

    // [Fact]
    // public async Task CacheControlTypeInterceptor_ApplyDefaults_Disabled()
    // {
    //     await new ServiceCollection()
    //         .AddGraphQL()
    //         .AddQueryType<Query>()
    //         .AddCacheControl()
    //         .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
    //         .BuildSchemaAsync()
    //         .MatchSnapshotAsync();
    // }

    public class Query
    {
        // public string PureField { get; } = "PureField";

        // public Task<string> TaskField() => default!;

        // public ValueTask<string> ValueTaskField() => default!;

        // public IExecutable<string> ExecutableField() => default!;

        // public IQueryable<string> QueryableField() => default!;

        public IPost UnionTypeHasNoCache() => default!;

        [CacheControl(500)]
        public IPost UnionTypeHasNoCacheButFieldHas() => default!;

        public ICachablePost UnionTypeHasCache() => default!;

        [CacheControl(500)]
        public ICachablePost UnionTypeHasCacheAndFieldHas() => default!;
    }

    public class Mutation
    {
        public string AddString() => default!;
    }

    public class Subscription
    {
        public ISourceStream<string> OnEvent() => default!;
    }

    [UnionType("Post")]
    public interface IPost { }

    [UnionType("CachablePost")]
    [CacheControl(60)]
    public interface ICachablePost { }

    public class TextPost : IPost, ICachablePost
    {
        public int MyProperty { get; set; }
    }

    public class PicturePost : IPost, ICachablePost
    {
        public int MyProperty { get; set; }
    }
}