namespace HotChocolate.Language.SyntaxTree;

public class FragmentSpreadNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("bb"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(3, 3, 3, 3),
            new("aa"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>
            {
                new("bb")
            });

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("bb"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));
        var d = new FragmentSpreadNode(
            new Location(2, 2, 2, 2),
            new("bb"),
            new List<ArgumentNode>(0),
            new List<DirectiveNode>(0));

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
    public void Equals_Should_ReturnFalse_When_ArgumentsDiffer()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));
        var c = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(2))
            },
            new List<DirectiveNode>(0));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);

        // assert
        Assert.True(abResult);
        Assert.False(acResult);
    }

    [Fact]
    public void GetHashCode_Should_ReturnDifferentHash_When_ArgumentsDiffer()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));
        var b = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(2))
            },
            new List<DirectiveNode>(0));

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);

        // assert
        Assert.NotEqual(aHash, bHash);
    }

    [Fact]
    public void WithName_Should_PreserveArguments()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));

        // act
        var b = a.WithName(new NameNode("cc"));

        // assert
        var argument = Assert.Single(b.Arguments);
        Assert.Equal("bb: 1", argument.ToString());
    }

    [Fact]
    public void WithDirectives_Should_PreserveArguments()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));

        // act
        var b = a.WithDirectives(new List<DirectiveNode> { new("cc") });

        // assert
        var argument = Assert.Single(b.Arguments);
        Assert.Equal("bb: 1", argument.ToString());
    }

    [Fact]
    public void WithLocation_Should_PreserveArguments()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));

        // act
        var b = a.WithLocation(new Location(2, 2, 2, 2));

        // assert
        var argument = Assert.Single(b.Arguments);
        Assert.Equal("bb: 1", argument.ToString());
    }

    [Fact]
    public void WithArguments_Should_ReplaceArguments()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode>(0));

        // act
        var b = a.WithArguments(new List<ArgumentNode> { new("cc", new IntValueNode(2)) });

        // assert
        var argument = Assert.Single(b.Arguments);
        Assert.Equal("cc: 2", argument.ToString());
    }

    [Fact]
    public void GetNodes_Should_YieldArgumentsBetweenNameAndDirectives()
    {
        // arrange
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<ArgumentNode>
            {
                new("bb", new IntValueNode(1))
            },
            new List<DirectiveNode> { new("cc") });

        // act
        var kinds = a.GetNodes().Select(t => t.Kind).ToArray();

        // assert
        Assert.Equal(
            [SyntaxKind.Name, SyntaxKind.Argument, SyntaxKind.Directive],
            kinds);
    }

    [Fact]
    public void Constructor_Should_YieldEmptyArguments_When_ArgumentsAreNotProvided()
    {
        // act
#pragma warning disable CS0618 // The obsolete constructor overload is under test.
        var a = new FragmentSpreadNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<DirectiveNode> { new("bb") });
#pragma warning restore CS0618

        // assert
        Assert.Empty(a.Arguments);
        var directive = Assert.Single(a.Directives);
        Assert.Equal("bb", directive.Name.Value);
    }
}
