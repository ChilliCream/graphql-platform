using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public class ArenaBufferWriterTests
{
    [Fact]
    public void GetSpan_Should_ReturnBoundedSpan_When_CurrentChunkHasRemainingSpace()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);

        // Act
        var span = writer.GetSpan();

        // Assert
        Assert.Equal(SourceResultDocument.GetDataChunkSize(0), span.Length);
    }

    [Fact]
    public void GetSpan_Should_RoundTripScratchBytes_When_RequestSpansChunks()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var blob = CreateAsciiPattern(SourceResultDocument.GetDataChunkSize(0) + 512);
        var json = Encoding.UTF8.GetBytes($$"""{"blob":"{{blob}}"}""");

        // Act
        var span = writer.GetSpan(json.Length);
        json.CopyTo(span);
        writer.Advance(json.Length);
        using var document = SourceResultDocument.ParseFilled(
            arena,
            writer.Segments,
            writer.UsedChunks,
            writer.LastLength);

        // Assert
        Assert.Equal(json.Length, span.Length);
        Assert.Equal(blob, document.Root.GetProperty("blob").AssertString());
        Assert.Equal(Encoding.UTF8.GetString(json), document.Root.GetRawText());
    }

    private static string CreateAsciiPattern(int length)
    {
        var chars = new char[length];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + (i % 26));
        }

        return new string(chars);
    }
}
