using ChilliCream.Nitro.CommandLine.Tui;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui;

public sealed class DisplayWidthTests
{
    [Theory]
    [InlineData("⌚")]
    [InlineData("❤️")]
    [InlineData("1️⃣")]
    public void Measure_Should_ReturnTwoCells_When_TextElementUsesEmojiPresentation(string value)
    {
        // act
        var width = DisplayWidth.Measure(value);

        // assert
        Assert.Equal(2, width);
    }

    [Theory]
    [InlineData("a⌚", "a")]
    [InlineData("❤️a", "❤️")]
    [InlineData("1️⃣a", "1️⃣")]
    public void Slice_Should_KeepEmojiTextElementsIntact_When_WidthEndsAtTheirBoundary(
        string value,
        string expected)
    {
        // act
        var slice = DisplayWidth.Slice(value, 2);

        // assert
        Assert.Equal(expected, slice);
    }
}
