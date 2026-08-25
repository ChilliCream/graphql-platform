using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchVariableWriterTests
{
    [Fact]
    public void Write_Should_PrefixEveryName_When_NoVariableIsShared()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup("query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name}}");
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };

        // act
        var json = Write(items, []);

        // assert
        json.MatchInlineSnapshot("""{"_0___fusion_1_id":"1","_1___fusion_1_id":"2"}""");
    }

    [Fact]
    public void Write_Should_WriteSharedValueOnce_When_VariableIsShared()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(
            "query($search: String, $__fusion_1_id: ID!){productById(id: $__fusion_1_id){name(q: $search)}}");
        var shared = GetVariableDefinition(document, "search");
        var items = new[]
        {
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"2"}""")
        };

        // act
        var json = Write(items, [shared]);

        // assert
        json.MatchInlineSnapshot(
            """{"search":"abc","_0___fusion_1_id":"1","_1___fusion_1_id":"2"}""");
    }

    [Fact]
    public void Write_Should_CopyValuesVerbatim_When_ValuesAreNested()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup("query($__fusion_1_key: KeyInput!){byKey(key: $__fusion_1_key){name}}");
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_key":{"a":[1,2,{"b":null}],"c":"x\"y"}}"""),
            CreateItem(memory, document, """{"__fusion_1_key":{"a":[],"c":true}}""")
        };

        // act
        var json = Write(items, []);

        // assert
        json.MatchInlineSnapshot(
            """{"_0___fusion_1_key":{"a":[1,2,{"b":null}],"c":"x\"y"},"_1___fusion_1_key":{"a":[],"c":true}}""");
    }

    [Fact]
    public void Write_Should_CopyValuesVerbatim_When_AValueCrossesAChunkBoundary()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup("query($__fusion_1_key: KeyInput!){byKey(key: $__fusion_1_key){name}}");
        // The values start shortly before the boundary, so the value of the variable is stored in
        // two chunks while its name is not.
        PadToBoundary(memory, bytesBeforeBoundary: 22);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_key":{"a":[1,2,{"b":null}],"c":"x\"y"}}""")
        };

        // act
        var json = Write(items, []);

        // assert
        json.MatchInlineSnapshot("""{"_0___fusion_1_key":{"a":[1,2,{"b":null}],"c":"x\"y"}}""");
    }

    [Fact]
    public void Write_Should_PrefixTheName_When_ANameCrossesAChunkBoundary()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup("query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name}}");
        // The values start six bytes before the boundary, so the name of the variable is stored in
        // two chunks.
        PadToBoundary(memory, bytesBeforeBoundary: 6);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };

        // act
        var json = Write(items, []);

        // assert
        json.MatchInlineSnapshot("""{"_0___fusion_1_id":"1"}""");
    }

    [Fact]
    public void Write_Should_PrefixTheName_When_ANameExceedingTheScratchBufferCrossesAChunkBoundary()
    {
        // arrange
        // 257 bytes, one more than the stack buffer a name is gathered into, so the gathered name
        // is held by pooled memory while the prefixed name is composed from it.
        var name = "__fusion_1_" + new string('x', 246);
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup($"query(${name}: ID!){{productById(id: ${name}){{name}}}}");
        // The values start six bytes before the boundary, so the name of the variable is stored in
        // two chunks.
        PadToBoundary(memory, bytesBeforeBoundary: 6);
        var items = new[] { CreateItem(memory, document, $$"""{"{{name}}":"1"}""") };

        // act
        var json = Write(items, []);

        // assert
        Assert.Equal($$"""{"_0_{{name}}":"1"}""", json);
    }

    [Fact]
    public void Write_Should_WriteSharedValueOnce_When_ASharedNameCrossesAChunkBoundary()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(
            "query($search: String, $__fusion_1_id: ID!){productById(id: $__fusion_1_id){name(q: $search)}}");
        var shared = GetVariableDefinition(document, "search");
        PadToBoundary(memory, bytesBeforeBoundary: 6);
        var items = new[]
        {
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"2"}""")
        };

        // act
        var json = Write(items, [shared]);

        // assert
        json.MatchInlineSnapshot(
            """{"search":"abc","_0___fusion_1_id":"1","_1___fusion_1_id":"2"}""");
    }

    private static string Write(
        AliasBatchItem[] items,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, default);
        AliasBatchVariableWriter.Write(writer, items, sharedVariables);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Fills the memory so that the next write starts the given number of bytes before the
    /// boundary of the next chunk.
    /// </summary>
    private static void PadToBoundary(ChunkedArrayWriter memory, int bytesBeforeBoundary)
    {
        var padding = JsonMemory.BufferSize - memory.Position - bytesBeforeBoundary;
        memory.GetSpan(padding)[..padding].Fill((byte)' ');
        memory.Advance(padding);
    }
}
