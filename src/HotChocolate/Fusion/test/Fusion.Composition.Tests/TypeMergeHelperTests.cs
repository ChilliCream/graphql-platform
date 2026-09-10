using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

public sealed class TypeMergeHelperTests
{
    [Theory]
    [InlineData("Int", "Int")]
    [InlineData("[Int]", "[Int]")]
    [InlineData("[[Int]]", "[[Int]]")]
    // Different nullability.
    [InlineData("Int", "Int!")]
    [InlineData("[Int]", "[Int!]")]
    [InlineData("[Int]", "[Int!]!")]
    [InlineData("[[Int]]", "[[Int!]]")]
    [InlineData("[[Int]]", "[[Int!]!]")]
    [InlineData("[[Int]]", "[[Int!]!]!")]
    public void SameTypeShape_True(string sdlTypeA, string sdlTypeB)
    {
        // arrange
        var schema1 = SchemaParser.Parse($$"""type Test { field: {{sdlTypeA}} }""");
        var schema2 = SchemaParser.Parse($$"""type Test { field: {{sdlTypeB}} }""");
        var typeA = ((MutableObjectTypeDefinition)schema1.Types["Test"]).Fields["field"].Type;
        var typeB = ((MutableObjectTypeDefinition)schema2.Types["Test"]).Fields["field"].Type;

        // act
        var result = TypeMergeHelper.SameTypeShape(typeA, typeB);

        // assert
        Assert.True(result);
    }

    [Theory]
    // Different type kind.
    [InlineData("Tag", "Tag")]
    [InlineData("[Tag]", "[Tag]")]
    [InlineData("[[Tag]]", "[[Tag]]")]
    // Different type name.
    [InlineData("String", "DateTime")]
    [InlineData("[String]", "[DateTime]")]
    [InlineData("[[String]]", "[[DateTime]]")]
    // Different depth.
    [InlineData("String", "[String]")]
    [InlineData("String", "[[String]]")]
    [InlineData("[String]", "[[String]]")]
    [InlineData("[[String]]", "[[[String]]]")]
    // Different depth and nullability.
    [InlineData("String", "[String!]")]
    [InlineData("String", "[String!]!")]
    public void SameTypeShape_False(string sdlTypeA, string sdlTypeB)
    {
        // arrange
        var schema1 = SchemaParser.Parse(
            $$"""
              type Test { field: {{sdlTypeA}} }

              type Tag { value: String }
              """);

        var schema2 = SchemaParser.Parse(
            $$"""
              type Test { field: {{sdlTypeB}} }

              scalar Tag
              """);

        var typeA = ((MutableObjectTypeDefinition)schema1.Types["Test"]).Fields["field"].Type;
        var typeB = ((MutableObjectTypeDefinition)schema2.Types["Test"]).Fields["field"].Type;

        // act
        var result = TypeMergeHelper.SameTypeShape(typeA, typeB);

        // assert
        Assert.False(result);
    }

    // The merged type is non-null only when every field type is non-null.
    [Fact]
    public void TryGetLeastRestrictiveType_AllNonNull_ReturnsNonNull()
    {
        // arrange
        var fields = OutputFieldTypes("Int!", "Int!");

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.IsType<NonNullType>(result);
        Assert.Equal("Int", result.NamedType().Name);
    }

    // When any field type is nullable, the merged type is nullable.
    [Fact]
    public void TryGetLeastRestrictiveType_AnyNullable_ReturnsNullable()
    {
        // arrange
        var fields = OutputFieldTypes("Int!", "Int");

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.False(result is NonNullType);
        Assert.Equal("Int", result!.NamedType().Name);
    }

    // List nullability is merged element-wise; any nullable source makes the part nullable.
    [Fact]
    public void TryGetLeastRestrictiveType_ListNullability_ReturnsNullableList()
    {
        // arrange
        var fields = OutputFieldTypes("[Int]!", "[Int!]");

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        var list = Assert.IsType<ListType>(result);
        Assert.False(list.ElementType is NonNullType);
    }

    // A list type and a non-list type cannot be merged.
    [Fact]
    public void TryGetLeastRestrictiveType_MixedListAndNonList_Fails()
    {
        // arrange
        var fields = OutputFieldTypes("Int", "[Int]");

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.False(success);
        Assert.Null(result);
    }

    // Leaf field types must be the same named type.
    [Fact]
    public void TryGetLeastRestrictiveType_DifferentScalars_Fails()
    {
        // arrange
        var fields = OutputFieldTypes("String", "Int");

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.False(success);
        Assert.Null(result);
    }

