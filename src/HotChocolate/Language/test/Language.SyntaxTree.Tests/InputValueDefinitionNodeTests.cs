using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language.SyntaxTree.Tests;

public class InputValueDefinitionNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var b = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var c = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("cc"),
            new NamedTypeNode("cc"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0));

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var a = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var b = new InputValueDefinitionNode(
            new Location(2, 2, 2, 2),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var c = new InputValueDefinitionNode(
            new Location(3, 3, 3, 3),
            new NameNode("aa"),
            new StringValueNode("cc"),
            new NamedTypeNode("cc"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0));

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var a = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var b = new InputValueDefinitionNode(
            new Location(2, 2, 2, 2),
            new NameNode("aa"),
            new StringValueNode("bb"),
            new NamedTypeNode("bb"),
            new StringValueNode("bb"),
            new List<DirectiveNode>(0));
        var c = new InputValueDefinitionNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"),
            new StringValueNode("cc"),
            new NamedTypeNode("cc"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0));
        var d = new InputValueDefinitionNode(
            new Location(2, 2, 2, 2),
            new NameNode("aa"),
            new StringValueNode("cc"),
            new NamedTypeNode("cc"),
            new StringValueNode("cc"),
            new List<DirectiveNode>(0));

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
