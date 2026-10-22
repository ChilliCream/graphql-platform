using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Transport.Http;

public sealed class FileEntryBuilderPathTests
{
    [Theory]
    [InlineData("Single", """{"file":"$.file(1)"}""", "variables")]
    [InlineData("List", """{"files":["$.file(1)","$.file(2)"]}""", "variables")]
    [InlineData("NestedDuplicate", """{"input":{"files":["$.file(1)","$.file(2)","$.file(1)"]},"text":"kept"}""", "variables")]
    [InlineData("MaximumIndexPrefix", """{"file":"$.file(1)"}""", "2147483647.variables")]
    public void Build_Should_PreservePathsAndFileIdentity_When_FilesAreMapped(
        string scenario,
        string json,
        string prefix)
    {
        // arrange
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(writer, json);
        var lookup = new FileLookup();

        // act
        var (cleanedJson, fileMap) = FileEntryBuilder.Build(writer, variables, lookup, prefix);

        // assert
        using var cleaned = JsonDocument.Parse(cleanedJson.AsSequence());
        Snapshot.Create(scenario)
            .Add(cleaned.RootElement.GetRawText(), "Cleaned Variables", MarkdownLanguages.Json)
            .Add(fileMap.Select(entry => new
            {
                entry.Key,
                entry.Path,
                SameFile = ReferenceEquals(lookup.Files[entry.Key], entry.file)
            }).ToArray(), "File Map")
            .Add(lookup.Calls, "Lookup Order")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("fr-FR")]
    public void Build_Should_PreserveNumericPaths_When_LookupChangesCurrentCulture(string cultureName)
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var changedCulture = (CultureInfo)new CultureInfo(cultureName).Clone();
        changedCulture.NumberFormat.PositiveSign = "positive";
        changedCulture.NumberFormat.NegativeSign = "negative";
        changedCulture.NumberFormat.NativeDigits = ["٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩"];
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(
            writer,
            """{"files":["$.file(1)",null,null,null,null,null,null,null,null,null,"$.file(2)"]}""");
        var lookup = new FileLookup { OnLookup = () => CultureInfo.CurrentCulture = changedCulture };

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // act
            var (_, fileMap) = FileEntryBuilder.Build(writer, variables, lookup, "0010.variables");

            // assert
            Assert.Equal(["10.variables.files.0", "10.variables.files.10"], fileMap.Select(entry => entry.Path));
            Assert.Equal(cultureName, CultureInfo.CurrentCulture.Name);
            Assert.Equal(["1", "2"], lookup.Calls);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Build_Should_ReturnOriginalSegment_When_NoFileMatches()
    {
        // arrange
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(writer, """{"file":"$.file(missing)","text":"kept"}""");
        var startPosition = writer.Position;
        var lookup = new FileLookup();

        // act
        var (cleanedJson, fileMap) = FileEntryBuilder.Build(writer, variables, lookup);

        // assert
        Assert.Equal(variables, cleanedJson);
        Assert.Empty(fileMap);
        Assert.Equal(startPosition, writer.Position);
        Assert.Equal(["missing"], lookup.Calls);
    }

    [Fact]
    public void Build_Should_PreserveDeepPrefix_When_JsonIsShallow()
    {
        // arrange
        var prefix = string.Join('.', Enumerable.Repeat("segment", 512));
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(writer, """{"file":"$.file(1)"}""");

        // act
        var (_, fileMap) = FileEntryBuilder.Build(writer, variables, new FileLookup(), prefix);

        // assert
        Assert.Equal(prefix + ".file", Assert.Single(fileMap).Path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("variables..file")]
    [InlineData("-1.variables")]
    public void Build_Should_RejectInvalidPrefixBeforeLookup_When_PrefixIsInvalid(string prefix)
    {
        // arrange
        using var writer = new ChunkedArrayWriter();
        var variables = CreateSegment(writer, """{"file":"$.file(1)"}""");
        var lookup = new FileLookup();

        // act
        var exception = Record.Exception(() => FileEntryBuilder.Build(writer, variables, lookup, prefix));

        // assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Empty(lookup.Calls);
    }

    private static JsonSegment CreateSegment(ChunkedArrayWriter writer, string json)
    {
        var start = writer.Position;
        var bytes = Encoding.UTF8.GetBytes(json);
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
        return JsonSegment.Create(writer.GetSequence(start, writer.Position - start));
    }

    private sealed class FileLookup : IFileLookup
    {
        public Dictionary<string, IFile> Files { get; } = new()
        {
            ["1"] = new TestFile("first"),
            ["2"] = new TestFile("second")
        };

        public List<string> Calls { get; } = [];

        public Action? OnLookup { get; init; }

        public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        {
            Calls.Add(name);
            OnLookup?.Invoke();
            return Files.TryGetValue(name, out file);
        }
    }

    private sealed class TestFile(string name) : IFile
    {
        public string Name => name;

        public long? Length => 0;

        public string? ContentType => null;

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Stream OpenReadStream() => Stream.Null;
    }
}
