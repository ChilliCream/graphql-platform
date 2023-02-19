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

        // act
        var success = schema.RenameType("Bar", "Baz");

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
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

    [Fact]
    public void Rename_Member()
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

        // act
        var success = schema.RenameMember(new("Bar", "field"), "__field");

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Foo {
                  field: Bar
                }

                type Bar {
                  __field: String
                }

                scalar String
                """);
    }

    [Fact]
    public void AddDirective_To_Type()
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
        var directiveType = new DirectiveType("source");
        directiveType.Arguments.Add(new("name", new NonNullType(schema.Types["String"])));
        schema.Directives.Add(directiveType);

        // act
        var success = schema.AddDirective(
            "Bar",
            new Directive(
                directiveType,
                new Argument("name", "abc")));

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Foo {
                  field: Bar
                }

                type Bar @source(name: "abc") {
                  field: String
                }

                scalar String
                """);
    }

    [Fact]
    public void AddDirective_To_Field()
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
        var directiveType = new DirectiveType("source");
        directiveType.Arguments.Add(new("name", new NonNullType(schema.Types["String"])));
        schema.Directives.Add(directiveType);

        // act
        var success = schema.AddDirective(
            new SchemaCoordinate("Bar", "field"),
            new Directive(
                directiveType,
                new Argument("name", "abc")));

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Foo {
                  field: Bar
                }

                type Bar {
                  field: String @source(name: "abc")
                }

                scalar String
                """);
    }
}
