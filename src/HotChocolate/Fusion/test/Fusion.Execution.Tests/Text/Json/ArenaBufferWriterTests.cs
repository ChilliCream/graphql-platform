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

    [Fact]
    public void GetHashCode_Should_MatchScalar_When_UsingSimdAndGeometricChunks()
    {
        // Arrange
        var lengths = new[]
        {
            0,
            1,
            63,
            64,
            65,
            512,
            SourceResultDocument.GetDataChunkSize(0) + 17
        };
        var mismatches = new List<string>();

        // Act
        foreach (var length in lengths)
        {
            var data = new byte[length];
            var rng = new Random(length);
            rng.NextBytes(data);

            using var arena = new MemoryArena();
            using var writer = CreateWriterWithData(arena, data);
            var expected = ScalarHash(data);
            var actual = writer.GetHashCode(0, data.Length);

            if (actual != expected)
            {
                mismatches.Add($"{length}: expected {expected}, actual {actual}");
            }
        }

        // Assert
        Assert.Empty(mismatches);
    }

    private static ArenaBufferWriter CreateWriterWithData(MemoryArena arena, byte[] data)
    {
        var writer = new ArenaBufferWriter(arena);
        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);
        return writer;
    }

    private static int ScalarHash(byte[] data)
    {
        var hash = 0u;

        foreach (var b in data)
        {
            hash = (hash * 31) + b;
        }

        return (int)(hash & 0x7FFFFFFF);
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
