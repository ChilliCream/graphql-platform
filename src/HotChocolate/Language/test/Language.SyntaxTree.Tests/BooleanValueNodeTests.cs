using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class BooleanValueNodeTests
{
    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CreateBooleanValue(bool value)
    {
        // act
        var booleanValueNode = new BooleanValueNode(value);

        // assert
        Assert.Equal(value, booleanValueNode.Value);
        Assert.Equal(SyntaxKind.BooleanValue, booleanValueNode.Kind);
        Assert.Null(booleanValueNode.Location);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CreateBooleanValueWithLocation(bool value)
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        // act
        var booleanValueNode = new BooleanValueNode(location, value);

        // assert
        Assert.Equal(value, booleanValueNode.Value);
        Assert.Equal(SyntaxKind.BooleanValue, booleanValueNode.Kind);
        Assert.Equal(location, booleanValueNode.Location);
    }

    [Fact]
    public void EqualsBooleanValueNode_SameLocation()
    {
        // arrange
        var a = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var c = new BooleanValueNode(new Location(1, 1, 1, 1), true);

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
    public void EqualsBooleanValueNode_DifferentLocation()
    {
        // arrange
        var a = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new Location(2, 2, 2, 2), false);
        var c = new BooleanValueNode(new Location(3, 3, 3, 3), true);

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
    public void EqualsBooleanValueNode()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);

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
    public void EqualsIValueNode()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);
        var d = new StringValueNode("foo");

        // act
        var ab_result = a.Equals((IValueNode)b);
        var aa_result = a.Equals((IValueNode)a);
        var ac_result = a.Equals((IValueNode)c);
        var ad_result = a.Equals((IValueNode)d);
        var anull_result = a.Equals(default(IValueNode));

        // assert
        Assert.True(ab_result);
        Assert.True(aa_result);
        Assert.False(ac_result);
        Assert.False(ad_result);
        Assert.False(anull_result);
    }

    [Fact]
    public void EqualsObject()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);
        var d = "foo";
        var e = 1;

        // act
        var abResult = a.Equals((object)b);
        var aaResult = a.Equals((object)a);
        var acResult = a.Equals((object)c);
        var adResult = a.Equals((object)d);
        var aeResult = a.Equals((object)e);
        var aNullResult = a.Equals(default(object));

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aeResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void CompareGetHashCode()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);

        // act
        var ahash = a.GetHashCode();
        var bhash = b.GetHashCode();
        var chash = c.GetHashCode();

        // assert
        Assert.Equal(ahash, bhash);
        Assert.NotEqual(ahash, chash);
    }

    [Fact]
    public void CompareGetHashCode_With_Location()
    {
        // arrange
        var a = new BooleanValueNode(new(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new(2, 2, 2, 2), false);
        var c = new BooleanValueNode(new(1, 1, 1, 1), true);
        var d = new BooleanValueNode(new(2, 2, 2, 2), true);

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

    [Fact]
    public void StringRepresentation()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(true);

        // act
        var aString = a.ToString();
        var bString = b.ToString();

        // assert
        Assert.Equal("false", aString);
        Assert.Equal("true", bString);
    }

    [Fact]
    public void ClassIsSealed()
    {
        Assert.True(typeof(BooleanValueNode).IsSealed);
    }

    [Fact]
    public void BooleanValue_WithNewValue_NewValueIsSet()
    {
        // arrange
        var booleanValueNode = new BooleanValueNode(false);

        // act
        booleanValueNode = booleanValueNode.WithValue(true);

        // assert
        Assert.True(booleanValueNode.Value);
    }
}
