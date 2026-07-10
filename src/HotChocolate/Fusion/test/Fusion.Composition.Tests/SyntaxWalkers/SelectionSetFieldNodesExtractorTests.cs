using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.SyntaxWalkers;

public sealed class SelectionSetFieldNodesExtractorTests
{
    [Theory]
#pragma warning disable xUnit1044
    [MemberData(nameof(ExamplesData))]
#pragma warning restore xUnit1044
    public void Examples(string selectionSetText, (string, string[])[] expected)
    {
        // arrange
        var selectionSet = ParseSelectionSet(selectionSetText);
        var extractor = new SelectionSetFieldNodesExtractor();

        // act
        var fieldNodes = extractor.ExtractFieldNodes(selectionSet);

        // assert
        Assert.Equal(expected.Length, fieldNodes.Length);

        for (var i = 0; i < fieldNodes.Length; i++)
        {
            var (fieldNode, fieldNamePath) = fieldNodes[i];

            Assert.Equal(expected[i].Item1, fieldNode.Name.Value);
            Assert.Equal(expected[i].Item2, fieldNamePath);
        }
    }

    public static TheoryData<string, (string, string[])[]> ExamplesData()
    {
        return new TheoryData<string, (string, string[])[]>
        {
            // Nested.
            {
                "{ a { b { c d } } e { f { g h } } } }",
                [
                    ("a", ["a"]),
                    ("b", ["a", "b"]),
                    ("c", ["a", "b", "c"]),
                    ("d", ["a", "b", "d"]),
                    ("e", ["e"]),
                    ("f", ["e", "f"]),
                    ("g", ["e", "f", "g"]),
                    ("h", ["e", "f", "h"])
                ]
            },
            // Inline fragments.
            {
                "{ ... on X { a { b { c d } } } ... on Y { e { f { g h } } } }",
                [
                    ("a", ["a"]),
                    ("b", ["a", "b"]),
                    ("c", ["a", "b", "c"]),
                    ("d", ["a", "b", "d"]),
                    ("e", ["e"]),
                    ("f", ["e", "f"]),
                    ("g", ["e", "f", "g"]),
                    ("h", ["e", "f", "h"])
                ]
            }
        };
    }
}
