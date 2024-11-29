using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class VariableNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"));
        var b = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"));
        var c = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("bb"));

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
        var a = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"));
        var b = new VariableNode(
            new Location(2, 2, 2, 2),
            new NameNode("aa"));
        var c = new VariableNode(
            new Location(3, 3, 3, 3),
            new NameNode("bb"));

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
        var a = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("aa"));
        var b = new VariableNode(
            new Location(2, 2, 2, 2),
            new NameNode("aa"));
        var c = new VariableNode(
            new Location(1, 1, 1, 1),
            new NameNode("bb"));
        var d = new VariableNode(
            new Location(2, 2, 2, 2),
            new NameNode("bb"));

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

    [Fact]
    public void Create_Name_Foo_NameIsPassed()
    {
        // arrange
        var name = new NameNode("foo");

        // act
        var node = new VariableNode(name);

        // assert
        Assert.Equal(name, node.Name);
    }

    [Fact]
    public void Create_Name_NullFoo_NameIsPassed()
    {
        // arrange
        var name = new NameNode("foo");

        // act
        var node = new VariableNode(null, name);

        // assert
        Assert.Equal(name, node.Name);
    }

    [Fact]
    public void Create_Name_LocationFoo_LocationAndNameIsPassed()
    {
        // arrange
        var name = new NameNode("foo");
        var location = AstTestHelper.CreateDummyLocation();

        // act
        var node = new VariableNode(location, name);

        // assert
        Assert.Equal(location, node.Location);
        Assert.Equal(name, node.Name);
    }

    [Fact]
    public void Create_Name_Null_ArgumentNullException()
    {
        // arrange
        // act
        VariableNode Action() => new((NameNode)null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_Name_LocationNull_ArgumentNullException()
    {
        // arrange
        var location = AstTestHelper.CreateDummyLocation();

        // act
        VariableNode Action() => new(location, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void WithName_Bar_NewObjectHasNewName()
    {
        // arrange
        var foo = new NameNode("foo");
        var bar = new NameNode("bar");
        var node = new VariableNode(foo);

        // act
        node = node.WithName(bar);

        // assert
        Assert.Equal(bar, node.Name);
    }

    [Fact]
    public void WithLocation_Location_NewObjectHasNewLocation()
    {
        // arrange
        var foo = new NameNode("foo");
        var node = new VariableNode(foo);
        var location = AstTestHelper.CreateDummyLocation();

        // act
        node = node.WithLocation(location);

        // assert
        Assert.Equal(location, node.Location);
    }
}
