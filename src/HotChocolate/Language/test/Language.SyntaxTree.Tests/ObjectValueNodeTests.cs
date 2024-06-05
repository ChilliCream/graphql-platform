using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ObjectValueNodeTests
{
    [Fact]
    public void GetHashCode_FieldOrder_DoesMatter()
    {
        // arrange
        var a = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var b = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var c = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var d = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.NotEqual(aHash, dHash);
    }

    [Fact]
    public void GetHashCode_Different_Objects()
    {
        // arrange
        var objectA = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectB = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "abc"));

        // act
        var hashA = objectA.GetHashCode();
        var hashB = objectB.GetHashCode();

        // assert
        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void Equals_FieldOrder_DoesMatter()
    {
        // arrange
        var a = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var b = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var c = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var d = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var adResult = SyntaxComparer.BySyntax.Equals(a, d);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_Different_Objects()
    {
        // arrange
        var objectA = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectB = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "abc"));

        // act
        var result = objectA.Equals(objectB);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObjectValueNode_SameLocation()
    {
        // arrange
        var a = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1), });
        var b = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1), });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2), });

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
    public void EqualsObjectFieldNode_DifferentLocations()
    {
        // arrange
        var a = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1), });
        var b = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 1), });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2), });

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
    public void CompareGetHashCode_WithLocations()
    {
        // arrange
        var a = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1), });
        var b = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 1), });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2), });
        var d = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 2), });

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
    }
}
