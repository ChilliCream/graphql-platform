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

    private static void Write(ArenaBufferWriter writer, byte[] bytes)
    {
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
    }

    private static JsonSegment Write(ArenaBufferWriter writer, string json)
    {
        var start = writer.Position;
        Write(writer, Encoding.UTF8.GetBytes(json));
        return JsonSegment.Create(writer, start, writer.Position - start);
    }
}
