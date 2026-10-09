namespace HotChocolate.Types.Mutable;

public class MutableObjectTypeDefinitionTests
{
    [Fact]
    public void SetDeprecationReason_Should_MarkAsDeprecated()
    {
        // arrange
        var objectType = new MutableObjectTypeDefinition("Foo");

        // act
        objectType.DeprecationReason = "Use Bar.";

        // assert
        Assert.True(objectType.IsDeprecated);
        Assert.Equal("Use Bar.", objectType.DeprecationReason);
    }

    [Fact]
    public void IsDeprecated_Should_BeFalse_When_DeprecationReasonIsCleared()
    {
        // arrange
        var objectType = new MutableObjectTypeDefinition("Foo")
        {
            DeprecationReason = "Use Bar."
        };

        // act
        objectType.DeprecationReason = null;

        // assert
        Assert.False(objectType.IsDeprecated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDeprecationReason_Should_ClearDeprecation_When_ValueIsEmptyOrWhiteSpace(string reason)
    {
        // arrange
        var objectType = new MutableObjectTypeDefinition("Foo");

        // act
        objectType.DeprecationReason = reason;

        // assert
        Assert.False(objectType.IsDeprecated);
        Assert.Null(objectType.DeprecationReason);
    }
}
