namespace HotChocolate.Fusion;

public sealed class StringUtilitiesTests
{
    [Theory]
    [InlineData("f", "F")]
    [InlineData("F", "F")]
    [InlineData("Foo", "FOO")]
    [InlineData("FOOBAR", "FOOBAR")]
    [InlineData("FooBar", "FOO_BAR")]
    [InlineData("Foo__Bar", "FOO_BAR")]
    [InlineData("IPAddress", "IP_ADDRESS")]
    [InlineData("FooBarBaz", "FOO_BAR_BAZ")]
    [InlineData("StringGUID", "STRING_GUID")]
    [InlineData("FOO_BAR_BAZ", "FOO_BAR_BAZ")]
    [InlineData("FirstIPAddress", "FIRST_IP_ADDRESS")]
    [InlineData("Foo-Bar", "FOO_BAR")]
    [InlineData("Foo--Bar", "FOO_BAR")]
    [InlineData("foo.bar", "FOO_BAR")]
    public void ToConstantCase_Examples_MatchExpectedResult(string input, string expected)
    {
        // arrange & act
        var result = StringUtilities.ToConstantCase(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToConstantCase_Long_Input()
    {
        var input = new string('a', 300);

        var result = StringUtilities.ToConstantCase(input);

        Assert.Equal(new string('A', 300), result);
    }
}
