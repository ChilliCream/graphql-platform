using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language.SyntaxTree.Tests;

public class FragmentSpreadNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("bb"),
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
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(3, 3, 3, 3),
            new("aa"),
            new List<DirectiveNode>
            {
                new("bb")
            });

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
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("bb"),
            new List<DirectiveNode>(0));
        var d = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("bb"),
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
