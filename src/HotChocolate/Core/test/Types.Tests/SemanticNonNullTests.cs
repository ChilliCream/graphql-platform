#nullable enable

using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class SemanticNonNullTests
{
    [Fact]
    public async Task Derive_SemanticNonNull_From_ImplementationFirst()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddQueryType<Query>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Derive_SemanticNonNull_From_CodeFirst()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddQueryType<QueryType>()
            .UseField(_ => _ => default)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Apply_SemanticNonNull_To_SchemaFirst()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddDocumentFromString(
                """
                type Query {
                  scalar: String
                  nonNulScalar: String!
                  scalarArray: [String]
                  nonNullScalarArray: [String!]!
                  outerNonNullScalarArray: [String]!
                  scalarNestedArray: [[String]]
                  nonNullScalarNestedArray: [[String!]!]!
                  innerNonNullScalarNestedArray: [[String!]]!
                  object: Foo
                  nonNullObject: Foo!
                  objectArray: [Foo]
                  nonNullObjectArray: [Foo!]!
                  objectNestedArray: [[Foo]]
                  nonNullObjectNestedArray: [[Foo!]!]!
                  innerNonNullObjectNestedArray: [[Foo!]]!
                }

                type Foo {
                  bar: String!
                }
                """)
            .UseField(_ => _ => default)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("scalar").Type<StringType>();
            descriptor.Field("nonNulScalar").Type<NonNullType<StringType>>();
            descriptor.Field("scalarArray").Type<ListType<StringType>>();
            descriptor.Field("nonNullScalarArray").Type<NonNullType<ListType<NonNullType<StringType>>>>();
            descriptor.Field("outerNonNullScalarArray").Type<NonNullType<ListType<StringType>>>();
            descriptor.Field("scalarNestedArray").Type<ListType<ListType<StringType>>>();
            descriptor.Field("nonNullScalarNestedArray").Type<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>();
            descriptor.Field("innerNonNullScalarNestedArray").Type<NonNullType<ListType<ListType<NonNullType<StringType>>>>>();
            descriptor.Field("object").Type<FooType>();
            descriptor.Field("nonNullObject").Type<NonNullType<FooType>>();
            descriptor.Field("objectArray").Type<ListType<FooType>>();
            descriptor.Field("nonNullObjectArray").Type<NonNullType<ListType<NonNullType<FooType>>>>();
            descriptor.Field("objectNestedArray").Type<ListType<ListType<FooType>>>();
            descriptor.Field("nonNullObjectNestedArray").Type<NonNullType<ListType<NonNullType<ListType<NonNullType<FooType>>>>>>();
            descriptor.Field("innerNonNullObjectNestedArray").Type<NonNullType<ListType<ListType<NonNullType<FooType>>>>>();
        }
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("bar").Type<NonNullType<StringType>>();
        }
    }

    public class Query
    {
        public string? Scalar { get; }

        public string NonNulScalar { get; } = null!;

        public string?[]? ScalarArray { get; }

        public string[] NonNullScalarArray { get; } = null!;

        public string?[] OuterNonNullScalarArray { get; } = null!;

        public string?[]?[]? ScalarNestedArray { get; }

        public string[][] NonNullScalarNestedArray { get; } = null!;

        public string[]?[] InnerNonNullScalarNestedArray { get; } = null!;

        public Foo? Object { get; }

        public Foo NonNullObject { get; } = null!;

        public Foo?[]? ObjectArray { get; }

        public Foo[] NonNullObjectArray { get; } = null!;

        public Foo?[]?[]? ObjectNestedArray { get; }

        public Foo[][] NonNullObjectNestedArray { get; } = null!;

        public Foo[]?[] InnerNonNullObjectNestedArray { get; } = null!;
    }

    public class Foo
    {
        public string Bar { get; } = default!;
    }
}
