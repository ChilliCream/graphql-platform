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

    [Fact]
    public void Parse_Complex_Type_With_Duplicate_Field()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field: String
                field: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "A field with the name 'field' has already been defined on the type 'Foo'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Complex_Type_With_Field_Not_Returning_Output_Type()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field: FooInput
            }

            input FooInput {
                bar: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The field 'Foo.field' must return an output type.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Complex_Type_With_Duplicate_Argument()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field(arg: String, arg: String): String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "An argument with the name 'arg' has already been defined on the field 'Foo.field'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Complex_Type_With_Argument_Not_Accepting_Input_Type()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field(arg: Foo): String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The argument 'Foo.field(arg:)' must accept an input type.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Input_Object_Type_With_Duplicate_Field()
    {
        // arrange
        const string sdl =
            """
            input Foo {
                field: String
                field: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "A field with the name 'field' has already been defined on the Input Object type 'Foo'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Input_Object_Type_With_Field_Not_Accepting_Input_Type()
    {
        // arrange
        const string sdl =
            """
            input FooInput {
                field: FooObject
            }

            type FooObject {
                field: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The Input Object field 'FooInput.field' must accept an input type.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Directive_With_Argument_Not_Accepting_Input_Type()
    {
        // arrange
        const string sdl =
            """
            directive @foo(arg: Foo) on FIELD

            type Foo {
                field: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The argument '@foo(arg:)' must accept an input type.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Union_Type_With_Non_Object_Member()
    {
        // arrange
        const string sdl =
            """
            union Foo = Bar

            input Bar {
                field: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The Union type 'Foo' cannot include the type 'Bar'. Unions can only contain Object types.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Duplicate_Type_Definition()
    {
        // arrange
        const string sdl =
            """
            scalar Foo
            scalar Foo
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "A type with the name 'Foo' has already been defined.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Duplicate_Directive_Definition()
    {
        // arrange
        const string sdl =
            """
            directive @foo on FIELD
            directive @foo on FIELD
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "A directive with the name '@foo' has already been defined.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_AddDirectiveToExistingField_When_ExtensionRedeclaresField()
    {
        // arrange
        const string sdl =
            """
            type Query { id: String }

            extend type Query {
                id: String @deprecated(reason: "Use newId")
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var queryType = Assert.IsType<MutableObjectTypeDefinition>(schema.Types["Query"]);
        var field = Assert.Single(queryType.Fields.AsEnumerable());
        Assert.Equal("id", field.Name);
        Assert.True(field.IsDeprecated);
        Assert.Equal("Use newId", field.DeprecationReason);
        Assert.True(field.Directives.ContainsName("deprecated"));
    }

    [Fact]
    public void Parse_Should_AddArgumentToExistingField_When_ExtensionAddsNewArgument()
    {
        // arrange
        const string sdl =
            """
            type Query {
                user(id: String): String
            }

            extend type Query {
                user(name: String): String
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var queryType = Assert.IsType<MutableObjectTypeDefinition>(schema.Types["Query"]);
        var field = Assert.Single(queryType.Fields.AsEnumerable());
        Assert.Collection(
            field.Arguments.AsEnumerable(),
            arg => Assert.Equal("id", arg.Name),
            arg => Assert.Equal("name", arg.Name));
    }

    [Fact]
    public void Parse_Should_AddDirectiveToExistingArgument_When_ExtensionRedeclaresArgument()
    {
        // arrange
        const string sdl =
            """
            type Query {
                user(id: String): String
            }

            extend type Query {
                user(id: String @deprecated(reason: "Use newId")): String
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var queryType = Assert.IsType<MutableObjectTypeDefinition>(schema.Types["Query"]);
        var field = Assert.Single(queryType.Fields.AsEnumerable());
        var argument = Assert.Single(field.Arguments.AsEnumerable());
        Assert.Equal("id", argument.Name);
        Assert.True(argument.IsDeprecated);
        Assert.Equal("Use newId", argument.DeprecationReason);
        Assert.True(argument.Directives.ContainsName("deprecated"));
    }

    [Fact]
    public void Parse_Should_RetainOriginalDescription_When_ExtensionRedeclaresFieldWithoutDescription()
    {
        // arrange
        const string sdl =
            """"
            type Query {
                """desc"""
                id: String
            }

            extend type Query {
                id: String @deprecated
            }
            """";

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var queryType = Assert.IsType<MutableObjectTypeDefinition>(schema.Types["Query"]);
        var field = Assert.Single(queryType.Fields.AsEnumerable());
        Assert.Equal("desc", field.Description);
        Assert.True(field.IsDeprecated);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionRedeclaresFieldWithDifferentDescription()
    {
        // arrange
        const string sdl =
            """"
            type Query {
                """original"""
                id: String
            }

            extend type Query {
                """different"""
                id: String
            }
            """";

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The description on field 'id' of type 'Query' in the extension does not match the original definition.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionFieldTypeMismatches()
    {
        // arrange
        const string sdl =
            """
            type Query { id: String }

            extend type Query {
                id: Int
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The field 'id' on type 'Query' was extended with a different type than the original definition.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionArgumentDefaultValueMismatches()
    {
        // arrange
        const string sdl =
            """
            type Query {
                user(id: String = "a"): String
            }

            extend type Query {
                user(id: String = "b"): String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The argument 'id' on field 'Query.user' was extended with a different default value than the original "
            + "definition.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionAppliesNonRepeatableDirectiveAlreadyApplied()
    {
        // arrange
        const string sdl =
            """
            type Query {
                id: String @deprecated(reason: "first")
            }

            extend type Query {
                id: String @deprecated(reason: "second")
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "The non-repeatable directive '@deprecated' was already applied to 'Query.id' "
            + "and cannot be applied again by an extension.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_AppendBothDirectives_When_ExtensionAppliesRepeatableDirectiveAlreadyApplied()
    {
        // arrange
        const string sdl =
            """
            directive @tag(name: String!) repeatable on FIELD_DEFINITION

            type Query {
                id: String @tag(name: "a")
            }

            extend type Query {
                id: String @tag(name: "b")
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var queryType = Assert.IsType<MutableObjectTypeDefinition>(schema.Types["Query"]);
        var field = Assert.Single(queryType.Fields.AsEnumerable());
        Assert.Equal(2, field.Directives["tag"].Count());
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionContainsDuplicateFieldNamesWithinItself()
    {
        // arrange
        const string sdl =
            """
            extend type Query {
                id: String
                id: String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "A field with the name 'id' has already been defined on the type 'Query'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionFieldDeclaresDuplicateArgumentName()
    {
        // arrange
        const string sdl =
            """
            type Query { user: String }

            extend type Query {
                user(id: String, id: String): String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "An argument with the name 'id' has already been defined on the field 'Query.user'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_Throw_When_ExtensionRedeclaresArgumentTwiceWithinSameFieldExtension()
    {
        // arrange
        const string sdl =
            """
            type Query { user(id: String): String }

            extend type Query {
                user(id: String, id: String): String
            }
            """;

        // act
        static void Action() => SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        Assert.Equal(
            "An argument with the name 'id' has already been defined on the field 'Query.user'.",
            Assert.Throws<SchemaInitializationException>(Action).Message);
    }

    [Fact]
    public void Parse_Should_AddDirectiveToExistingField_When_InterfaceExtensionRedeclaresField()
    {
        // arrange
        const string sdl =
            """
            interface Node { id: ID }

            extend interface Node {
                id: ID @deprecated(reason: "Use newId")
            }
            """;

        // act
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // assert
        var nodeType = Assert.IsType<MutableInterfaceTypeDefinition>(schema.Types["Node"]);
        var field = Assert.Single(nodeType.Fields.AsEnumerable());
        Assert.Equal("id", field.Name);
        Assert.True(field.IsDeprecated);
        Assert.Equal("Use newId", field.DeprecationReason);
        Assert.True(field.Directives.ContainsName("deprecated"));
    }
}
