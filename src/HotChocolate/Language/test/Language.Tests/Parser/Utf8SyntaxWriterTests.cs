using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

public class Utf8SyntaxWriterTests
{
    [Fact]
    public void Write_Should_EmitUnicodeEscapes_When_ValueCarriesOtherControlCharacters()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8SyntaxWriter(buffer, escape: true);

        // act
        writer.Write([0x01, (byte)'a', 0x1F]);

        // assert
        Text(buffer).MatchInlineSnapshot("""\u0001a\u001F""");
    }

    [Fact]
    public void Write_Should_EmitTwoByteEscapes_When_ValueCarriesNamedControlCharacters()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8SyntaxWriter(buffer, escape: true);

        // act
        writer.Write("\b\f\n\r\t\"\\"u8);

        // assert
        Text(buffer).MatchInlineSnapshot("""\b\f\n\r\t\"\\""");
    }

    [Fact]
    public void Write_Should_EmitValueUnchanged_When_WriterDoesNotEscape()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8SyntaxWriter(buffer, escape: false);

        // act
        writer.Write("a\"b\\c"u8);

        // assert
        Text(buffer).MatchInlineSnapshot("""a"b\c""");
    }

    [Fact]
    public void Write_Should_EscapeEncodedText_When_ValueIsString()
    {
        // arrange
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8SyntaxWriter(buffer, escape: true);

        // act
        writer.Write("größe \" ü \\ ");

        // assert
        Text(buffer).MatchInlineSnapshot("""größe \" ü \\ """);
    }

    private static string Text(ArrayBufferWriter<byte> writer)
        => Encoding.UTF8.GetString(writer.WrittenSpan);
}
