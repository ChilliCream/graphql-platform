using System.Text;
using HotChocolate.Language;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Skimmed;

public class SchemaParserTests
{
    [Fact]
    public void Parse_Single_Object_Type()
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
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var fooType = Assert.IsType<ObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields,
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<ScalarTypeDefinition>(field.Type);
                        Assert.Equal("String", fieldType.Name);
                    });
            },
            type =>
            {
                var stringType = Assert.IsType<ScalarTypeDefinition>(type);
                Assert.Equal("String", stringType.Name);
            });
    }

    [Fact]
    public void Parse_Single_Object_With_Missing_Field_Type()
    {
        // arrange
        var sdl =
            """
            type Foo {
                field: Bar
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var stringType = Assert.IsType<MissingTypeDefinition>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<ObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields,
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MissingTypeDefinition>(field.Type);
                        Assert.Equal("Bar", fieldType.Name);
                    });
            });
    }

    [Fact]
    public void Parse_Single_Object_Extension_With_Missing_Field_Type()
    {
        // arrange
        var sdl =
            """
            extend type Foo {
                field: Bar
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var stringType = Assert.IsType<MissingTypeDefinition>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<ObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.True(fooType.IsTypeExtension());
                Assert.Collection(
                    fooType.Fields,
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MissingTypeDefinition>(field.Type);
                        Assert.Equal("Bar", fieldType.Name);
                    });
            });
    }

    [Fact]
    public void Parse_With_Custom_BuiltIn_Scalar_Type()
    {
        // arrange
        var sdl =
            """
            "Custom description"
            scalar String @custom
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var scalar = schema.Types["String"];

        // assert
        Assert.Equal("Custom description", scalar.Description);
        Assert.True(scalar.Directives.ContainsName("custom"));
    }

    [Fact]
    public void Parse_With_Custom_BuiltIn_Directive()
    {
        // arrange
        var sdl =
            """
            "Custom description"
            directive @skip("Custom argument description" ifCustom: String! @custom) on ENUM_VALUE
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        var directive = schema.DirectiveDefinitions["skip"];
        var argument = directive.Arguments["ifCustom"];

        // assert
        Assert.Equal("Custom description", directive.Description);
        Assert.Equal("Custom argument description", argument.Description);
        Assert.Equal("String", argument.Type.NamedType().Name);
        Assert.True(argument.Directives.ContainsName("custom"));
        Assert.Equal(DirectiveLocation.EnumValue, directive.Locations);
    }

    [Fact]
    public void Parse_Input_Object_With_Default_Value()
    {
        // arrange
        var sdl =
            """
            input BookFilter {
                genre: Genre = FANTASY
            }
            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        // assert
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var inputType = Assert.IsType<InputObjectTypeDefinition>(type);
                Assert.Equal("BookFilter", inputType.Name);
                var genreField = Assert.Single(inputType.Fields);
                Assert.Equal("genre", genreField.Name);
                Assert.IsType<EnumTypeDefinition>(genreField.Type);
                Assert.NotNull(genreField.DefaultValue);
                Assert.Equal("FANTASY", genreField.DefaultValue.Value);
            },
            type =>
            {
                var genreType = Assert.IsType<EnumTypeDefinition>(type);
                Assert.Equal("Genre", genreType.Name);
            });
    }

    [Fact]
    public void Parse_Input_Object_With_Multiple_Default_Values()
    {
        // arrange
        var sdl =
            """
            input BookFilter {
                genre: Genre = FANTASY
                author: String = "Lorem ipsum"
            }
            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }

            scalar String
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        // assert
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var inputType = Assert.IsType<InputObjectTypeDefinition>(type);
                Assert.Equal("BookFilter", inputType.Name);
                Assert.Collection(inputType.Fields.OrderBy(f => f.Name),
                    authorField =>
                    {
                        Assert.Equal("author", authorField.Name);
                        var fieldType = Assert.IsType<ScalarTypeDefinition>(authorField.Type);
                        Assert.Equal("String", fieldType.Name);
                        Assert.NotNull(authorField.DefaultValue);
                        Assert.Equal("Lorem ipsum", authorField.DefaultValue.Value);
                    },
                    genreField =>
                    {
                        Assert.Equal("genre", genreField.Name);
                        Assert.IsType<EnumTypeDefinition>(genreField.Type);
                        Assert.NotNull(genreField.DefaultValue);
                        Assert.Equal("FANTASY", genreField.DefaultValue.Value);
                    });
            },
            type =>
            {
                var genreType = Assert.IsType<EnumTypeDefinition>(type);
                Assert.Equal("Genre", genreType.Name);
            },
            type =>
            {
                var stringType = Assert.IsType<ScalarTypeDefinition>(type);
                Assert.Equal("String", stringType.Name);
            });
    }
}
