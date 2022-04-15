using System.Collections.Generic;
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
        var aaResult = a.Equals(a);
        var abResult = a.Equals(b);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var aaResult = a.Equals(a);
        var abResult = a.Equals(b);
        var acResult = a.Equals(c);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
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
