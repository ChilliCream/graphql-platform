using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class DirectiveNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var arguments = new List<ArgumentNode> { new("abc", "def"), };

        var a = new DirectiveNode(
            TestLocations.Location1,
            new("aa"),
            arguments);
        var b = new DirectiveNode(
            TestLocations.Location1,
            new("aa"),
            arguments);
        var c = new DirectiveNode(
            TestLocations.Location1,
            new("ab"),
            arguments);

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
        var arguments = new List<ArgumentNode> { new("abc", "def"), };

        var a = new DirectiveNode(
            TestLocations.Location1,
            new("aa"),
            arguments);
        var b = new DirectiveNode(
            TestLocations.Location2,
            new("aa"),
            arguments);
        var c = new DirectiveNode(
            TestLocations.Location1,
            new("ab"),
            arguments);

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
        var arguments = new List<ArgumentNode> { new("abc", "def"), };

        var a = new DirectiveNode(
            TestLocations.Location1,
            new("aa"),
            arguments);
        var b = new DirectiveNode(
            TestLocations.Location2,
            new("aa"),
            arguments);
        var c = new DirectiveNode(
            TestLocations.Location1,
            new("ab"),
            arguments);
        var d = new DirectiveNode(
            TestLocations.Location2,
            new("ab"),
            arguments);

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
