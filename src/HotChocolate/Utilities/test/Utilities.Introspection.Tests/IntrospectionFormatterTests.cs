using System.Text.Json;
using static HotChocolate.Utilities.Introspection.IntrospectionClient;

namespace HotChocolate.Utilities.Introspection;

public class IntrospectionFormatterTests
{
    [Fact]
    public void DeserializeStarWarsIntrospectionResult()
    {
        // arrange
        var json = FileResource.Open("StarWarsIntrospectionResult.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchSnapshot();
    }

    [Fact]
    public void DeserializeIntrospectionWithIntDefaultValues()
    {
        // arrange
        var json = FileResource.Open("IntrospectionWithDefaultValues.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchSnapshot();
    }

    [Fact]
    public void DeserializeIntrospectionWithDeprecatedDirectives()
    {
        // arrange
        var json = FileResource.Open("IntrospectionWithDeprecatedDirectives.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: String
            }

            "Built-in String"
            scalar String

            directive @obsolete(
              obsoleteArg: String @deprecated(reason: "Argument no longer supported.")
            ) @deprecated(reason: "Directive no longer supported.") on FIELD
            """);
    }

    [Fact]
    public void DeserializeIntrospectionWithDeprecatedObjects()
    {
        // arrange
        var json = FileResource.Open("IntrospectionWithDeprecatedObjects.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: String
            }

            type DeprecatedObject @deprecated(reason: "Object no longer supported.") {
              field: String
            }

            "Built-in String"
            scalar String
            """);
    }

    [Fact]
    public void DeserializeIntrospectionWithNullDeprecationReason()
    {
        // arrange
        var json = FileResource.Open("IntrospectionWithNullDeprecationReason.json");
        var result = JsonSerializer.Deserialize<IntrospectionResult>(json, SerializerOptions);

        // act
        var schema = IntrospectionFormatter.Format(result!);

        // assert
        schema.ToString(true).MatchSnapshot();
    }
}
