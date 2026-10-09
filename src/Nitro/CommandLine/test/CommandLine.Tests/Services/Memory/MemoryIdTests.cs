using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class MemoryIdTests
{
    [Fact]
    public void New_Should_ReturnWellFormedId()
    {
        // act
        var id = MemoryId.New(DateTimeOffset.UtcNow);

        // assert
        Assert.Equal(MemoryId.Length, id.Length);
        Assert.True(MemoryId.IsValid(id));
        Assert.Equal(id, id.ToLowerInvariant());
    }

    [Fact]
    public void New_Should_ReturnDistinctIds_When_CalledRepeatedlyAtTheSameTimestamp()
    {
        // arrange
        var timestamp = DateTimeOffset.UtcNow;

        // act
        var ids = Enumerable.Range(0, 1000).Select(_ => MemoryId.New(timestamp)).ToList();

        // assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void New_Should_SortLexicographicallyByCreationTime()
    {
        // arrange
        var earlier = MemoryId.New(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var later = MemoryId.New(new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero));

        // act
        var comparison = string.CompareOrdinal(earlier, later);

        // assert
        Assert.True(comparison < 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("012345678901234567890123456")] // one character too long
    public void IsValid_Should_ReturnFalse_When_LengthIsWrong(string value)
    {
        // act & assert
        Assert.False(MemoryId.IsValid(value));
    }

    [Fact]
    public void IsValid_Should_ReturnFalse_When_ContainsExcludedCharacters()
    {
        // arrange: Crockford base32 excludes i, l, o, and u.
        var value = new string('i', MemoryId.Length);

        // act & assert
        Assert.False(MemoryId.IsValid(value));
    }
}
