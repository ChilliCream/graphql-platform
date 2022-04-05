using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class OperationTypeDefinitionNodeTests
{
    [Fact]
    public void EqualsOperationTypeDefinitionNode_SameLocation()
    {
        // arrange
        var a = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("b"));
        var d = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Mutation,
            new NamedTypeNode("a"));

        // act
        var aaResult = a.Equals(a);
        var abResult = a.Equals(b);
        var acResult = a.Equals(c);
        var adResult = a.Equals(d);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsOperationTypeDefinitionNode_DifferentLocations()
    {
        // arrange
        var a = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(TestLocations.Location2, OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(TestLocations.Location3, OperationType.Query,
            new NamedTypeNode("b"));
        var d = new OperationTypeDefinitionNode(TestLocations.Location3, OperationType.Mutation,
            new NamedTypeNode("a"));

        // act
        var aaResult = a.Equals(a);
        var abResult = a.Equals(b);
        var acResult = a.Equals(c);
        var adResult = a.Equals(d);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(aaResult);
        Assert.True(abResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void CompareGetHashCode_WithLocations()
    {
        // arrange
        var a = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("a"));
        var b = new OperationTypeDefinitionNode(TestLocations.Location2, OperationType.Query,
            new NamedTypeNode("a"));
        var c = new OperationTypeDefinitionNode(TestLocations.Location1, OperationType.Query,
            new NamedTypeNode("b"));
        var d = new OperationTypeDefinitionNode(TestLocations.Location2, OperationType.Mutation,
            new NamedTypeNode("a"));
        var e = new OperationTypeDefinitionNode(TestLocations.Location3, OperationType.Mutation,
            new NamedTypeNode("a"));

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();
        var eHash = e.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.NotEqual(aHash, dHash);
        Assert.NotEqual(bHash, dHash);
        Assert.Equal(dHash, eHash);
    }
}
