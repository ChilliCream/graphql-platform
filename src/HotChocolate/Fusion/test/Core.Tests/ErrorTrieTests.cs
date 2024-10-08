using System.Buffers;
using CookieCrumble;
using CookieCrumble.Formatters;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion;

public class ErrorTrieTests
{
    [Fact]
    public void Build_Error_Trie_From_Errors()
    {
        // arrange
        List<IError> errors = [
            ErrorBuilder.New().SetMessage("Root level error").Build(),
            ErrorBuilder.New().SetMessage("First level error").SetPath(["first"]).Build(),
            ErrorBuilder.New().SetMessage("Second level error").SetPath(["first", "second"]).Build(),
            ErrorBuilder.New().SetMessage("Another first level error").SetPath(["first"]).Build(),
            ErrorBuilder.New().SetMessage("Third level error").SetPath(["first", "second", 1]).Build(),
            ErrorBuilder.New().SetMessage("Third level error 2").SetPath(["first", "second", 1]).Build(),
            ErrorBuilder.New().SetMessage("Fourth level error").SetPath(["first", "test", 1, "other"]).Build(),
            ErrorBuilder.New().SetMessage("Another third level error").SetPath(["first", "second", 1]).Build(),
        ];

        // act
        var trie = ErrorTrie.FromErrors(errors);

        // assert
        trie.MatchInlineSnapshot(
            """
            {
              Errors: [
                Root level error,
              ],
              [first]: {
                 Errors: [
                   First level error,
                   Another first level error,
                 ],
                 [second]: {
                    Errors: [
                      Second level error,
                    ],
                    [1]: {
                       Errors: [
                         Third level error,
                         Third level error 2,
                         Another third level error,
                       ],
                    },
                 },
                 [test]: {
                    [1]: {
                       [other]: {
                          Errors: [
                            Fourth level error,
                          ],
                       },
                    },
                 },
              },
            }
            """,
            formatter: new ErrorTrieSnapshotFormatter());
    }
}

internal sealed class ErrorTrieSnapshotFormatter : SnapshotValueFormatter<ErrorTrie>
{
    protected override void Format(IBufferWriter<byte> snapshot, ErrorTrie value)
    {
        snapshot.Append("{\n");
        PrintTrie(snapshot, value, 0);
        snapshot.Append("}");
    }

    private static void PrintTrie(IBufferWriter<byte> snapshot, ErrorTrie value, int step)
    {
        var indentation = new string(Enumerable.Repeat(' ', step).ToArray());

        if (value.Errors is not null)
        {
            snapshot.Append(indentation + "  Errors: [\n");

            foreach (var error in value.Errors)
            {
                snapshot.Append(indentation + "    " + error.Message + ",\n");
            }

            snapshot.Append(indentation + "  ],\n");
        }

        foreach (var (key, subTrie) in value)
        {
            snapshot.Append(indentation + "  [" + key + "]: {\n");
            PrintTrie(snapshot, subTrie, step + 3);
            snapshot.Append(indentation + "  },\n");
        }
    }
}
