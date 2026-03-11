using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi.Serialization;

public class OpenApiModelSettingsSerializerTests
{
    [Fact]
    public void Parse_With_Initialized_Properties_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse(
            """
            {
              "description": "The user's address information"
            }
            """);

        // act
        var settings = OpenApiModelSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Equal("The user's address information", settings.Description);
    }

    [Fact]
    public void Parse_With_Null_Properties_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse(
            """
            {
              "description": null
            }
            """);

        // act
        var settings = OpenApiModelSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Null(settings.Description);
    }

    [Fact]
    public void Parse_Empty_JsonObject_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse("{}");

        // act
        var settings = OpenApiModelSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Null(settings.Description);
    }
}
