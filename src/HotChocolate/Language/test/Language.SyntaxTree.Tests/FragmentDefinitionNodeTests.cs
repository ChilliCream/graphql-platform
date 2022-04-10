using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language.SyntaxTree.Tests;

public class FragmentDefinitionNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(3, 3, 3, 3),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var b = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("cc"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var c = new FragmentDefinitionNode(
            new Location(1, 1, 1, 1),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));
        var d = new FragmentDefinitionNode(
            new Location(2, 2, 2, 2),
            new("aa"),
            new List<VariableDefinitionNode>(),
            new NamedTypeNode("dd"),
            new List<DirectiveNode>(),
            new SelectionSetNode(new List<ISelectionNode>()));

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
}
