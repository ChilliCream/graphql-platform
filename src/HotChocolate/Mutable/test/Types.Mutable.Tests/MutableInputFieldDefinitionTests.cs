namespace HotChocolate.Types.Mutable;

public class MutableInputFieldDefinitionTests
{
    [Fact]
    public void SetDeprecationReason_Should_MarkAsDeprecated()
    {
        // arrange
        var field = new MutableInputFieldDefinition("foo");

        // act
        field.DeprecationReason = "Use bar.";

        // assert
        Assert.True(field.IsDeprecated);
        Assert.Equal("Use bar.", field.DeprecationReason);
    }

    [Fact]
    public void IsDeprecated_Should_BeFalse_When_DeprecationReasonIsCleared()
    {
        // arrange
        var field = new MutableInputFieldDefinition("foo")
        {
            DeprecationReason = "Use bar."
        };

        // act
        field.DeprecationReason = null;

        // assert
        Assert.False(field.IsDeprecated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDeprecationReason_Should_ClearDeprecation_When_ValueIsEmptyOrWhiteSpace(string reason)
    {
        // arrange
        var field = new MutableInputFieldDefinition("foo");

        // act
        field.DeprecationReason = reason;

        // assert
        Assert.False(field.IsDeprecated);
        Assert.Null(field.DeprecationReason);
    }
}
