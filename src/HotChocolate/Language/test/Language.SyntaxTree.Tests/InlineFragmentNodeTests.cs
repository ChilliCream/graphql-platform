using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class InlineFragmentNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var b = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var c = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("bb"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var b = new InlineFragmentNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var c = new InlineFragmentNode(
            new Location(3, 3, 3, 3),
            new("aa"),
            new List<DirectiveNode> { new("bb"), },
            new SelectionSetNode(new List<ISelectionNode>(0)));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var b = new InlineFragmentNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var c = new InlineFragmentNode(
            new Location(1, 1, 1, 1),
            new("bb"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));
        var d = new InlineFragmentNode(
            new Location(2, 2, 2, 2),
            new("bb"),
            new List<DirectiveNode>(0),
            new SelectionSetNode(new List<ISelectionNode>(0)));

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
