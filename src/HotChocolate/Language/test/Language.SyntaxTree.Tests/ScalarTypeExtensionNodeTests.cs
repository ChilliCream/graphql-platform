using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ScalarTypeExtensionNodeTests
{
    private readonly NameNode _name1 = new("name1");
    private readonly NameNode _name2 = new("name2");
    private readonly IReadOnlyList<DirectiveNode> _directives = Array.Empty<DirectiveNode>();

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new ScalarTypeExtensionNode(TestLocations.Location1, _name1, _directives);
        var b = new ScalarTypeExtensionNode(TestLocations.Location1, _name1, _directives);
        var c = new ScalarTypeExtensionNode(TestLocations.Location1, _name2, _directives);

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
        var a = new ScalarTypeExtensionNode(TestLocations.Location1, _name1, _directives);
        var b = new ScalarTypeExtensionNode(TestLocations.Location2, _name1, _directives);
        var c = new ScalarTypeExtensionNode(TestLocations.Location3, _name2, _directives);

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
        var a = new ScalarTypeExtensionNode(TestLocations.Location1, _name1, _directives);
        var b = new ScalarTypeExtensionNode(TestLocations.Location2, _name1, _directives);
        var c = new ScalarTypeExtensionNode(TestLocations.Location1, _name2, _directives);
        var d = new ScalarTypeExtensionNode(TestLocations.Location2, _name2, _directives);

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
