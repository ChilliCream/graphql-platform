namespace HotChocolate.Language.SyntaxTree;

public class DirectiveExtensionNodeTests
{
    private readonly NameNode _name1 = new("name1");
    private readonly NameNode _name2 = new("name2");
    private readonly IReadOnlyList<DirectiveNode> _directives = [new DirectiveNode("tag")];

    [Fact]
    public void Create_DirectiveExtension()
    {
        // arrange
        var name = new NameNode("foo");

        // act
        var extension = new DirectiveExtensionNode(
            TestLocations.Location1, name, _directives);

        // assert
        Assert.Equal(SyntaxKind.DirectiveExtension, extension.Kind);
        Assert.Equal(TestLocations.Location1, extension.Location);
        Assert.Equal(name, extension.Name);
        Assert.Equal(_directives, extension.Directives);
    }

    [Fact]
    public void GetNodes_Returns_Name_And_Directives()
    {
        // arrange
        var extension = new DirectiveExtensionNode(null, _name1, _directives);

        // act
        var nodes = extension.GetNodes().ToArray();

        // assert
        Assert.Collection(
            nodes,
            n => Assert.Equal(_name1, n),
            n => Assert.Equal(_directives[0], n));
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new DirectiveExtensionNode(TestLocations.Location1, _name1, _directives);
        var b = new DirectiveExtensionNode(TestLocations.Location2, _name1, _directives);
        var c = new DirectiveExtensionNode(TestLocations.Location3, _name2, _directives);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
        var a = new DirectiveExtensionNode(TestLocations.Location1, _name1, _directives);
        var b = new DirectiveExtensionNode(TestLocations.Location2, _name1, _directives);
        var c = new DirectiveExtensionNode(TestLocations.Location1, _name2, _directives);
        var d = new DirectiveExtensionNode(TestLocations.Location2, _name2, _directives);

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
