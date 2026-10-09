using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

public sealed class TelemetryTextExtensionsTests
{
    [Theory]
    [InlineData("before\u001B[31mred\u001B[0mafter", "before\\u001B[31mred\\u001B[0mafter")]
    [InlineData("a\r\nb", "a\\nb")]
    [InlineData("a\rb", "a\\nb")]
    [InlineData("a\nb", "a\\nb")]
    [InlineData("a\tb", "a\\tb")]
    [InlineData("a\u0000b", "a\\u0000b")]
    [InlineData("a\u007Fb", "a\\u007Fb")]
    [InlineData("a\u009Bb", "a\\u009Bb")]
    public void EscapeControlCharacters_Should_EscapeControlCharacters_When_ValueContainsThem(
        string value,
        string expected)
    {
        // act
        var escaped = value.EscapeControlCharacters();

        // assert
        Assert.Equal(expected, escaped);
    }

    [Fact]
    public void EscapeControlCharacters_Should_ReturnSameInstance_When_ValueHasNoControlCharacters()
    {
        // arrange
        const string value = "plain text é";

        // act
        var escaped = value.EscapeControlCharacters();

        // assert
        Assert.Same(value, escaped);
    }
}
