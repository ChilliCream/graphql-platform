using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Language.SyntaxTree;

public class FieldDefinitionNodeTests
{
    [Fact]
    public void Equals_FieldDefinitionNode_When_Both_Are_Equal()
    {
        // arrange
        FieldDefinitionNode a = ParseFieldDefinition("foo(a: String): String @bar");
        FieldDefinitionNode b = ParseFieldDefinition("foo(a: String): String @bar");

        // act
        var success = a.Equals(b);

        // assert
        Assert.True(success);
    }
}
