using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion;

public class JsonSegmentTests
{
    [Fact]
    public void AsSequence_Should_ReturnWrittenBytes_When_ArenaValueSpansSegments()
    {
        // arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        Write(writer, new byte[SourceResultDocument.GetDataChunkSize(0) - 4]);
        var segment = Write(writer, """{"a":"bcdef"}""");

        // act
        var sequence = segment.AsSequence();

        // assert
        Encoding.UTF8.GetString(sequence).MatchInlineSnapshot("""{"a":"bcdef"}""");
    }

    [Fact]
    public void WriteTo_Should_WriteRawValue_When_ArenaValueSpansSegments()
    {
        // arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        Write(writer, new byte[SourceResultDocument.GetDataChunkSize(0) - 4]);
        var segment = Write(writer, """{"a":"bcdef"}""");
        using var output = new PooledArrayWriter();
        var jsonWriter = new JsonWriter(output, new JsonWriterOptions { Indented = false });

        // act
        jsonWriter.WriteStartArray();
        segment.WriteTo(jsonWriter);
        segment.WriteTo(jsonWriter);
        jsonWriter.WriteEndArray();

        // assert
        Encoding.UTF8.GetString(output.WrittenSpan).MatchInlineSnapshot(
            """[{"a":"bcdef"},{"a":"bcdef"}]""");
    }

    [Fact]
    public void Equals_Should_CompareBytes_When_OneArenaValueSpansSegments()
    {
        // arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        Write(writer, new byte[SourceResultDocument.GetDataChunkSize(0) - 4]);
        var spanning = Write(writer, """{"a":"bcdef"}""");
        var contiguous = Write(writer, """{"a":"bcdef"}""");
        var different = Write(writer, """{"a":"bcdeX"}""");

        // act
        var equalsContiguous = spanning.Equals(contiguous);
        var equalsDifferent = spanning.Equals(different);

        // assert
        Assert.True(equalsContiguous);
        Assert.Equal(contiguous.GetHashCode(), spanning.GetHashCode());
        Assert.False(equalsDifferent);
    }

    [Fact]
    public void Equals_Should_CompareBytes_When_SegmentsComeFromDifferentWriters()
    {
        // arrange
        using var arena = new MemoryArena();
        using var arenaWriter = new ArenaBufferWriter(arena);
        using var chunkedWriter = new ChunkedArrayWriter();
        var arenaSegment = Write(arenaWriter, """{"a":"bcdef"}""");
        var chunkedSegment = Write(chunkedWriter, """{"a":"bcdef"}""");

        // act
        var equals = arenaSegment.Equals(chunkedSegment);

        // assert
        Assert.True(equals);
        Assert.Equal(arenaSegment.GetHashCode(), chunkedSegment.GetHashCode());
    }

    [Fact]
    public void Detach_Should_KeepBytes_When_SourceMemoryIsWrittenAgain()
    {
        // arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var segment = Write(writer, """{"a":"bcdef"}""");

        // act
        var detached = segment.Detach(arena);
        writer.Reset();
        Write(writer, """{"a":"XXXXX"}""");

        // assert
        Encoding.UTF8.GetString(segment.AsSequence()).MatchInlineSnapshot("""{"a":"XXXXX"}""");
        Encoding.UTF8.GetString(detached.AsSequence()).MatchInlineSnapshot("""{"a":"bcdef"}""");
    }

    [Fact]
    public void Detach_Should_CopyEveryByte_When_SegmentExceedsLargestArenaRental()
    {
        // arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var bytes = new byte[JsonMemory.BufferSize + 100];
        Random.Shared.NextBytes(bytes);
        Write(writer, bytes);
        var segment = JsonSegment.Create(writer.GetSequence(0, bytes.Length));

        // act
        var detached = segment.Detach(arena);

        // assert
        Assert.False(detached.AsSequence().IsSingleSegment);
        Assert.Equal(bytes, detached.AsSequence().ToArray());
    }

    private static void Write(ArenaBufferWriter writer, byte[] bytes)
    {
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
    }

    private static JsonSegment Write(ArenaBufferWriter writer, string json)
    {
        var start = writer.Position;
        Write(writer, Encoding.UTF8.GetBytes(json));
        return JsonSegment.Create(writer.GetSequence(start, writer.Position - start));
    }

    private static JsonSegment Write(ChunkedArrayWriter writer, string json)
    {
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
        return JsonSegment.Create(writer.GetSequence(start, writer.Position - start));
    }
}
