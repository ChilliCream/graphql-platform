using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi.Serialization;

public class OpenApiEndpointSettingsSerializerTests
{
    [Fact]
    public void Parse_With_Initialized_Properties_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse(
            """
            {
              "description": "Fetches user posts",
              "routeParameters": [
                {
                  "key": "userId",
                  "variableName":"userId"
                }],
              "queryParameters": [
                {
                  "key": "limit",
                  "variableName": "limit"
                },
                {
                  "key": "offset",
                  "variableName": "offset"
                }],
              "bodyVariableName": "input"
            }
            """);

        // act
        var settings = OpenApiEndpointSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Equal("Fetches user posts", settings.Description);

        var routeParam = Assert.Single(settings.RouteParameters);
        Assert.Equal("userId", routeParam.Key);
        Assert.Equal("userId", routeParam.VariableName);

        Assert.Equal(2, settings.QueryParameters.Length);
        Assert.Contains(settings.QueryParameters, p => p.Key == "limit");
        Assert.Contains(settings.QueryParameters, p => p.Key == "offset");

        Assert.Equal("input", settings.BodyVariableName);
    }

    [Fact]
    public void Parse_With_Null_Properties_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse(
            """
            {
              "description": null,
              "routeParameters": [
                {
                  "key": "userId",
                  "variableName":"userId"
                }],
             "queryParameters": [
                {
                  "key": "limit",
                  "variableName": "limit"
                },
                {
                  "key": "offset",
                  "variableName": "offset"
                }],
              "bodyVariableName": null
            }
            """);

        // act
        var settings = OpenApiEndpointSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Null(settings.Description);

        var routeParam = Assert.Single(settings.RouteParameters);
        Assert.Equal("userId", routeParam.Key);
        Assert.Equal("userId", routeParam.VariableName);

        Assert.Equal(2, settings.QueryParameters.Length);
        Assert.Contains(settings.QueryParameters, p => p.Key == "limit");
        Assert.Contains(settings.QueryParameters, p => p.Key == "offset");

        Assert.Null(settings.BodyVariableName);
    }

    [Fact]
    public void Parse_Empty_JsonObject_Succeeds()
    {
        // arrange
        var jsonDocument = JsonDocument.Parse("{}");

        // act
        var settings = OpenApiEndpointSettingsSerializer.Parse(jsonDocument);

        // assert
        Assert.Null(settings.Description);
        Assert.Empty(settings.RouteParameters);
        Assert.Empty(settings.QueryParameters);
        Assert.Null(settings.BodyVariableName);
    }
}
