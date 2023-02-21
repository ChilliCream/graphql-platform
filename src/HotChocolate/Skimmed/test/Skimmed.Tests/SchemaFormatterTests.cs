using System.Text;
using CookieCrumble;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Skimmed;

public class SchemaFormatterTests
{
    [Fact]
    public void Format_Single_InputObject_Type()
    {
        // arrange
        var sdl =
            """
            input Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            input Foo {
              field: String
            }

            scalar String
            """);
    }

    [Fact]
    public void Format_Two_InputObject_Extensions_Into_One()
    {
        // arrange
        var sdl =
            """
            extend input Foo {
                field1: String
            }

            extend input Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            scalar String

            extend input Foo {
              field1: String
              field2: [String]!
            }
            """);
    }

    [Fact]
    public void Format_Single_Object_Type()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            type Foo {
              field: String
            }

            scalar String
            """);
    }

    [Fact]
    public void Format_Two_Object_Extensions_Into_One()
    {
        // arrange
        var sdl =
            """
            extend type Foo {
                field1: String
            }

            extend type Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            scalar String

            extend type Foo {
              field1: String
              field2: [String]!
            }
            """);
    }

    [Fact]
    public void Format_Single_Interface_Type()
    {
        // arrange
        var sdl =
            """
            interface Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            interface Foo {
              field: String
            }

            scalar String
            """);
    }

    [Fact]
    public void Format_Two_interface_Extensions_Into_One()
    {
        // arrange
        var sdl =
            """
            extend interface Foo {
                field1: String
            }

            extend interface Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            scalar String

            extend interface Foo {
              field1: String
              field2: [String]!
            }
            """);
    }
}
