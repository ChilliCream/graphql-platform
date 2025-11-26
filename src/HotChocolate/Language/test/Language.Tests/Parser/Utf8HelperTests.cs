using System.Text;

namespace HotChocolate.Language;

public class Utf8HelperTests
{
    [Fact]
    public void Unescape_NothingIsEscaped_InputIsOutput()
    {
        // arrange
        var inputData = "hello_123"u8.ToArray();
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

    [Fact]
    public void Unescape_EmptyString_ReturnsEmpty()
    {
        // arrange
        var inputData = Array.Empty<byte>();
        var outputBuffer = new byte[10];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void Unescape_LongStringNoEscapes_BulkCopy()
    {
        // arrange - 64 bytes, exercises SIMD fast path (no escapes)
        var inputData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-"u8.ToArray();
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_LongStringWithEscapeAtStart()
    {
        // arrange - escape at position 0, then 60+ bytes
        var inputData = Encoding.UTF8.GetBytes("\\nABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("\nABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_LongStringWithEscapeInMiddle()
    {
        // arrange - 32 bytes, then escape, then more bytes (exercises SIMD + escape + SIMD)
        var inputData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef\\nghijklmnopqrstuvwxyz0123456789_-");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef\nghijklmnopqrstuvwxyz0123456789_-",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_LongStringWithEscapeAtEnd()
    {
        // arrange - 60+ bytes then escape at end (exercises SIMD + scalar tail)
        var inputData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789\\n");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_MultipleEscapesInLongString()
    {
        // arrange - multiple escapes spread across SIMD boundaries
        var inputData = Encoding.UTF8.GetBytes("ABCDEFGHIJ\\nKLMNOPQRSTUVWXYZ\\tabcdefghij\\rklmnopqrstuvwxyz");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("ABCDEFGHIJ\nKLMNOPQRSTUVWXYZ\tabcdefghij\rklmnopqrstuvwxyz",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_SurrogatePair_Emoji()
    {
        // arrange - surrogate pair for 😀 (U+1F600) = \uD83D\uDE00
        var inputData = Encoding.UTF8.GetBytes("hello\\uD83D\\uDE00world");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("hello😀world", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_ConsecutiveEscapes()
    {
        // arrange - multiple escapes in a row
        var inputData = Encoding.UTF8.GetBytes("\\n\\r\\t\\b");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("\n\r\t\b", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_Exactly32Bytes_NoEscape()
    {
        // arrange - exactly Vector256 size
        var inputData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef"u8.ToArray();
        Assert.Equal(32, inputData.Length);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_Exactly16Bytes_NoEscape()
    {
        // arrange - exactly Vector128 size
        var inputData = "ABCDEFGHIJKLMNOP"u8.ToArray();
        Assert.Equal(16, inputData.Length);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_ForwardSlash()
    {
        // arrange - forward slash escape
        var inputData = Encoding.UTF8.GetBytes("path\\/to\\/file");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("path/to/file", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_BackslashEscape()
    {
        // arrange - escaped backslash
        var inputData = Encoding.UTF8.GetBytes("path\\\\to\\\\file");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("path\\to\\file", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_SmallStringWithEscape_ScalarPath()
    {
        // arrange - 10 bytes total, too small for SIMD, goes to scalar
        var inputData = Encoding.UTF8.GetBytes("abc\\ndef");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("abc\ndef", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_31Bytes_JustUnderVector256()
    {
        // arrange - 31 bytes (just under Vector256 threshold)
        var inputData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcde"u8.ToArray();
        Assert.Equal(31, inputData.Length);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_33Bytes_JustOverVector256()
    {
        // arrange - 33 bytes (just over Vector256, will do one SIMD + 1 byte scalar tail)
        var inputData = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefg"u8.ToArray();
        Assert.Equal(33, inputData.Length);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_15Bytes_JustUnderVector128()
    {
        // arrange - 15 bytes (just under Vector128 threshold)
        var inputData = "ABCDEFGHIJKLMNO"u8.ToArray();
        Assert.Equal(15, inputData.Length);
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal(inputData, output.ToArray());
    }

    [Fact]
    public void Unescape_EscapeAtPosition31()
    {
        // arrange - escape right at Vector256 boundary
        var inputData = Encoding.UTF8.GetBytes("0123456789012345678901234567890\\n");
        Assert.Equal(33, inputData.Length); // 31 chars + \n (2 bytes)
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("0123456789012345678901234567890\n",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_InvalidEscapeChar_ThrowsException()
    {
        // arrange - invalid escape sequence \q
        var inputData = Encoding.UTF8.GetBytes("hello\\qworld");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_TruncatedUnicodeEscape_ThrowsException()
    {
        // arrange - truncated unicode \u00
        var inputData = Encoding.UTF8.GetBytes("hello\\u00");
        var outputBuffer = new byte[inputData.Length];

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_UnexpectedHighSurrogate_ThrowsException()
    {
        // arrange - two high surrogates in a row
        var inputData = Encoding.UTF8.GetBytes("\\uD83D\\uD83D");
        var outputBuffer = new byte[inputData.Length];

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_UnexpectedLowSurrogate_ThrowsException()
    {
        // arrange - low surrogate without high surrogate
        var inputData = Encoding.UTF8.GetBytes("\\uDE00");
        var outputBuffer = new byte[inputData.Length];

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_HighSurrogateNotFollowedByLowSurrogate_ThrowsException()
    {
        // arrange - high surrogate followed by regular char
        var inputData = Encoding.UTF8.GetBytes("\\uD83D\\u0041");
        var outputBuffer = new byte[inputData.Length];

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_TrailingBackslash_ThrowsException()
    {
        // arrange - string ending with backslash
        var inputData = Encoding.UTF8.GetBytes("hello\\");
        var outputBuffer = new byte[inputData.Length];

        // act & assert
        Assert.Throws<Utf8EncodingException>(() =>
        {
            Span<byte> o = outputBuffer;
            Utf8Helper.Unescape(inputData, ref o, false);
        });
    }

    [Fact]
    public void Unescape_OnlyBackslashN()
    {
        // arrange - just an escape, nothing else
        var inputData = Encoding.UTF8.GetBytes("\\n");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("\n", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_64BytesWithEscapeAtByte32()
    {
        // arrange - escape exactly at byte 32 (SIMD boundary)
        var inputData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345\\n67890abcdefghijklmnopqrstuvwxyz");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345\n67890abcdefghijklmnopqrstuvwxyz",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void Unescape_UnicodeInLongString()
    {
        // arrange - unicode escape in a long string (exercises SIMD + unicode handling)
        var inputData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij\\u20ACklmnopqrstuvwxyz0123456789");
        var outputBuffer = new byte[inputData.Length];

        var input = new ReadOnlySpan<byte>(inputData);
        var output = new Span<byte>(outputBuffer);

        // act
        Utf8Helper.Unescape(in input, ref output, false);

        // assert
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij€klmnopqrstuvwxyz0123456789",
            Encoding.UTF8.GetString(output.ToArray()));
    }
}
