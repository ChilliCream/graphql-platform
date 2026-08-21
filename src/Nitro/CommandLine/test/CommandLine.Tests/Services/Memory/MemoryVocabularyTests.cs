using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class MemoryVocabularyTests
{
    [Theory]
    [InlineData("Fact", "fact")]
    [InlineData(" decision ", "decision")]
    public void MemoryTypes_Normalize_Should_TrimAndLowercase(string value, string expected)
        => Assert.Equal(expected, MemoryTypes.Normalize(value));

    [Theory]
    [InlineData("fact", true)]
    [InlineData("reference", true)]
    [InlineData("custom-type", true)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("Uppercase", false)]
    public void MemoryTypes_IsValid_Should_EnforceCharacterPolicy(string value, bool expected)
        => Assert.Equal(expected, MemoryTypes.IsValid(value));

    [Fact]
    public void MemoryTypes_IsValid_Should_ReturnFalse_When_ExceedsMaxLength()
        => Assert.False(MemoryTypes.IsValid(new string('a', 41)));

    [Theory]
    [InlineData("foo-bar", true)]
    [InlineData("", false)]
    [InlineData("foo bar", false)]
    public void MemoryTags_IsValid_Should_EnforceCharacterPolicy(string value, bool expected)
        => Assert.Equal(expected, MemoryTags.IsValid(value));

    [Theory]
    [InlineData("project", true)]
    [InlineData("global", true)]
    [InlineData("other", false)]
    public void MemoryScopes_IsValid_Should_OnlyAcceptKnownScopes(string value, bool expected)
        => Assert.Equal(expected, MemoryScopes.IsValid(value));
}
