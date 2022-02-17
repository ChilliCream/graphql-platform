using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Text;
using Snapshooter.Xunit;
using System;
using HotChocolate.Execution.Configuration;

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

    [Fact]
    public void NegativeMaxAge()
    {
        ExpectErrors(builder => builder
            .AddDocumentFromString(@"
                type Query {
                    field: String @cacheControl(maxAge: -10)
                }
            ")
            .Use(_ => _ => default)
            .AddCacheControl());
    }

    [Fact]
    public void MaxAgeAndInheritMaxAgeOnSameField()
    {
        ExpectErrors(builder => builder
            .AddDocumentFromString(@"
                type Query {
                    field: String @cacheControl(maxAge: 10 inheritMaxAge: true)
                }
            ")
            .Use(_ => _ => default)
            .AddCacheControl());
    }

    [Fact(Skip = "Not yet implemented")]
    public void InheritMaxAgeOnObjectType()
    {
        ExpectErrors(builder => builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType
                }

                type ObjectType @cacheControl(inheritMaxAge: true) {
                    field: String
                }
            ")
            .Use(_ => _ => default)
            .AddCacheControl());
    }

    [Fact(Skip = "Not yet implemented")]
    public void InheritMaxAgeOnInterfaceType()
    {
        ExpectErrors(builder => builder
            .AddDocumentFromString(@"
                type Query {
                    field: InterfaceType
                }

                type InterfaceType @cacheControl(inheritMaxAge: true) {
                    field: String
                }

                type ObjectType {
                    field: String
                }
            ")
            .Use(_ => _ => default)
            .AddCacheControl());
    }

    [Fact(Skip = "Not yet implemented")]
    public void InheritMaxAgeOnUnionType()
    {
        ExpectErrors(builder => builder
            .AddDocumentFromString(@"
                type Query {
                    field: UnionType
                }

                union UnionType @cacheControl(inheritMaxAge: true) = ObjectType

                type ObjectType {
                    field: String
                }
            ")
            .Use(_ => _ => default)
            .AddCacheControl());
    }

    private static void ExpectErrors(Action<SchemaBuilder> configureBuilder)
    {
        try
        {
            var builder = SchemaBuilder.New();

            configureBuilder(builder);

            builder.Create();

            Assert.False(true, "Expected error!");
        }
        catch (SchemaException ex)
        {
            Assert.NotEmpty(ex.Errors);

            var text = new StringBuilder();

            foreach (ISchemaError error in ex.Errors)
            {
                text.AppendLine(error.ToString());
                text.AppendLine();
            }

            text.ToString().MatchSnapshot();
        }
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