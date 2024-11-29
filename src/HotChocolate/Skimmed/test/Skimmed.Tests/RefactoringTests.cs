using System.Text;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public class RefactoringTests
{
    [Fact]
    public void Rename_ObjectType()
    {
        // arrange
        const string sdl =
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
        const string sdl =
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
        const string sdl =
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
        const string sdl =
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
        var directiveType = new DirectiveDefinition("source");
        directiveType.Arguments.Add(new("name", new NonNullTypeDefinition(schema.Types["String"])));
        directiveType.Locations = DirectiveLocation.TypeSystem;
        schema.DirectiveDefinitions.Add(directiveType);

        // act
        var success = schema.AddDirective(
            new SchemaCoordinate("Bar"),
            new Directive(
                directiveType,
                new ArgumentAssignment("name", "abc")));

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
        const string sdl =
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
        var directiveType = new DirectiveDefinition("source");
        directiveType.Arguments.Add(new("name", new NonNullTypeDefinition(schema.Types["String"])));
        directiveType.Locations = DirectiveLocation.TypeSystem;
        schema.DirectiveDefinitions.Add(directiveType);

        // act
        var success = schema.AddDirective(
            new SchemaCoordinate("Bar", "field"),
            new Directive(
                directiveType,
                new ArgumentAssignment("name", "abc")));

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
        const string sdl =
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
