using System.Text;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Skimmed;

public class ToStringTests
{
    [Fact]
    public void ObjectType_ToString()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        schema.Types["Foo"].ToString().MatchInlineSnapshot(
            """
            type Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void InterfaceType_ToString()
    {
        // arrange
        var sdl =
            """
            interface Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        schema.Types["Foo"].ToString().MatchInlineSnapshot(
            """
            interface Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void InputObjectType_ToString()
    {
        // arrange
        var sdl =
            """
            input Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        schema.Types["Foo"].ToString().MatchInlineSnapshot(
            """
            input Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void OutputField_ToString()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((ObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field: String
                """);
    }

    [Fact]
    public void OutputField_WithArg_ToString()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field(a: String): String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((ObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field(a: String): String
                """);
    }

    [Fact]
    public void InputField_ToString()
    {
        // arrange
        var sdl =
            """
            input Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((InputObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field: String
                """);
    }
}
