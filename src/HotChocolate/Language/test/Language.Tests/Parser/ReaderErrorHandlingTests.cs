using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

public class ReaderErrorHandlingTests
{
    [Fact]
    public void Read_DotAtEndOfSource_DoesNotThrow()
    {
        // arrange
        var source = "."u8.ToArray();

        // act
        var spanReader = new Utf8GraphQLReader(source);
        var sequenceReader = new Utf8GraphQLReader(new ReadOnlySequence<byte>(source));

        // assert
        Assert.True(spanReader.Read());
        Assert.Equal(TokenKind.Dot, spanReader.Kind);
        Assert.True(sequenceReader.Read());
        Assert.Equal(TokenKind.Dot, sequenceReader.Kind);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("-")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("\"abc\\")]
    [InlineData("\"\"\"abc\"")]
    [InlineData("\"\"\"abc\\\"\"")]
    public void Read_MalformedInput_ThrowsSyntaxException(string sourceText)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(sourceText);

        // assert
        AssertThrowsSyntaxException(source);
    }

    [Fact]
    public void Read_IncompleteUtf8BomByte_ThrowsSyntaxException()
    {
        AssertThrowsSyntaxException([239]);
    }

    [Fact]
    public void Read_IncompleteUtf8BomPrefix_ThrowsSyntaxException()
    {
        AssertThrowsSyntaxException([239, 187]);
    }

    [Fact]
    public void Read_IncompleteUtf16BomPrefix_ThrowsSyntaxException()
    {
        AssertThrowsSyntaxException([254]);
    }

    [Fact]
    public void Read_UnterminatedStringWithNewLine_ThrowsSyntaxException_ForAllReaderModes()
    {
        // arrange
        var source = "\"abc\n"u8.ToArray();
        var multiSegment = TestSequenceSegment.CreateMultiSegment(source, 2);

        // assert
        Assert.Throws<SyntaxException>(() =>
        {
            var reader = new Utf8GraphQLReader(source);
            reader.Read();
        });

        Assert.Throws<SyntaxException>(() =>
        {
            var reader = new Utf8GraphQLReader(new ReadOnlySequence<byte>(source));
            reader.Read();
        });

        Assert.Throws<SyntaxException>(() =>
        {
            var reader = new Utf8GraphQLReader(multiSegment);
            reader.Read();
        });
    }

    [Fact]
    public void Parse_UnterminatedStringWithNewLine_ThrowsUnterminatedString()
    {
        // arrange
        var source = "\"abc\n"u8.ToArray();

        // act
        var exception = Assert.Throws<SyntaxException>(() => Utf8GraphQLParser.Parse(source.AsSpan()));

        // assert
        Assert.Contains("Unterminated string", exception.Message);
    }

    private static void AssertThrowsSyntaxException(byte[] source)
    {
        Assert.Throws<SyntaxException>(() =>
        {
            var reader = new Utf8GraphQLReader(source);
            reader.Read();
        });

        Assert.Throws<SyntaxException>(() =>
        {
            var reader = new Utf8GraphQLReader(new ReadOnlySequence<byte>(source));
            reader.Read();
        });
    }
}
