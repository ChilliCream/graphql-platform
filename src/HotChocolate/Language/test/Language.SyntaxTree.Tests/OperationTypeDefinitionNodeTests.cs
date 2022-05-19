using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class OperationTypeDefinitionNodeTests
{
    [Fact]
    public void EqualsOperationTypeDefinitionNode_SameLocation()
    {
        // arrange
        var a = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Query,
            new NamedTypeNode("b"));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsOperationTypeDefinitionNode_DifferentLocations()
    {
        // arrange
        var a = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(
            TestLocations.Location2,
            OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(
            TestLocations.Location3,
            OperationType.Query,
            new NamedTypeNode("b"));

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

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
        var a = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(
            TestLocations.Location2,
            OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(
            TestLocations.Location1,
            OperationType.Mutation,
            new NamedTypeNode("b"));
        var d = new OperationTypeDefinitionNode(
            TestLocations.Location3,
            OperationType.Mutation,
            new NamedTypeNode("b"));

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
