#nullable enable

using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class SemanticNonNullTests
{
    [Fact]
    public async Task Object_With_Id_Field()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
                o.EnsureAllNodesCanBeResolved = false;
            })
            .AddQueryType<QueryWithTypeWithId>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Interface_With_Id_Field()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddQueryType<QueryWithInteface>()
            .AddType<InterfaceImplementingNode>()
            .UseField(_ => _ => default)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MutationConventions()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.StrictValidation = false;
                o.EnableSemanticNonNull = true;
            })
            .AddMutationConventions(applyToAllMutations: false)
            .AddMutationType<Mutation>()
            .AddTypeExtension<MutationExtensions>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Pagination()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o =>
            {
                o.EnableSemanticNonNull = true;
            })
            .AddQueryType<QueryWithPagination>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

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
    public async Task Derive_SemanticNonNull_From_ImplementationFirst_With_GraphQLType_As_Type()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddQueryType<QueryWithTypeAttribute>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Derive_SemanticNonNull_From_ImplementationFirst_With_GraphQLType_As_String()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableSemanticNonNull = true)
            .AddType<Foo>()
            .AddQueryType<QueryWithTypeAttributeAsString>()
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

    public class QueryWithInteface
    {
        public SomeObject GetSomeObject() => new();
    }

    [Node]
    [ImplementsInterface<InterfaceImplementingNode>]
    public record SomeObject
    {
        public int Id { get; set; }

        public string Field { get; set; } = null!;

        public static SomeObject? Get(int id) => new();
    }

    public class InterfaceImplementingNode : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Implements<NodeType>();
            descriptor
                .Field("field")
                .Type("String!");
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ImplementsInterfaceAttribute<T> : ObjectTypeDescriptorAttribute
        where T : InterfaceType
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type) => descriptor.Implements<T>();
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

    [ObjectType("Query")]
    public class QueryWithTypeAttribute
    {
        [GraphQLType<StringType>]
        public string? Scalar { get; }

        [GraphQLType<NonNullType<StringType>>]
        public string NonNulScalar { get; } = null!;

        [GraphQLType<ListType<StringType>>]
        public string?[]? ScalarArray { get; }

        [GraphQLType<NonNullType<ListType<NonNullType<StringType>>>>]
        public string[] NonNullScalarArray { get; } = null!;

        [GraphQLType<NonNullType<ListType<StringType>>>]
        public string?[] OuterNonNullScalarArray { get; } = null!;

        [GraphQLType<ListType<ListType<StringType>>>]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLType<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>]
        public string[][] NonNullScalarNestedArray { get; } = null!;

        [GraphQLType<NonNullType<ListType<ListType<NonNullType<StringType>>>>>]
        public string[]?[] InnerNonNullScalarNestedArray { get; } = null!;

        [GraphQLType<FooType>]
        public Foo? Object { get; }

        [GraphQLType<NonNullType<FooType>>]
        public Foo NonNullObject { get; } = null!;

        [GraphQLType<ListType<FooType>>]
        public Foo?[]? ObjectArray { get; }

        [GraphQLType<NonNullType<ListType<NonNullType<FooType>>>>]
        public Foo[] NonNullObjectArray { get; } = null!;

        [GraphQLType<ListType<ListType<FooType>>>]
        public Foo?[]?[]? ObjectNestedArray { get; }

        [GraphQLType<NonNullType<ListType<NonNullType<ListType<NonNullType<FooType>>>>>>]
        public Foo[][] NonNullObjectNestedArray { get; } = null!;

        [GraphQLType<NonNullType<ListType<ListType<NonNullType<FooType>>>>>]
        public Foo[]?[] InnerNonNullObjectNestedArray { get; } = null!;
    }

    [ObjectType("Query")]
    public class QueryWithTypeAttributeAsString
    {
        [GraphQLType("String")]
        public string? Scalar { get; }

        [GraphQLType("String!")]
        public string NonNulScalar { get; } = null!;

        [GraphQLType("[String]")]
        public string?[]? ScalarArray { get; }

        [GraphQLType("[String!]!")]
        public string[] NonNullScalarArray { get; } = null!;

        [GraphQLType("[String]!")]
        public string?[] OuterNonNullScalarArray { get; } = null!;

        [GraphQLType("[[String]]")]
        public string?[]?[]? ScalarNestedArray { get; }

        [GraphQLType("[[String!]!]!")]
        public string[][] NonNullScalarNestedArray { get; } = null!;

        [GraphQLType("[[String!]]!")]
        public string[]?[] InnerNonNullScalarNestedArray { get; } = null!;

        [GraphQLType("Foo")]
        public Foo? Object { get; }

        [GraphQLType("Foo!")]
        public Foo NonNullObject { get; } = null!;

        [GraphQLType("[Foo]")]
        public Foo?[]? ObjectArray { get; }

        [GraphQLType("[Foo!]!")]
        public Foo[] NonNullObjectArray { get; } = null!;

        [GraphQLType("[[Foo]]")]
        public Foo?[]?[]? ObjectNestedArray { get; }

        [GraphQLType("[[Foo!]!]!")]
        public Foo[][] NonNullObjectNestedArray { get; } = null!;

        [GraphQLType("[[Foo!]]!")]
        public Foo[]?[] InnerNonNullObjectNestedArray { get; } = null!;
    }

    public class Foo
    {
        public string Bar { get; } = null!;
    }

    [ObjectType("Query")]
    public class QueryWithTypeWithId
    {
        public MyType GetMyNode() => new(1);
    }

    public record MyType([property: ID] int Id);

    public class Mutation
    {
        [UseMutationConvention]
        [Error<MyException>]
        public bool DoSomething() => true;
    }

    [ExtendObjectType<Mutation>]
    public class MutationExtensions
    {
        public bool DoSomethingElse() => true;
    }

    public class MyException : Exception;

    public class QueryWithPagination
    {
        [UsePaging]
        public string[] GetCursorPagination() => [];

        [UseOffsetPaging]
        public string[] GetOffsetPagination() => [];
    }
}
