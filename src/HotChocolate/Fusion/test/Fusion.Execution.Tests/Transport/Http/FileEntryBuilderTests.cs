using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Transport.Http;

public class FileEntryBuilderTests
{
    [Fact]
    public void Build_Should_Not_DoubleEncode_When_StringValueIsEscaped()
    {
        // arrange
        const string original = "q\" b\\ n\n ué";
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(
            writer,
            $$"""{"file":"$.file(1)","text":{{JsonSerializer.Serialize(original)}}}""");
        var fileLookup = new StubFileLookup("1");

        // act
        var (cleanedJson, fileMap) = FileEntryBuilder.Build(writer, variables, fileLookup);

        // assert
        using var doc = JsonDocument.Parse(cleanedJson.AsSequence());
        Assert.Equal(original, doc.RootElement.GetProperty("text").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("file").ValueKind);
        Assert.Single(fileMap);
    }

    [Fact]
    public void Build_Should_Not_DoubleEncode_When_PropertyNameIsEscaped()
    {
        // arrange
        // The JSON key uses a \uXXXX escape sequence (t = 't'), making it an escaped
        // property name that must decode to the plain name "text".
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(
            writer,
            "{\"\\u0074ext\":\"v\",\"file\":\"$.file(1)\"}");
        var fileLookup = new StubFileLookup("1");

        // act
        var (cleanedJson, _) = FileEntryBuilder.Build(writer, variables, fileLookup);

        // assert
        using var doc = JsonDocument.Parse(cleanedJson.AsSequence());
        Assert.Equal("v", doc.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public void Build_Should_KeepValues_When_TokensSpanArenaSegments()
    {
        // arrange
        using var arena = new MemoryArena();
        using var arenaWriter = new ArenaBufferWriter(arena);

        // the file marker starts 4 bytes before the end of the first segment and the text spans
        // the second segment
        var pad = new string('p', SourceResultDocument.GetDataChunkSize(0) - 22);
        var text = new string('t', SourceResultDocument.GetDataChunkSize(1));
        var variables = CreateSegment(
            arenaWriter,
            $$"""{"pad":"{{pad}}","file":"$.file(1)","text":"{{text}}","count":12345}""");
        using var writer = new ChunkedArrayWriter();
        var fileLookup = new StubFileLookup("1");

        // act
        var (cleanedJson, fileMap) = FileEntryBuilder.Build(writer, variables, fileLookup);

        // assert
        Assert.Equal(
            $$"""{"pad":"{{pad}}","file":null,"text":"{{text}}","count":12345}""",
            Encoding.UTF8.GetString(cleanedJson.AsSequence()));
        Assert.Equal("variables.file", Assert.Single(fileMap).Path);
    }

    private static JsonSegment CreateSegment(ArenaBufferWriter writer, string json)
    {
        var startPosition = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
        return JsonSegment.Create(writer, startPosition, writer.Position - startPosition);
    }

    private static JsonSegment CreateSegment(ChunkedArrayWriter writer, string json)
    {
        var startPosition = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        var span = writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        writer.Advance(bytes.Length);
        return JsonSegment.Create(writer, startPosition, writer.Position - startPosition);
    }

    private sealed class StubFileLookup(string key) : IFileLookup
    {
        private readonly IFile _file = new StubFile();

        public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        {
            if (name == key)
            {
                file = _file;
                return true;
            }

            file = null;
            return false;
        }
    }

    private sealed class StubFile : IFile
    {
        public string Name => "stub";

        public long? Length => 0;

        public string? ContentType => null;

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Stream OpenReadStream() => Stream.Null;
    }
}
