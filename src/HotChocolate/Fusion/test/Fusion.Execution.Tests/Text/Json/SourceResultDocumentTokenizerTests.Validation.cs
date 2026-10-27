using System.Text;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerTests
{
    [Theory]
    [InlineData("", "{}", " \t\r\n")]
    // A closing brace completes the root object; trailing input is ignored.
    [InlineData("{", "\"a\":1}", " \t\r\n}")]
    [InlineData("{\"a\"", ":1}", " \t\r\n")]
    [InlineData("[0", ",1]", " \t\r\n")]
    [InlineData("[true", "]", " \t\r\n")]
    [InlineData("[\"\\", "\"]", "\"\\/bfnrt")]
    [InlineData("[\"\\u0", "00\"]", "0123456789abcdefABCDEF")]
    public void Parse_Should_AcceptOnlyAllowedBytes_When_ValidatingToken(
        string prefix,
        string suffix,
        string allowed)
    {
        // arrange
        var expected = Encoding.UTF8.GetBytes(allowed).Order();

        // act
        var accepted = GetAcceptedBytes(prefix, suffix);

        // assert
        Assert.Equal(expected, accepted);
    }

    [Theory]
    [InlineData("[\"", "\"]")]
    [InlineData("{\"", "\":1}")]
    public void Parse_Should_AcceptOnlyStringContentBytes_When_StringByteIsUnescaped(string prefix, string suffix)
    {
        // arrange
        var expected = Enumerable.Range(0x20, 0x100 - 0x20)
            .Select(t => (byte)t)
            .Where(t => t is not ((byte)'"' or (byte)'\\'));

        // act
        var accepted = GetAcceptedBytes(prefix, suffix);

        // assert
        Assert.Equal(expected, accepted);
    }

    private static List<byte> GetAcceptedBytes(string prefix, string suffix)
    {
        var before = Encoding.UTF8.GetBytes(prefix);
        var after = Encoding.UTF8.GetBytes(suffix);
        var accepted = new List<byte>();

        for (var value = 0; value <= byte.MaxValue; value++)
        {
            byte[] input = [.. before, (byte)value, .. after];

            if (AssertBoundaryIndependence(input) != Rejected)
            {
                accepted.Add((byte)value);
            }
        }

        return accepted;
    }
}
