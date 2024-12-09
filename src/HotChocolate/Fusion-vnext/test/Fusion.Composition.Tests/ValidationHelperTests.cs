using HotChocolate.Fusion;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition;

public sealed class ValidationHelperTests
{
    [Test]
    [Arguments("Int", "Int")]
    [Arguments("[Int]", "[Int]")]
    [Arguments("[[Int]]", "[[Int]]")]
    // Different nullability.
    [Arguments("Int", "Int!")]
    [Arguments("[Int]", "[Int!]")]
    [Arguments("[Int]", "[Int!]!")]
    [Arguments("[[Int]]", "[[Int!]]")]
    [Arguments("[[Int]]", "[[Int!]!]")]
    [Arguments("[[Int]]", "[[Int!]!]!")]
    public async Task SameTypeShape_True(string sdlTypeA, string sdlTypeB)
    {
        // arrange
        var schema1 = SchemaParser.Parse($$"""type Test { field: {{sdlTypeA}} }""");
        var schema2 = SchemaParser.Parse($$"""type Test { field: {{sdlTypeB}} }""");
        var typeA = ((ObjectTypeDefinition)schema1.Types["Test"]).Fields["field"].Type;
        var typeB = ((ObjectTypeDefinition)schema2.Types["Test"]).Fields["field"].Type;

        // act
        var result = ValidationHelper.SameTypeShape(typeA, typeB);

        // assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    // Different type kind.
    [Arguments("Tag", "Tag")]
    [Arguments("[Tag]", "[Tag]")]
    [Arguments("[[Tag]]", "[[Tag]]")]
    // Different type name.
    [Arguments("String", "DateTime")]
    [Arguments("[String]", "[DateTime]")]
    [Arguments("[[String]]", "[[DateTime]]")]
    // Different depth.
    [Arguments("String", "[String]")]
    [Arguments("String", "[[String]]")]
    [Arguments("[String]", "[[String]]")]
    [Arguments("[[String]]", "[[[String]]]")]
    // Different depth and nullability.
    [Arguments("String", "[String!]")]
    [Arguments("String", "[String!]!")]
    public async Task SameTypeShape_False(string sdlTypeA, string sdlTypeB)
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
        await Assert.That(result).IsFalse();
    }
}
