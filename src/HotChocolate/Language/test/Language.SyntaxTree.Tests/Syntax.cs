using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public static class SyntaxEqualityComparerTests
{
    [Fact]
    public static void SelectionSetNode_GetHashCode_Equals()
    {
        // arrange
        var a = new SelectionSetNode(
            new List<ISelectionNode>
            {
                new FieldNode("a"),
                new FieldNode("b"),
            });

        var b = new SelectionSetNode(
            new List<ISelectionNode>
            {
                new FieldNode("a"),
                new FieldNode("b"),
            });

        // fact
        var hashA = SyntaxComparer.BySyntax.GetHashCode(a);
        var hashB = SyntaxComparer.BySyntax.GetHashCode(b);

        // assert
        Assert.Equal(hashA, hashB);
    }
}
