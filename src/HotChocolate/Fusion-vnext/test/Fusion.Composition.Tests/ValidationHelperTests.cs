using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

public sealed class ValidationHelperTests
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
        var typeA = ((ObjectTypeDefinition)schema1.Types["Test"]).Fields["field"].Type;
        var typeB = ((ObjectTypeDefinition)schema2.Types["Test"]).Fields["field"].Type;

        // act
        var result = ValidationHelper.SameTypeShape(typeA, typeB);

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

        var typeA = ((ObjectTypeDefinition)schema1.Types["Test"]).Fields["field"].Type;
        var typeB = ((ObjectTypeDefinition)schema2.Types["Test"]).Fields["field"].Type;

        // act
        var result = ValidationHelper.SameTypeShape(typeA, typeB);

        // assert
        Assert.False(result);
    }
}
