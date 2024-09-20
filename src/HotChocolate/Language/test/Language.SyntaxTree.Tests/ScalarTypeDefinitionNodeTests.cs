using Xunit;
using static HotChocolate.Language.SyntaxTree.TestLocations;

namespace HotChocolate.Language.SyntaxTree;

public class ScalarTypeDefinitionNodeTests
{
    private readonly NameNode _name1 = new("name1");
    private readonly StringValueNode _description1 = new("value1");
    private readonly StringValueNode _description2 = new("value2");
    private readonly IReadOnlyList<DirectiveNode> _directives = Array.Empty<DirectiveNode>();

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new ScalarTypeDefinitionNode(Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(Location1, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(Location1, _name1, _description2, _directives);

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
        var a = new ScalarTypeDefinitionNode(Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(Location2, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(Location3, _name1, _description2, _directives);

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
        var a = new ScalarTypeDefinitionNode(Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(Location2, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(Location1, _name1, _description2, _directives);
        var d = new ScalarTypeDefinitionNode(Location2, _name1, _description2, _directives);

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
