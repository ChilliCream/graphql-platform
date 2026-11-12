namespace HotChocolate.Language.SyntaxTree;

public class ObjectValueNodeTests
{
    [Fact]
    public void GetHashCode_FieldOrder_DoesNotMatter()
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
        Assert.Equal(aHash, cHash);
        Assert.Equal(aHash, dHash);
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
    public void Equals_FieldOrder_DoesNotMatter()
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
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.True(acResult);
        Assert.True(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_Should_IgnoreFieldOrder_When_ObjectHasManyFields()
    {
        // arrange
        var fields = Enumerable.Range(0, 12)
            .Select(i => new ObjectFieldNode($"f{i:00}", i))
            .ToArray();
        var a = new ObjectValueNode(fields);
        var b = new ObjectValueNode(Enumerable.Reverse(fields).ToArray());

        // act
        var equals = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.True(equals);
        Assert.Equal(
            SyntaxComparer.BySyntax.GetHashCode(a),
            SyntaxComparer.BySyntax.GetHashCode(b));
    }

    [Fact]
    public void Equals_Should_BeFalse_When_ManyFieldsDifferInOneValue()
    {
        // arrange
        var a = new ObjectValueNode(
            Enumerable.Range(0, 12).Select(i => new ObjectFieldNode($"f{i:00}", i)).ToArray());
        var b = new ObjectValueNode(
            Enumerable.Range(0, 12).Select(i => new ObjectFieldNode($"f{i:00}", i == 7 ? 99 : i)).ToArray());

        // act
        var equals = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.False(equals);
        Assert.NotEqual(
            SyntaxComparer.BySyntax.GetHashCode(a),
            SyntaxComparer.BySyntax.GetHashCode(b));
    }

    [Fact]
    public void Equals_Should_BeFalse_When_NestedValueDiffers()
    {
        // arrange
        var a = CreateNestedObject(depth: 4, leafValue: 1);
        var b = CreateNestedObject(depth: 4, leafValue: 2);

        // act
        var equals = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.False(equals);
        Assert.True(SyntaxComparer.BySyntax.Equals(a, CreateNestedObject(depth: 4, leafValue: 1)));
    }

    [Fact]
    public void Equals_Should_BeFalse_When_ReorderedFieldsDifferInOneValue()
    {
        // arrange
        var a = new ObjectValueNode(
            Enumerable.Range(0, 12).Select(i => new ObjectFieldNode($"f{i:00}", i)).ToArray());
        var b = new ObjectValueNode(
            Enumerable.Range(0, 12).Reverse().Select(i => new ObjectFieldNode($"f{i:00}", i == 7 ? 99 : i)).ToArray());

        // act
        var equals = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.False(equals);
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
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
    }

    private static ObjectValueNode CreateNestedObject(int depth, int leafValue)
    {
        IValueNode value = new IntValueNode(leafValue);

        for (var level = 0; level < depth; level++)
        {
            var fields = new List<ObjectFieldNode> { new("child", value) };

            for (var i = 0; i < 8; i++)
            {
                fields.Add(new ObjectFieldNode($"f{i}", i));
            }

            value = new ObjectValueNode(fields);
        }

        return (ObjectValueNode)value;
    }
}
