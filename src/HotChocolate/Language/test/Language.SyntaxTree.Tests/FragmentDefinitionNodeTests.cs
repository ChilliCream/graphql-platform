namespace HotChocolate.Language.SyntaxTree;

public class FragmentDefinitionNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(3, 3, 3, 3),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var d = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            description: null,
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
