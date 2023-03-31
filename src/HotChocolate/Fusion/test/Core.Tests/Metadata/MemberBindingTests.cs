namespace HotChocolate.Fusion.Metadata;

public class MemberBindingTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        // arrange
        const string subgraphName = "testSubgraph";
        const string name = "testName";

        // act
        var memberBinding = new MemberBinding(subgraphName, name);

        // assert
        Assert.NotNull(memberBinding);
        Assert.Equal(subgraphName, memberBinding.SubgraphName);
        Assert.Equal(name, memberBinding.Name);
    }

    [Theory]
    [InlineData(null, "testName")]
    [InlineData("", "testName")]
    [InlineData(" ", "testName")]
    [InlineData("testSubgraph", null)]
    [InlineData("testSubgraph", "")]
    [InlineData("testSubgraph", " ")]
    public void Constructor_WithInvalidArguments_ThrowsArgumentException(
        string subgraphName,
        string name)
    {
        // arrange, act, assert
        Assert.Throws<ArgumentException>(() => new MemberBinding(subgraphName, name));
    }
}
