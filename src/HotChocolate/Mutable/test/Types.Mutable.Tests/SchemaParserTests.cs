using System.Text;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Mutable;

public class SchemaParserTests
{
    [Fact]
    public void Parse_Single_Object_Type()
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
        Assert.Collection(
            schema.Types.OrderBy(t => t.Name),
            type =>
            {
                var fooType = Assert.IsType<MutableObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields.AsEnumerable(),
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MutableScalarTypeDefinition>(field.Type);
                        Assert.Equal("String", fieldType.Name);
                    });
            },
            type =>
            {
                var stringType = Assert.IsType<MutableScalarTypeDefinition>(type);
                Assert.Equal("String", stringType.Name);
            });
    }

    [Fact]
    public void Parse_Single_Object_With_Missing_Field_Type()
    {
        // arrange
        const string sdl =
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
                var stringType = Assert.IsType<MissingType>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<MutableObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields.AsEnumerable(),
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MissingType>(field.Type);
                        Assert.Equal("Bar", fieldType.Name);
                    });
            });
    }

    [Fact]
    public void Parse_Single_Object_Extension_With_Missing_Field_Type()
    {
        // arrange
        const string sdl =
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
                var stringType = Assert.IsType<MissingType>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<MutableObjectTypeDefinition>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.True(fooType.IsTypeExtension());
                Assert.Collection(
                    fooType.Fields.AsEnumerable(),
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MissingType>(field.Type);
                        Assert.Equal("Bar", fieldType.Name);
                    });
            });
    }

    [Fact]
    public void Parse_With_Custom_BuiltIn_Scalar_Type()
    {
        // arrange
        const string sdl =
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
        const string sdl =
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
        Assert.Equal("String", argument.Type.AsTypeDefinition().Name);
        Assert.True(argument.Directives.ContainsName("custom"));
        Assert.Equal(DirectiveLocation.EnumValue, directive.Locations);
    }

    [Fact]
    public void Parse_Input_Object_With_Default_Value()
    {
        // arrange
        const string sdl =
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
                var inputType = Assert.IsType<MutableInputObjectTypeDefinition>(type);
                Assert.Equal("BookFilter", inputType.Name);
                var genreField = Assert.Single(inputType.Fields.AsEnumerable());
                Assert.Equal("genre", genreField.Name);
                Assert.IsType<MutableEnumTypeDefinition>(genreField.Type);
                Assert.NotNull(genreField.DefaultValue);
                Assert.Equal("FANTASY", genreField.DefaultValue.Value);
            },
            type =>
            {
                var genreType = Assert.IsType<MutableEnumTypeDefinition>(type);
                Assert.Equal("Genre", genreType.Name);
            });
    }

    [Fact]
    public void Parse_Input_Object_With_Multiple_Default_Values()
    {
        // arrange
        const string sdl =
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
                var inputType = Assert.IsType<MutableInputObjectTypeDefinition>(type);
                Assert.Equal("BookFilter", inputType.Name);
                Assert.Collection(inputType.Fields.AsEnumerable().OrderBy(f => f.Name),
                    authorField =>
                    {
                        Assert.Equal("author", authorField.Name);
                        var fieldType = Assert.IsType<MutableScalarTypeDefinition>(authorField.Type);
                        Assert.Equal("String", fieldType.Name);
                        Assert.NotNull(authorField.DefaultValue);
                        Assert.Equal("Lorem ipsum", authorField.DefaultValue.Value);
                    },
                    genreField =>
                    {
                        Assert.Equal("genre", genreField.Name);
                        Assert.IsType<MutableEnumTypeDefinition>(genreField.Type);
                        Assert.NotNull(genreField.DefaultValue);
                        Assert.Equal("FANTASY", genreField.DefaultValue.Value);
                    });
            },
            type =>
            {
                var genreType = Assert.IsType<MutableEnumTypeDefinition>(type);
                Assert.Equal("Genre", genreType.Name);
            },
            type =>
            {
                var stringType = Assert.IsType<MutableScalarTypeDefinition>(type);
                Assert.Equal("String", stringType.Name);
            });
    }
}
