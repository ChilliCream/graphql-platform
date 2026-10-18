using System.Globalization;
using CookieCrumble;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class FileMapTrieTests
{
    [Fact]
    public void Parse_Should_PreserveFileBindings_When_MapsContainOrdinaryUploadPaths()
    {
        // arrange
        (string Name, Dictionary<string, string[]> Map, string[][] Paths)[] cases =
        [
            ("SingleFile", new() { ["0"] = ["variables.file"] }, [["file"]]),
            ("List2", new()
            {
                ["0"] = ["variables.files.0"],
                ["1"] = ["variables.files.1"]
            }, [["files", "0"], ["files", "1"]]),
            ("NestedList2", new()
            {
                ["0"] = ["variables.input.files.0"],
                ["1"] = ["variables.input.files.1"]
            }, [["input", "files", "0"], ["input", "files", "1"]]),
            ("EmptyMapControl", new(), [])
        ];
        var results = new List<object>();

        // act
        foreach (var item in cases)
        {
            var trie = FileMapTrie.Parse(item.Map);
            results.Add(new
            {
                item.Name,
                RootFileKey = trie.Root.FileKey,
                Bindings = DescribeBindings(trie, item.Paths),
                Missing = trie.Root.TryGetNode("missing", out _),
                WrongCase = trie.Root.TryGetNode("File", out _),
                Prefix = trie.Root.TryGetNode("variables", out _)
            });
        }

        // assert
        Snapshot.Create()
            .Add(results, "Complete File Bindings")
            .MatchMarkdownSnapshot();
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
        object[] bindings;

        // act
        try
        {
            CultureInfo.CurrentCulture = culture;
            var trie = FileMapTrie.Parse(map);
            bindings = DescribeBindings(trie, [["files", "~2"], ["files", "1"]]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // assert
        Snapshot.Create()
            .Add(bindings, "Current Culture Numeric Bindings")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Parse_Should_PreserveBindingErrors_When_PathsConflict()
    {
        // arrange
        (string Name, Dictionary<string, string[]> Map)[] cases =
        [
            ("DuplicateLeaf", new()
            {
                ["0"] = ["variables.file"],
                ["1"] = ["variables.file"]
            }),
            ("ChildBeforeParent", new()
            {
                ["0"] = ["variables.input.file"],
                ["1"] = ["variables.input"]
            })
        ];
        var results = new List<object>();

        // act
        foreach (var item in cases)
        {
            var exception = Record.Exception(() =>
            {
                _ = FileMapTrie.Parse(item.Map);
            });
            var error = Assert.IsType<InvalidOperationException>(exception);
            results.Add(new { item.Name, Exception = error.GetType().Name, error.Message });
        }

        // assert
        Snapshot.Create()
            .Add(results, "Binding Error Order")
            .MatchMarkdownSnapshot();
    }

    private static object[] DescribeBindings(FileMapTrie trie, string[][] paths)
    {
        var bindings = new List<object>();

        foreach (var path in paths)
        {
            var current = trie.Root;
            var nodes = new List<object>();

            foreach (var key in path)
            {
                Assert.True(current.TryGetNode(key, out var next));
                current = next!;
                nodes.Add(new { Key = key, current.FileKey });
            }

            bindings.Add(new { Nodes = nodes, current.FileKey });
        }

        return bindings.ToArray();
    }
}
