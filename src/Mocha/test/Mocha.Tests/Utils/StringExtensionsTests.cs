namespace Mocha.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void StringExtensions_EqualsOrdinal_Should_ReturnTrue_When_StringsAreEqual()
    {
        // arrange
        const string str1 = "test";
        const string str2 = "test";

        // act
        var result = str1.EqualsOrdinal(str2);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void StringExtensions_EqualsOrdinal_Should_ReturnFalse_When_StringsAreDifferent()
    {
        // arrange
        const string str1 = "test";
        const string str2 = "Test";

        // act
        var result = str1.EqualsOrdinal(str2);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void StringExtensions_EqualsOrdinal_Should_BeCaseSensitive_When_ComparingStrings()
    {
        // arrange
        const string str1 = "Test";
        const string str2 = "test";

        // act
        var result = str1.EqualsOrdinal(str2);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void StringExtensions_EqualsOrdinal_Should_HandleNullValues_When_StringIsNull()
    {
        // arrange
        const string? str1 = null;
        const string? str2 = null;

        // act
        var result = str1.EqualsOrdinal(str2);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void StringExtensions_EqualsOrdinal_Should_ReturnFalse_When_OneStringIsNull()
    {
        // arrange
        const string str1 = "test";
        const string? str2 = null;

        // act
        var result = str1.EqualsOrdinal(str2);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void StringExtensions_EqualsInvariantIgnoreCase_Should_ReturnTrue_When_StringsAreEqualIgnoringCase()
    {
        // arrange
        const string str1 = "test";
        const string str2 = "TEST";

        // act
        var result = str1.EqualsInvariantIgnoreCase(str2);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void StringExtensions_EqualsInvariantIgnoreCase_Should_ReturnTrue_When_StringsAreIdentical()
    {
        // arrange
        const string str1 = "Test";
        const string str2 = "Test";

        // act
        var result = str1.EqualsInvariantIgnoreCase(str2);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void StringExtensions_EqualsInvariantIgnoreCase_Should_ReturnFalse_When_StringsAreDifferent()
    {
        // arrange
        const string str1 = "test";
        const string str2 = "other";

        // act
        var result = str1.EqualsInvariantIgnoreCase(str2);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void StringExtensions_EqualsInvariantIgnoreCase_Should_HandleNullValues_When_BothAreNull()
    {
        // arrange
        const string? str1 = null;
        const string? str2 = null;

        // act
        var result = str1.EqualsInvariantIgnoreCase(str2);

        // assert
        Assert.True(result);
    }
}
