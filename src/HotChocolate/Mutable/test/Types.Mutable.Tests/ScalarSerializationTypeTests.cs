using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Mutable;

public class ScalarSerializationTypeTests
{
    [Fact]
    public void Custom_Scalar_Serializes_To_String()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @serializeAs(type: STRING)

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.String, type.SerializationType);
        Assert.Null(type.Pattern);
    }

    [Fact]
    public void Custom_Scalar_Serializes_To_Int()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @serializeAs(type: INT)

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.Int, type.SerializationType);
        Assert.Null(type.Pattern);
    }

    [Fact]
    public void Custom_Scalar_Serializes_To_Int_Or_String()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @serializeAs(type: [INT STRING])

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.Int | ScalarSerializationType.String, type.SerializationType);
        Assert.Null(type.Pattern);
    }

    [Fact]
    public void Custom_Scalar_Serializes_To_Invalid()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @serializeAs(type: [INVALID])

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.Undefined, type.SerializationType);
        Assert.Null(type.Pattern);
    }

    [Fact]
    public void Custom_Scalar_Serializes_To_String_With_Pattern()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @serializeAs(type: STRING, pattern: "\\b\\d{3}\\b")

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.String, type.SerializationType);
        Assert.Equal("\\b\\d{3}\\b", type.Pattern);
    }

    [Fact]
    public void Serialization_Type_Resolved_From_SpecifiedBy_Url()
    {
        // arrange
        const string sdl =
            """
            scalar Custom @specifiedBy(url: "https://scalars.graphql.org/chillicream/uuid.html")
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.String, type.SerializationType);
        Assert.Null(type.Pattern);
    }

    [Fact]
    public void SerializeAs_Takes_Precedence_Over_SpecifiedBy_Url()
    {
        // arrange
        const string sdl =
            """
            scalar Custom
                @serializeAs(type: INT)
                @specifiedBy(url: "https://scalars.graphql.org/chillicream/uuid.html")

            directive @serializeAs(type: [ScalarSerializationType!], pattern: String) on SCALAR

            enum ScalarSerializationType {
              STRING
              BOOLEAN
              INT
              FLOAT
              OBJECT
              LIST
            }
            """;

        // act
        var schema = SchemaParser.Parse(sdl);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.Int, type.SerializationType);
    }
}
