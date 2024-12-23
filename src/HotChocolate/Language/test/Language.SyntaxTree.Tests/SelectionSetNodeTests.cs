using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class SelectionSetNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>(0));
        var b = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>(0));
        var c = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("bb"),
                    new NameNode("bb"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });

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
        var a = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>(0));
        var b = new SelectionSetNode(
            new Location(2, 2, 2, 2),
            new List<ISelectionNode>(0));
        var c = new SelectionSetNode(
            new Location(3, 3, 3, 3),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("bb"),
                    new NameNode("bb"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });

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
        var a = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("aa"),
                    new NameNode("aa"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });
        var b = new SelectionSetNode(
            new Location(2, 2, 2, 2),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("aa"),
                    new NameNode("aa"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });
        var c = new SelectionSetNode(
            new Location(1, 1, 1, 1),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("bb"),
                    new NameNode("bb"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });
        var d = new SelectionSetNode(
            new Location(2, 2, 2, 2),
            new List<ISelectionNode>
            {
                new FieldNode(TestLocations.Location1,
                    new NameNode("bb"),
                    new NameNode("bb"),
                    new List<DirectiveNode>(0),
                    new List<ArgumentNode>(0),
                    new SelectionSetNode(
                        TestLocations.Location1,
                        new List<ISelectionNode>(0))),
            });

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
    public void CreateSelectionSet()
    {
        // arrange
        var location = AstTestHelper.CreateDummyLocation();
        var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
            };

        // act
        var selectionSet = new SelectionSetNode
        (
            location,
            selections
        );

        // assert
        selectionSet.MatchSnapshot();
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var location = AstTestHelper.CreateDummyLocation();
        var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
            };

        var selectionSet = new SelectionSetNode
        (
            null,
            selections
        );

        // act
        selectionSet = selectionSet.WithLocation(location);

        // assert
        selectionSet.MatchSnapshot();
    }

    [Fact]
    public void WithSelections()
    {
        // arrange
        var location = AstTestHelper.CreateDummyLocation();
        var selections = new List<ISelectionNode>
            {
                new FieldNode
                (
                    null,
                    new NameNode("bar"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null
                ),
            };

        var selectionSet = new SelectionSetNode
        (
            location,
            selections
        );

        // act
        selectionSet = selectionSet.WithSelections(
            new List<ISelectionNode>
            {
                    new FieldNode
                    (
                        null,
                        new NameNode("baz"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null
                    ),
            });

        // assert
        selectionSet.MatchSnapshot();
    }
}
