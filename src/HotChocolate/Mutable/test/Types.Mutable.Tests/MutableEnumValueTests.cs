namespace HotChocolate.Types.Mutable;

public class MutableEnumValueTests
{
    [Fact]
    public void SetDeprecationReason_Should_MarkAsDeprecated()
    {
        // arrange
        var enumValue = new MutableEnumValue("FOO");

        // act
        enumValue.DeprecationReason = "Use BAR.";

        // assert
        Assert.True(enumValue.IsDeprecated);
        Assert.Equal("Use BAR.", enumValue.DeprecationReason);
    }

    [Fact]
    public void IsDeprecated_Should_BeFalse_When_DeprecationReasonIsCleared()
    {
        // arrange
        var enumValue = new MutableEnumValue("FOO")
        {
            DeprecationReason = "Use BAR."
        };

        // act
        enumValue.DeprecationReason = null;

        // assert
        Assert.False(enumValue.IsDeprecated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDeprecationReason_Should_ClearDeprecation_When_ValueIsEmptyOrWhiteSpace(string reason)
    {
        // arrange
        var enumValue = new MutableEnumValue("FOO");

        // act
        enumValue.DeprecationReason = reason;

        // assert
        Assert.False(enumValue.IsDeprecated);
        Assert.Null(enumValue.DeprecationReason);
    }
}
