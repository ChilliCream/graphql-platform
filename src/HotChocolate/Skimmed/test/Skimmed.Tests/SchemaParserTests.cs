using System.Text;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Skimmed.WellKnownContextData;

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
                var fooType = Assert.IsType<ObjectType>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields,
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<ScalarType>(field.Type);
                        Assert.Equal("String", fieldType.Name);
                    });
            },
            type =>
            {
                var stringType = Assert.IsType<ScalarType>(type);
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
                var stringType = Assert.IsType<MissingType>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<ObjectType>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.Collection(
                    fooType.Fields,
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
                var stringType = Assert.IsType<MissingType>(type);
                Assert.Equal("Bar", stringType.Name);
            },
            type =>
            {
                var fooType = Assert.IsType<ObjectType>(type);
                Assert.Equal("Foo", fooType.Name);
                Assert.True(fooType.ContextData.ContainsKey(TypeExtension));
                Assert.Collection(
                    fooType.Fields,
                    field =>
                    {
                        Assert.Equal("field", field.Name);
                        var fieldType = Assert.IsType<MissingType>(field.Type);
                        Assert.Equal("Bar", fieldType.Name);
                    });
            });
    }
}
