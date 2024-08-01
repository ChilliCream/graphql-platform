using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ObjectTypeDefinitionNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var b = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var c = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var b = new ObjectTypeDefinitionNode(
            TestLocations.Location2,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var c = new ObjectTypeDefinitionNode(
            TestLocations.Location3,
            new NameNode("aa"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var b = new ObjectTypeDefinitionNode(
            TestLocations.Location2,
            new NameNode("aa"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var c = new ObjectTypeDefinitionNode(
            TestLocations.Location1,
            new NameNode("aa"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));
        var d = new ObjectTypeDefinitionNode(
            TestLocations.Location2,
            new NameNode("aa"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0),
            new List<NamedTypeNode>(0),
            new List<FieldDefinitionNode>(0));

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
