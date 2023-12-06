using System.Text;
using CookieCrumble;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Skimmed;

public class RefactoringTests
{
    [Fact]
    public void Rename_ObjectType()
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
        var success = schema.RenameMember(new SchemaCoordinate("Bar"), "Baz");

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Baz {
                  field: String
                }

                type Foo {
                  field: Baz
                }
                """);
    }

    [Fact]
    public void Rename_UnionType()
    {
        // arrange
        var sdl =
            """
            union FooOrBar = Foo | Bar

            type Foo {
                field: Bar
            }

            type Bar {
                field: String
            }

            type Baz {
                some: FooOrBar
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var success = schema.RenameMember(new SchemaCoordinate("FooOrBar"), "FooOrBar1");

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Bar {
                  field: String
                }

                type Baz {
                  some: FooOrBar1
                }

                type Foo {
                  field: Bar
                }

                union FooOrBar1 = Foo | Bar
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
        var success = schema.RenameMember(new SchemaCoordinate("Bar", "field"), "__field");

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Bar {
                  __field: String
                }

                type Foo {
                  field: Bar
                }
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
        directiveType.Locations = DirectiveLocation.TypeSystem;
        schema.DirectiveTypes.Add(directiveType);

        // act
        var success = schema.AddDirective(
            new SchemaCoordinate("Bar"),
            new Directive(
                directiveType,
                new Argument("name", "abc")));

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Bar
                  @source(name: "abc") {
                  field: String
                }

                type Foo {
                  field: Bar
                }

                directive @source(name: String!) on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
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
        directiveType.Locations = DirectiveLocation.TypeSystem;
        schema.DirectiveTypes.Add(directiveType);

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
                type Bar {
                  field: String
                    @source(name: "abc")
                }

                type Foo {
                  field: Bar
                }

                directive @source(name: String!) on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
                """);
    }

    [Fact]
    public void Remove_ObjectType()
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
        var success = schema.RemoveMember(new SchemaCoordinate("Bar"));

        // assert
        Assert.True(success);

        SchemaFormatter
            .FormatAsString(schema)
            .MatchInlineSnapshot(
                """
                type Foo {

                }
                """);
    }
}
