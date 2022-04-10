using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ScalarTypeDefinitionNodeTests
{
    private readonly NameNode _name1 = new("name1");
    private readonly NameNode _name2 = new("name2");
    private readonly StringValueNode _description1 = new("value1");
    private readonly StringValueNode _description2 = new("value2");
    private readonly IReadOnlyList<DirectiveNode> _directives = Array.Empty<DirectiveNode>();

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description2, _directives);
        var d = new ScalarTypeDefinitionNode(TestLocations.Location1, _name2, _description2, _directives);

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var cdResult = c.Equals(d);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(cdResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(TestLocations.Location2, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(TestLocations.Location3, _name1, _description2, _directives);

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
        var a = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description1, _directives);
        var b = new ScalarTypeDefinitionNode(TestLocations.Location2, _name1, _description1, _directives);
        var c = new ScalarTypeDefinitionNode(TestLocations.Location1, _name1, _description2, _directives);
        var d = new ScalarTypeDefinitionNode(TestLocations.Location2, _name1, _description2, _directives);
        var e = new ScalarTypeDefinitionNode(TestLocations.Location1, _name2, _description2, _directives);

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();
        var eHash = e.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
        Assert.NotEqual(dHash, eHash);
    }
}
