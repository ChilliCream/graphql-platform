namespace HotChocolate.Fusion.Planning;

public sealed class OperationShortHashTests
{
    [Fact]
    public void ToNameSafe_Should_ReturnTheSameInstance_When_TheShortHashHoldsNameCharactersOnly()
    {
        // arrange
        const string shortHash = "f26ee48d";

        // act
        var nameSafe = OperationShortHash.ToNameSafe(shortHash);

        // assert
        Assert.Same(shortHash, nameSafe);
    }

    [Fact]
    public void ToNameSafe_Should_KeepTheLeadingDigit_When_TheShortHashStartsWithOne()
    {
        // arrange
        const string shortHash = "94a407d5";

        // act
        var nameSafe = OperationShortHash.ToNameSafe(shortHash);

        // assert
        Assert.Equal("94a407d5", nameSafe);
    }

    [Theory]
    [InlineData("BnjEJe8-", "BnjEJe8_")]
    [InlineData("-BnjEJe8", "_BnjEJe8")]
    [InlineData("--------", "________")]
    [InlineData("Bnj.Je8+", "Bnj_Je8_")]
    [InlineData("BnjéJe8/", "Bnj_Je8_")]
    public void ToNameSafe_Should_ReplaceTheCharacter_When_AGraphQLNameCannotHoldIt(
        string shortHash,
        string expected)
    {
        // act
        var nameSafe = OperationShortHash.ToNameSafe(shortHash);

        // assert
        Assert.Equal(expected, nameSafe);
    }
}
