namespace HotChocolate.Fusion.Metadata;

public class MemberBindingTests
{
    [Theory]
    [InlineData("Subgraph1", "Member1")]
    [InlineData("SubgraphA", "MemberB")]
    public void Constructor_ValidInput_CreatesInstance(string subgraphName, string name)
    {
        // act
        var memberBinding = new MemberBinding(subgraphName, name);

        // assert
        Assert.NotNull(memberBinding);
        Assert.Equal(subgraphName, memberBinding.SubgraphName);
        Assert.Equal(name, memberBinding.Name);
    }

    [Theory]
    [InlineData(null, "Member1")]
    [InlineData("", "Member1")]
    [InlineData("  ", "Member1")]
    public void Constructor_InvalidSubgraphName_ThrowsArgumentException(
        string? subgraphName,
        string name)
    {
        // act and assert
        Assert.Throws<ArgumentException>(() => new MemberBinding(subgraphName!, name));
    }

    [Theory]
    [InlineData("Subgraph1", null)]
    [InlineData("Subgraph1", "")]
    [InlineData("Subgraph1", "  ")]
    public void Constructor_InvalidName_ThrowsArgumentException(
        string subgraphName,
        string? name)
    {
        // act and assert
        Assert.Throws<ArgumentException>(() => new MemberBinding(subgraphName, name!));
    }
}
