using System.Text;
using HotChocolate.Skimmed.Serialization;

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

    #region SemanticNonNull

    [Fact]
    public void Parse_SemanticNonNull_Field()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: String @semanticNonNull
                   }
                   """;

        // assert
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(text));

        // assert
        var fieldDefinition = schema.Types
            .OfType<ObjectTypeDefinition>()
            .FirstOrDefault(t => t.Name == "MyObject")?
            .Fields.FirstOrDefault(f => f.Name == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeDefinition>(fieldDefinition.Type);
        Assert.IsType<ScalarTypeDefinition>(fieldReturnType.NullableType);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_Nullable_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull
                   }
                   """;

        // assert
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(text));

        // assert
        var fieldDefinition = schema.Types
            .OfType<ObjectTypeDefinition>()
            .FirstOrDefault(t => t.Name == "MyObject")?
            .Fields.FirstOrDefault(f => f.Name == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeDefinition>(fieldDefinition.Type);
        var innerListType = Assert.IsType<ListTypeDefinition>(fieldReturnType.NullableType);
        Assert.IsType<ScalarTypeDefinition>(innerListType.ElementType);
    }

    [Fact]
    public void Parse_Nullable_List_And_SemanticNonNull_List_Item()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull(levels: [ 1 ])
                   }
                   """;

        // assert
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(text));

        // assert
        var fieldDefinition = schema.Types
            .OfType<ObjectTypeDefinition>()
            .FirstOrDefault(t => t.Name == "MyObject")?
            .Fields.FirstOrDefault(f => f.Name == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<ListTypeDefinition>(fieldDefinition.Type);
        var innerListType = Assert.IsType<SemanticNonNullTypeDefinition>(fieldReturnType.ElementType);
        Assert.IsType<ScalarTypeDefinition>(innerListType.NullableType);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_SemanticNonNull_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [String] @semanticNonNull(levels: [ 0, 1 ])
                   }
                   """;

        // assert
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(text));

        // assert
        var fieldDefinition = schema.Types
            .OfType<ObjectTypeDefinition>()
            .FirstOrDefault(t => t.Name == "MyObject")?
            .Fields.FirstOrDefault(f => f.Name == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeDefinition>(fieldDefinition.Type);
        var listType = Assert.IsType<ListTypeDefinition>(fieldReturnType.NullableType);
        var innerListType = Assert.IsType<SemanticNonNullTypeDefinition>(listType.ElementType);
        Assert.IsType<ScalarTypeDefinition>(innerListType.NullableType);
    }

    [Fact]
    public void Parse_SemanticNonNull_List_And_Nested_Nullable_List_And_SemanticNonNull_ListItem()
    {
        // arrange
        var text = """
                   type MyObject {
                     field: [[String]] @semanticNonNull(levels: [ 0, 2 ])
                   }
                   """;

        // assert
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(text));

        // assert
        var fieldDefinition = schema.Types
            .OfType<ObjectTypeDefinition>()
            .FirstOrDefault(t => t.Name == "MyObject")?
            .Fields.FirstOrDefault(f => f.Name == "field");
        Assert.NotNull(fieldDefinition);
        Assert.Empty(fieldDefinition.Directives);
        var fieldReturnType = Assert.IsType<SemanticNonNullTypeDefinition>(fieldDefinition.Type);
        var listType = Assert.IsType<ListTypeDefinition>(fieldReturnType.NullableType);
        var innerListType = Assert.IsType<ListTypeDefinition>(listType.ElementType);
        var innermostListType = Assert.IsType<SemanticNonNullTypeDefinition>(innerListType.ElementType);
        Assert.IsType<ScalarTypeDefinition>(innermostListType.NullableType);
    }

    #endregion
}
