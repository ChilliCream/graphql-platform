using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ObjectValueNodeTests
{
    [Fact]
    public void GetHashCode_FieldOrder_DoesMatter()
    {
        // arrange
        var objectA = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectB = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var objectC = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        var objectD = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        // act
        var hashA = objectA.GetHashCode();
        var hashB = objectB.GetHashCode();
        var hashC = objectC.GetHashCode();
        var hashD = objectD.GetHashCode();

        // assert
        Assert.NotEqual(hashA, hashB);
        Assert.NotEqual(hashB, hashC);
        Assert.Equal(hashA, hashD);
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
        var objectA = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectB = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var objectC = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        var objectD = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        // act
        var resultA = objectA.Equals(objectB);
        var resultB = objectB.Equals(objectC);
        var resultC = objectA.Equals(objectD);

        // assert
        Assert.False(resultA);
        Assert.False(resultB);
        Assert.True(resultC);
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
            new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2) });

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
    public void EqualsObjectFieldNode_DifferentLocations()
    {
        // arrange
        var a = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2) });

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
    public void CompareGetHashCode_WithLocations()
    {
        // arrange
        var a = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(
            TestLocations.Location1,
            new[] { new ObjectFieldNode("a", 2) });
        var d = new ObjectValueNode(
            TestLocations.Location2,
            new[] { new ObjectFieldNode("a", 2) });

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
    }
}
