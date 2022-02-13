using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlTypeInterceptorTests
{
    [Fact]
    public async Task ApplyDefaults()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddCacheControl()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ApplyDefaults_DefaultMaxAge()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddCacheControl()
            .ModifyCacheControlOptions(o => o.DefaultMaxAge = 666)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ApplyDefaults_Disabled()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddCacheControl()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ApplyDefaults_DataResolvers()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddCacheControl()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class Query : NestedType
    {
        public NestedType Nested { get; } = new();
    }

    public class NestedType
    {
        public string PureField { get; } = default!;

        public Task<string> TaskField() => default!;

        public ValueTask<string> ValueTaskField() => default!;

        public IExecutable<string> ExecutableField() => default!;

        public IQueryable<string> QueryableField() => default!;

        [UsePaging]
        public IQueryable<string> QueryableFieldWithConnection() => default!;

        [UseOffsetPaging]
        public IQueryable<string> QueryableFieldWithCollectionSegment() => default!;
    }
}