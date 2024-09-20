#nullable enable
using System.ComponentModel.DataAnnotations;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class GraphQLNonNullTypeTests
{
    [Fact]
    public async Task GraphQLNonNull_Should_RewriteFirstToNonNull_When_NoParametersAreSet()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLNonNull_Should_RewriteToNonNull_When_ParametersAreSet()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDeep>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLNonNull_Should_RewriteToNonNull_When_GraphQLTypeIsUsed()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDeepWithType>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLNonNull_Should_RewriteFirstToNonNull_When_RequiredIsSet()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryRequired>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task GraphQLNonNull_Should_RewriteFirstToNonNull_When_RequiredAndTypeIsSet()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryRequiredWithType>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLNonNullType()]
        public string? Scalar { get; }

        [GraphQLNonNullType]
        public string?[]? ScalarArray { get; }

        [GraphQLNonNullType]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLNonNullType]
        public Foo? Object { get; }

        [GraphQLNonNullType]
        public Foo?[]? ObjectArray { get; }

        [GraphQLNonNullType]
        public Foo?[]?[]? ObjectNestedArray { get; }
    }

    public class QueryRequired
    {
        [Required]
        public string? Scalar { get; }

        [Required]
        public string?[]? ScalarArray { get; }

        [Required]
        public string?[]?[]? ScalarNestedArray { get; }

        [Required]
        public Foo? Object { get; }

        [Required]
        public Foo?[]? ObjectArray { get; }

        [Required]
        public Foo?[]?[]? ObjectNestedArray { get; }
    }

    public class QueryDeep
    {
        [GraphQLNonNullType()]
        public string? Scalar { get; }

        [GraphQLNonNullType(false, false)]
        public string?[]? ScalarArray { get; }

        [GraphQLNonNullType(false, false,false)]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLNonNullType]
        public Foo? Object { get; }

        [GraphQLNonNullType(false, false)]
        public Foo?[]? ObjectArray { get; }

        [GraphQLNonNullType(false, false,false)]
        public Foo?[]?[]? ObjectNestedArray { get; }
    }

    public class QueryRequiredWithType
    {
        [Required]
        [GraphQLType(typeof(IdType))]
        public string Scalar { get; } = default!;

        [Required]
        [GraphQLType(typeof(ListType<IdType>))]
        public string[] ScalarArray { get; } = default!;

        [Required]
        [GraphQLType(typeof(ListType<ListType<IdType>>))]
        public string[][] ScalarNestedArray { get; } = default!;

        [Required]
        [GraphQLType(typeof(FooType))]
        public Foo Object { get; } = default!;

        [Required]
        [GraphQLType(typeof(ListType<FooType>))]
        public Foo[] ObjectArray { get; } = default!;

        [Required]
        [GraphQLType(typeof(ListType<ListType<FooType>>))]
        public Foo[][] ObjectNestedArray { get; } = default!;
    }

    public class QueryDeepWithType
    {
        [GraphQLNonNullType()]
        [GraphQLType(typeof(IdType))]
        public string? Scalar { get; }

        [GraphQLNonNullType(false, false)]
        [GraphQLType(typeof(ListType<IdType>))]
        public string?[]? ScalarArray { get; }

        [GraphQLNonNullType(false, false,false)]
        [GraphQLType(typeof(ListType<ListType<IdType>>))]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLNonNullType]
        [GraphQLType(typeof(FooType))]
        public Foo? Object { get; }

        [GraphQLNonNullType(false, false)]
        [GraphQLType(typeof(ListType<FooType>))]
        public Foo?[]? ObjectArray { get; }

        [GraphQLNonNullType(false, false,false)]
        [GraphQLType(typeof(ListType<ListType<FooType>>))]
        public Foo?[]?[]? ObjectNestedArray { get; }
    }

    public class Foo
    {
        public string Bar { get; } = default!;
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("CustomType");
            descriptor.Field("bar").Resolve(10);
        }
    }
}
