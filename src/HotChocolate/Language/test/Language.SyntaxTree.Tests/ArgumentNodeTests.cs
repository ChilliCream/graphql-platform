using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public sealed class ArgumentNodeTests
{
    [Fact]
    public void CreateArgumentWithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(location, name, value);

        // assert
        Assert.Equal(SyntaxKind.Argument, argument.Kind);
        Assert.Equal(location, argument.Location);
        Assert.Equal(name, argument.Name);
        Assert.Equal(value, argument.Value);
    }

    [Fact]
    public void CreateArgumentWithoutLocation()
    {
        // arrange
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(name, value);

        // assert
        Assert.Equal(SyntaxKind.Argument, argument.Kind);
        Assert.Null(argument.Location);
        Assert.Equal(name, argument.Name);
        Assert.Equal(value, argument.Value);
    }

    [Fact]
    public void CreateArgumentWithConvenienceConstructor()
    {
        // arrange
        var name = "foo";
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(name, value);

        // assert
        Assert.Equal(SyntaxKind.Argument, argument.Kind);
        Assert.Null(argument.Location);
        Assert.Equal(name, argument.Name.Value);
        Assert.Equal(value, argument.Value);
    }

    [Fact]
    public void CreateArgumentWithoutName()
    {
        // arrange
        var value = new StringValueNode("bar");

        // act
        void Action() => new ArgumentNode(null, null!, value);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void CreateArgumentWithoutValue()
    {
        // arrange
        var name = new NameNode("foo");

        // act
        Action action = () => new ArgumentNode(null, name, null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void ArgumentNode_WithNewName_NewNameIsSet()
    {
        // arrange
        var argument = new ArgumentNode(
            "foo", new StringValueNode("bar"));

        // act
        argument = argument.WithName(new NameNode("bar"));

        // assert
        Assert.Equal("bar", argument.Name.Value);
    }

    [Fact]
    public void ArgumentNode_WithNewValue_NewValueIsSet()
    {
        // arrange
        var argument = new ArgumentNode(
            "foo", new StringValueNode("bar"));

        // act
        argument = argument.WithValue(new StringValueNode("foo"));

        // assert
        Assert.Equal("foo", ((StringValueNode)argument.Value).Value);
    }

    [Fact]
    public void ArgumentNode_WithNewLocation_NewLocationIsSet()
    {
        // arrange
        var argument = new ArgumentNode(
            "foo",
            new StringValueNode("bar"));
        Assert.Null(argument.Location);

        var location = new Location(0, 0, 0, 0);

        // act
        argument = argument.WithLocation(location);

        // assert
        Assert.Equal(location, argument.Location);
    }

    [Fact]
    public void Argument_ToString()
    {
        // arrange
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(null, name, value);

        // assert
        argument.ToString().MatchSnapshot();
    }

    [Fact]
    public void Argument_ToString_Indented()
    {
        // arrange
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(null, name, value);

        // assert
        argument.ToString(true).MatchSnapshot();
    }

    [Fact]
    public void Argument_ToString_UnIndented()
    {
        // arrange
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");

        // act
        var argument = new ArgumentNode(null, name, value);

        // assert
        argument.ToString(false).MatchSnapshot();
    }

    [Fact]
    public void Argument_GetNodes()
    {
        // arrange
        var name = new NameNode("foo");
        var value = new StringValueNode("bar");
        var argument = new ArgumentNode(null, name, value);

        // act
        ISyntaxNode[] nodes = argument.GetNodes().ToArray();

        // assert
        Assert.Collection(nodes,
            n => Assert.Equal(name, n),
            v => Assert.Equal(value, v));
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(567));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

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
        var a = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(3, 3, 3, 3), new("aa"), new IntValueNode(567));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

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
        var a = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(567));
        var d = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(567));

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
