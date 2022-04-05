using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ObjectValueNodeTests
{
    [Fact]
    public void GetHashCode_FieldOrder_DoesNotMatter()
    {
        // arrange
        var objecta = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectb = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var objectc = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        // act
        int hasha = objecta.GetHashCode();
        int hashb = objectb.GetHashCode();
        int hashc = objectc.GetHashCode();

        // assert
        Assert.Equal(hasha, hashb);
        Assert.Equal(hashb, hashc);
    }

    [Fact]
    public void GetHashCode_Different_Objects()
    {
        // arrange
        var objecta = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectb = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "abc"));

        // act
        int hasha = objecta.GetHashCode();
        int hashb = objectb.GetHashCode();

        // assert
        Assert.NotEqual(hasha, hashb);
    }

    [Fact]
    public void Equals_FieldOrder_DoesNotMatter()
    {
        // arrange
        var objecta = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectb = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "foo"));

        var objectc = new ObjectValueNode(
            new ObjectFieldNode("c", "foo"),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123));

        // act
        bool resulta = objecta.Equals(objectb);
        bool resultb = objectb.Equals(objectc);

        // assert
        Assert.True(resulta);
        Assert.True(resultb);
    }

    [Fact]
    public void Equals_Different_Objects()
    {
        // arrange
        var objecta = new ObjectValueNode(
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("c", "foo"));

        var objectb = new ObjectValueNode(
            new ObjectFieldNode("b", true),
            new ObjectFieldNode("a", 123),
            new ObjectFieldNode("c", "abc"));

        // act
        bool result = objecta.Equals(objectb);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObjectValueNode_SameLocation()
    {
        // arrange
        var a = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 2) });

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
        var a = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(TestLocations.Location2, new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(TestLocations.Location3, new[] { new ObjectFieldNode("a", 2) });

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
        var a = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 1) });
        var b = new ObjectValueNode(TestLocations.Location2, new[] { new ObjectFieldNode("a", 1) });
        var c = new ObjectValueNode(TestLocations.Location1, new[] { new ObjectFieldNode("a", 2) });
        var d = new ObjectValueNode(TestLocations.Location2, new[] { new ObjectFieldNode("b", 1) });

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.NotEqual(aHash, dHash);
        Assert.NotEqual(bHash, dHash);
    }
}
