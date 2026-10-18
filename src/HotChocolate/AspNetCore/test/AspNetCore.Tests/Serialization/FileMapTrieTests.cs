using System.Globalization;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class FileMapTrieTests
{
    [Theory]
    [InlineData("variables.file", new string[] { "file" })]
    [InlineData("variables.files.0", new string[] { "files", "0" })]
    [InlineData("variables.input.files.0", new string[] { "input", "files", "0" })]
    public void Parse_Should_PreserveFileBindings_When_MapsContainOrdinaryUploadPaths(
        string firstPath,
        string[] firstKeys)
    {
        // arrange
        var isList = firstKeys[^1] == "0";
        Dictionary<string, string[]> map = new() { ["0"] = [firstPath] };
        if (isList)
        {
            map.Add("1", [firstPath[..^1] + "1"]);
        }

        // act
        var trie = FileMapTrie.Parse(map);

        // assert
        Assert.Null(trie.Root.FileKey);
        Assert.Equal(
            firstKeys.Select((key, index) => (key, index == firstKeys.Length - 1 ? "0" : null)),
            GetNodes(trie, firstKeys));
        if (isList)
        {
            var secondKeys = firstKeys.ToArray();
            secondKeys[^1] = "1";
            Assert.Equal(
                secondKeys.Select((key, index) => (key, index == secondKeys.Length - 1 ? "1" : null)),
                GetNodes(trie, secondKeys));
        }
        Assert.Equal(
            (false, false, false),
            (trie.Root.TryGetNode("missing", out _),
                trie.Root.TryGetNode("File", out _),
                trie.Root.TryGetNode("variables", out _)));
    }

    [Fact]
    public void Parse_Should_ReturnEmptyTrie_When_MapIsEmpty()
    {
        // arrange
        Dictionary<string, string[]> map = new();

        // act
        var trie = FileMapTrie.Parse(map);

        // assert
        Assert.Null(trie.Root.FileKey);
        Assert.Equal(
            (false, false, false),
            (trie.Root.TryGetNode("missing", out _),
                trie.Root.TryGetNode("File", out _),
                trie.Root.TryGetNode("variables", out _)));
    }

    [Fact]
    public void Parse_Should_NormalizeNumericKeys_When_CurrentCultureDefinesTheSign()
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        Dictionary<string, string[]> map = new()
        {
            ["0"] = ["variables.files.~2"],
            ["1"] = ["variables.files. +1 "]
        };
        FileMapTrie trie;

        // act
        try
        {
            CultureInfo.CurrentCulture = culture;
            trie = FileMapTrie.Parse(map);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // assert
        Assert.Equal(
            new (string, string?)[] { ("files", null), ("~2", "0") },
            GetNodes(trie, ["files", "~2"]));
        Assert.Equal(
            new (string, string?)[] { ("files", null), ("1", "1") },
            GetNodes(trie, ["files", "1"]));
    }

    [Theory]
    [InlineData("variables.file", "variables.file", "A filename can only be set once.")]
    [InlineData("variables.input.file", "variables.input", "A filename can only be set on a leaf node.")]
    public void Parse_Should_PreserveBindingErrors_When_PathsConflict(
        string firstPath,
        string secondPath,
        string message)
    {
        // arrange
        Dictionary<string, string[]> map = new()
        {
            ["0"] = [firstPath],
            ["1"] = [secondPath]
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => FileMapTrie.Parse(map));

        // assert
        Assert.Equal(message, exception.Message);
    }

    private static IEnumerable<(string Key, string? FileKey)> GetNodes(FileMapTrie trie, string[] path)
    {
        var current = trie.Root;

        foreach (var key in path)
        {
            Assert.True(current.TryGetNode(key, out var next));
            current = next;
            yield return (key, current.FileKey);
        }
    }
}
