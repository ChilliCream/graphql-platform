using System.Collections.Generic;
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
            .AddType<Contract1>()
            .AddType<Contract2>()
            .AddType<Contract3>()
            .AddType<Contract4>()
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

    public class Query : NestedType
    {
        // public string PureField { get; } = "PureField";

        // public Task<string> TaskField() => default!;

        // public ValueTask<string> ValueTaskField() => default!;

        // public IExecutable<string> ExecutableField() => default!;

        // public IQueryable<string> QueryableField() => default!;

        public NestedType Nested { get; } = new();
    }

    public class NestedType
    {
        #region Scalars
        public string? Scalar() => default!;

        [CacheControl(500)]
        public string? Scalar_FieldCache() => default!;

        public string NonNullScalar() => default!;

        [CacheControl(500)]
        public string NonNullScalar_FieldCache() => default!;

        public List<string?> ScalarList() => default!;

        [CacheControl(500)]
        public List<string?> ScalarList_FieldCache() => default!;

        public List<string> NonNullScalarList() => default!;

        [CacheControl(500)]
        public List<string> NonNullScalarList_FieldCache() => default!;
        #endregion

        #region Object Types
        public User ObjectType() => default!;

        [CacheControl(500)]
        public User ObjectType_FieldCache() => default!;

        public CachableUser ObjectType_TypeCache() => default!;

        [CacheControl(500)]
        public CachableUser ObjectType_FieldCache_TypeCache() => default!;
        #endregion

        #region Interface Types
        public IContract InterfaceType() => default!;

        [CacheControl(500)]
        public IContract InterfaceType_FieldCache() => default!;

        public ICachableContract InterfaceType_TypeCache() => default!;

        [CacheControl(500)]
        public ICachableContract InterfaceType_FieldCache_TypeCache() => default!;
        #endregion

        #region Union Types
        public IPost UnionType() => default!;

        [CacheControl(500)]
        public IPost UnionType_FieldCache() => default!;

        public ICachablePost UnionType_TypeCache() => default!;

        [CacheControl(500)]
        public ICachablePost UnionType_FieldCache_TypeCache() => default!;
        #endregion
    }

    public class Mutation
    {
        public string AddString() => default!;
    }

    public class Subscription
    {
        public ISourceStream<string> OnEvent() => default!;
    }

    public class User
    {
        public int MyProperty { get; set; }
    }

    [CacheControl(120)]
    public class CachableUser
    {
        public int MyProperty { get; set; }
    }

    [InterfaceType("Contract")]
    public interface IContract
    {
        int MyProperty { get; set; }
    }

    [InterfaceType("CachableContract")]
    [CacheControl(180)]
    public interface ICachableContract
    {
        int MyProperty { get; set; }
    }

    public class Contract1 : IContract
    {
        public int MyProperty { get; set; }
    }

    public class Contract2 : IContract
    {
        public int MyProperty { get; set; }
    }

    public class Contract3 : ICachableContract
    {
        public int MyProperty { get; set; }
    }

    public class Contract4 : ICachableContract
    {
        public int MyProperty { get; set; }
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