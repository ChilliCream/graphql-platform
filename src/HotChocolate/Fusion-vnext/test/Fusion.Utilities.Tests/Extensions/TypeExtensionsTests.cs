using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

public sealed class TypeExtensionsTests
{
    [Theory]
    // Leaf basics
    [InlineData("ID", "ID", true)]
    [InlineData("ID!", "ID", true)] // stricter is fine
    [InlineData("ID!", "ID!", true)]
    [InlineData("ID", "ID!", false)] // looser where other is NonNull
    // Named leaf mismatch
    [InlineData("String", "ID", false)]
    [InlineData("[String!]!", "[ID!]!", false)]
    // List with nullable everything on 'other'
    [InlineData("[ID]", "[ID]", true)]
    [InlineData("[ID]!", "[ID]", true)] // outer stricter
    [InlineData("[ID!]", "[ID]", true)] // inner stricter
    [InlineData("[ID!]!", "[ID]", true)] // both stricter
    // NonNull list of NonNull
    [InlineData("[ID!]!", "[ID!]!", true)] // exact
    [InlineData("[ID!]", "[ID!]!", false)] // outer looser
    [InlineData("[ID]!", "[ID!]!", false)] // inner looser
    [InlineData("[ID]", "[ID!]!", false)] // both looser
    // Mixed nesting
    [InlineData("[[ID]!]", "[[ID]!]", true)]
    [InlineData("[[ID]!]!", "[[ID]!]!", true)]
    [InlineData("[[ID]!]!", "[[ID]!]", true)] // stricter outer
    [InlineData("[[ID]!]", "[[ID]!]!", false)] // outer looser
    [InlineData("[[ID]!]!", "[[ID!]]!", false)] // inner position looser
    public void CompatibilityCases(string typeReference, string otherTypeReference, bool expected)
    {
        // arrange
        var type = TypeReferenceToType(typeReference);
        var otherType = TypeReferenceToType(otherTypeReference);

        // act
        var actual = type.IsCompatibleWith(otherType);

        // assert
        Assert.Equal(expected, actual);
    }

    private static IType TypeReferenceToType(string typeReference)
    {
        var typeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(typeReference);
        var typeDefinition = typeNode.NamedType().Name.Value switch
        {
            "ID" => new MutableScalarTypeDefinition("ID"),
            "String" => new MutableScalarTypeDefinition("String"),
            _ => throw new NotSupportedException()
        };

        return typeNode.RewriteToType(typeDefinition);
    }
}
