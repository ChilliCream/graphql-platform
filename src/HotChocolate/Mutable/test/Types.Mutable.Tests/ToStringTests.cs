using System.Text;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Mutable;

public class ToStringTests
{
    [Fact]
    public void ObjectType_ToString()
    {
        // arrange
        const string sdl =
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
        const string sdl =
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
        const string sdl =
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
        const string sdl =
            """
            type Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((MutableObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field: String
                """);
    }

    [Fact]
    public void OutputField_WithArg_ToString()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field(a: String): String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((MutableObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field(a: String): String
                """);
    }

    [Fact]
    public void InputField_ToString()
    {
        // arrange
        const string sdl =
            """
            input Foo {
                field: String
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        ((MutableInputObjectTypeDefinition)schema.Types["Foo"]).Fields["field"]
            .ToString().MatchInlineSnapshot(
                """
                field: String
                """);
    }

    [Fact]
    public void Schema_With_DirectivesOnDirectiveDefinition_ToString()
    {
        // arrange
        const string sdl =
            """
            directive @foo @meta(value: "a") on OBJECT

            directive @meta(value: String) on DIRECTIVE_DEFINITION
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        schema.ToString().MatchInlineSnapshot(
            """
            directive @foo @meta(value: "a") on OBJECT

            directive @meta(value: String) on DIRECTIVE_DEFINITION
            """);
    }

    [Fact]
    public void Schema_With_DirectiveExtension_ToString()
    {
        // arrange
        const string sdl =
            """
            directive @meta(value: String) on DIRECTIVE_DEFINITION

            extend directive @foo @meta(value: "a")
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        schema.ToString().MatchInlineSnapshot(
            """
            extend directive @foo @meta(value: "a")

            directive @meta(value: String) on DIRECTIVE_DEFINITION
            """);
    }
}
