using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class DirectiveNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode("abc", "def")
        };

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
        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode("abc", "def")
        };

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
        var arguments = new List<ArgumentNode>
        {
            new ArgumentNode("abc", "def")
        };

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
