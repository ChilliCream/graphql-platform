using System.Text;
using CookieCrumble;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Skimmed;

public class RefactoringTests
{
    [Fact]
    public void Rename_Type()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field: Bar
            }

            type Bar {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        schema.RenameType("Bar", "Baz");

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            type Foo {
                field: Baz
            }

            type Baz {
                field: String
            }

            scalar String
            """);
    }
}
