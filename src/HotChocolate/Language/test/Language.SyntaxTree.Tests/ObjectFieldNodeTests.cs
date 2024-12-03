using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ObjectFieldNodeTests
{
    [Fact]
    public void EqualsObjectFieldNode_SameLocation()
    {
        // arrange
        var a = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(1));
        var b = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(1));
        var c = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(2));
        var d = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("d"),
            new IntValueNode(1));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var adResult = SyntaxComparer.BySyntax.Equals(a, d);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsObjectFieldNode_DifferentLocations()
    {
        // arrange
        var a = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(1));
        var b = new ObjectFieldNode(
            TestLocations.Location2,
            new NameNode("a"),
            new IntValueNode(1));
        var c = new ObjectFieldNode(
            TestLocations.Location3,
            new NameNode("a"),
            new IntValueNode(2));
        var d = new ObjectFieldNode(
            TestLocations.Location3,
            new NameNode("d"),
            new IntValueNode(1));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var adResult = SyntaxComparer.BySyntax.Equals(a, d);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void CompareGetHashCode_WithLocation()
    {
        // arrange
        var a = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(1));
        var b = new ObjectFieldNode(
            TestLocations.Location2,
            new NameNode("a"),
            new IntValueNode(1));
        var c = new ObjectFieldNode(
            TestLocations.Location1,
            new NameNode("a"),
            new IntValueNode(2));
        var d = new ObjectFieldNode(
            TestLocations.Location2,
            new NameNode("d"),
            new IntValueNode(1));
        var e = new ObjectFieldNode(
            TestLocations.Location3,
            new NameNode("d"),
            new IntValueNode(1));

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);
        var eHash = SyntaxComparer.BySyntax.GetHashCode(e);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.NotEqual(aHash, dHash);
        Assert.NotEqual(cHash, dHash);
        Assert.Equal(dHash, eHash);
    }

    [Fact]
    public void Create_Float()
    {
        // arrange
        // act
        var obj = new ObjectFieldNode("abc", 1.2);

        // assert
        Assert.Equal("abc", obj.Name.Value);
        Assert.Equal("1.2", Assert.IsType<FloatValueNode>(obj.Value).Value);
    }

    [Fact]
    public void Create_Int()
    {
        // arrange
        // act
        var obj = new ObjectFieldNode("abc", 1);

        // assert
        Assert.Equal("abc", obj.Name.Value);
        Assert.Equal("1", Assert.IsType<IntValueNode>(obj.Value).Value);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void Create_Bool(bool value)
    {
        // arrange
        // act
        var obj = new ObjectFieldNode("abc", value);

        // assert
        Assert.Equal("abc", obj.Name.Value);
        Assert.Equal(value, Assert.IsType<BooleanValueNode>(obj.Value).Value);
    }

    [Fact]
    public void Create_String()
    {
        // arrange
        // act
        var obj = new ObjectFieldNode("abc", "def");

        // assert
        Assert.Equal("abc", obj.Name.Value);
        Assert.Equal("def", Assert.IsType<StringValueNode>(obj.Value).Value);
    }
}
