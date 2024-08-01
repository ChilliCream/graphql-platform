using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class Utf8HelperTests
{
    [Fact]
    public void Unescape_NothingIsEscaped_InputIsOutput()
    {
        // arrange
        var inputData = Encoding.UTF8.GetBytes("hello_123");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, outputBuffer);
    }

    [InlineData("\\b", "\b")]
    [InlineData("\\f", "\f")]
    [InlineData("\\n", "\n")]
    [InlineData("\\r", "\r")]
    [InlineData("\\t", "\t")]
    [InlineData("\\\"", "\"")]
    [Theory]
    public void Unescape_StandardEscapeChars_OutputIsUnescaped(
        string escaped, string unescaped)
    {
        // arrange
        var inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("hello_123_" + unescaped,
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [InlineData("\\b", "\b")]
    [InlineData("\\f", "\f")]
    [InlineData("\\n", "\n")]
    [InlineData("\\r", "\r")]
    [InlineData("\\t", "\t")]
    [InlineData("\\\"\"\"", "\"\"\"")]
    [Theory]
    public void Unescape_BlockStringEscapeChars_OutputIsUnescaped(
        string escaped, string unescaped)
    {
        // arrange
        var inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, true);

        // assert
        Assert.Equal("hello_123_" + unescaped,
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [InlineData("\\\"\"")]
    [InlineData("\\\"")]
    [Theory]
    public void Unescape_BlockStringInvalidEscapeChars_Exception(
       string escaped)
    {
        // arrange
        var inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        try
        {
            Utf8Helper.Unescape(in input, ref output, true);

            // assert
            Assert.Fail("The unescape method should fail.");
        }
        catch
        {
        }
    }

    [InlineData("\\u0024", "$")]
    [InlineData("\\u00A2", "¢")]
    [InlineData("\\u0939", "ह")]
    [InlineData("\\u20AC", "€")]
    [Theory]
    public void Unescape_UnicodeEscapeChars_OutputIsUnescaped(
       string escaped, string unescaped)
    {
        // arrange
        var inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("hello_123_" + unescaped,
            Encoding.UTF8.GetString(output.ToArray()));
    }
}