    // A union that contains the object type is the least restrictive return type.
    [Fact]
    public void TryGetLeastRestrictiveType_ObjectAndUnionSupertype_ReturnsUnion()
    {
        // arrange
        var fields = OutputFieldTypes(
            "featured",
            """
            type Query { featured: Product }
            type Product { id: ID }
            """,
            """
            type Query { featured: FeaturedItem }
            union FeaturedItem = Product
            type Product { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.Equal("FeaturedItem", result!.NamedType().Name);
    }

    // An interface implemented by the object type is the least restrictive return type.
    [Fact]
    public void TryGetLeastRestrictiveType_InterfaceSupertypeOfObject_ReturnsInterface()
    {
        // arrange
        var fields = OutputFieldTypes(
            "featured",
            """
            type Query { featured: Product }
            interface Node { id: ID }
            type Product implements Node { id: ID }
            """,
            """
            type Query { featured: Node }
            interface Node { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.Equal("Node", result!.NamedType().Name);
    }

    // Two source schemas declare a composite type with the same name but a different kind (interface
    // versus union), which cannot be unified.
    [Fact]
    public void TryGetLeastRestrictiveType_SameNameDifferentCompositeKind_Fails()
    {
        // arrange
        var fields = OutputFieldTypes(
            "thing",
            """
            type Query { thing: Thing }
            interface Thing { id: ID }
            type Foo implements Thing { id: ID }
            """,
            """
            type Query { thing: Thing }
            union Thing = Bar
            type Bar { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.False(success);
        Assert.Null(result);
    }

    // When no declared type is a supertype of all the others, the fields are not mergeable.
    [Fact]
    public void TryGetLeastRestrictiveType_NoCommonSupertype_Fails()
    {
        // arrange
        var fields = OutputFieldTypes(
            "featured",
            """
            type Query { featured: FeaturedItem }
            union FeaturedItem = Product
            type Product { id: ID }
            """,
            """
            type Query { featured: Review }
            type Review { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.False(success);
        Assert.Null(result);
    }

    // The same union is declared in multiple schemas with different members. The union covers the
    // object type because its members are aggregated across every schema, not read from a single one.
    [Fact]
    public void TryGetLeastRestrictiveType_UnionMembersSplitAcrossSchemas_ReturnsUnion()
    {
        // arrange
        var fields = OutputFieldTypes(
            "featured",
            """
            type Query { featured: FeaturedItem }
            union FeaturedItem = Product
            type Product { id: ID }
            """,
            """
            type Query { featured: Product }
            type Product { id: ID }
            """,
            """
            type Query { featured: FeaturedItem }
            union FeaturedItem = Review
            type Review { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.Equal("FeaturedItem", result!.NamedType().Name);
    }

    // The selected return type does not depend on source schema order.
    [Fact]
    public void TryGetLeastRestrictiveType_OrderIndependent_SelectsSameSupertype()
    {
        // arrange
        const string objectSchema =
            """
            type Query { featured: Product }
            type Product { id: ID }
            """;
        const string unionSchema =
            """
            type Query { featured: FeaturedItem }
            union FeaturedItem = Product
            type Product { id: ID }
            """;

        // act
        var forward = TypeMergeHelper.TryGetLeastRestrictiveType(
            OutputFieldTypes("featured", objectSchema, unionSchema), out var forwardResult);
        var reverse = TypeMergeHelper.TryGetLeastRestrictiveType(
            OutputFieldTypes("featured", unionSchema, objectSchema), out var reverseResult);

        // assert
        Assert.True(forward);
        Assert.True(reverse);
        Assert.Equal(forwardResult!.NamedType().Name, reverseResult!.NamedType().Name);
    }

    // When multiple candidates equally qualify, the tie is broken by type name.
    [Fact]
    public void TryGetLeastRestrictiveType_EquallyQualifyingCandidates_BreaksTieByName()
    {
        // arrange
        var fields = OutputFieldTypes(
            "featured",
            """
            type Query { featured: AItem }
            union AItem = Product
            type Product { id: ID }
            """,
            """
            type Query { featured: ZItem }
            union ZItem = Product
            type Product { id: ID }
            """);

        // act
        var success = TypeMergeHelper.TryGetLeastRestrictiveType(fields, out var result);

        // assert
        Assert.True(success);
        Assert.Equal("AItem", result!.NamedType().Name);
    }

    private static List<(IType Type, MutableSchemaDefinition Schema)> OutputFieldTypes(
        string sdlTypeA,
        string sdlTypeB)
        => OutputFieldTypes(
            "field",
            $$"""type Test { field: {{sdlTypeA}} }""",
            $$"""type Test { field: {{sdlTypeB}} }""");

    private static List<(IType Type, MutableSchemaDefinition Schema)> OutputFieldTypes(
        string fieldName,
        params string[] sources)
    {
        var typeName = fieldName == "field" ? "Test" : "Query";
        var result = new List<(IType, MutableSchemaDefinition)>();

        foreach (var source in sources)
        {
            var schema = SchemaParser.Parse(source);
            var type = (MutableComplexTypeDefinition)schema.Types[typeName];
            result.Add((type.Fields[fieldName].Type, schema));
        }

        return result;
    }
}
