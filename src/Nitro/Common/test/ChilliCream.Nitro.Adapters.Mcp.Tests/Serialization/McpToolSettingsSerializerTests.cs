using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.Adapters.Mcp.Serialization;
using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp.Core.Serialization;

public sealed class McpToolSettingsSerializerTests
{
    [Fact]
    public void Parse_FullDocument_ShouldDeserializeTopLevelProperties()
    {
        // arrange
        var document = JsonDocument.Parse(FullJson);

        // act
        var settings = McpToolSettingsSerializer.Parse(document);

        // assert
        Assert.Equal("Get User By ID", settings.Title);
        Assert.NotNull(settings.Icons);
        Assert.NotNull(settings.Annotations);
        Assert.NotNull(settings.View);
        Assert.NotNull(settings.Visibility);
    }

    [Fact]
    public void Parse_FullDocument_ShouldDeserializeIcon()
    {
        // arrange
        var document = JsonDocument.Parse(FullJson);

        // act
        var icon = Assert.Single(McpToolSettingsSerializer.Parse(document).Icons!.Value);

        // assert
        Assert.Equal(new Uri("https://example.com/tool-icon.png"), icon.Source);
        Assert.Equal("image/png", icon.MimeType);
        Assert.Equal(["64x64", "128x128"], icon.Sizes);
        Assert.Equal("dark", icon.Theme);
    }

    [Fact]
    public void Parse_FullDocument_ShouldDeserializeAnnotations()
    {
        // arrange
        var document = JsonDocument.Parse(FullJson);

        // act
        var annotations = McpToolSettingsSerializer.Parse(document).Annotations!;

        // assert
        Assert.False(annotations.DestructiveHint);
        Assert.True(annotations.IdempotentHint);
        Assert.False(annotations.OpenWorldHint);
    }

    [Fact]
    public void Parse_FullDocument_ShouldDeserializeView()
    {
        // arrange
        var document = JsonDocument.Parse(FullJson);

        // act
        var view = McpToolSettingsSerializer.Parse(document).View!;

        // assert
        Assert.Equal("example.com", view.Domain);
        Assert.True(view.PrefersBorder);
        Assert.Equal(["https://example.com"], view.Csp!.BaseUriDomains);
        Assert.True(view.Permissions!.Camera);
        Assert.False(view.Permissions.ClipboardWrite);
    }

    [Fact]
    public void Parse_FullDocument_ShouldDeserializeVisibility()
    {
        // arrange
        var document = JsonDocument.Parse(FullJson);

        // act
        var settings = McpToolSettingsSerializer.Parse(document);

        // assert
        Assert.Equal([McpAppViewVisibility.Model, McpAppViewVisibility.App], settings.Visibility);
    }

    [Fact]
    public void Parse_EmptyJsonObject_ShouldReturnSettingsWithNullProperties()
    {
        // arrange
        var document = JsonDocument.Parse("{}");

        // act
        var settings = McpToolSettingsSerializer.Parse(document);

        // assert
        Assert.Null(settings.Title);
        Assert.Null(settings.Icons);
        Assert.Null(settings.Annotations);
        Assert.Null(settings.View);
        Assert.Null(settings.Visibility);
    }

    [Fact]
    public void Parse_JsonNull_ShouldThrowJsonException()
    {
        // arrange
        var document = JsonDocument.Parse("null");

        // act
        void Act() => McpToolSettingsSerializer.Parse(document);

        // assert
        var exception = Assert.Throws<JsonException>(Act);
        Assert.Equal("Failed to deserialize tool settings.", exception.Message);
    }

    [Fact]
    public void Format_EmptySettings_ShouldOmitNullProperties()
    {
        // arrange
        var settings = new McpToolSettings();

        // act
        using var document = McpToolSettingsSerializer.Format(settings);

        // assert
        ToJson(document).MatchInlineSnapshot("{}");
    }

    [Fact]
    public void Format_AllProperties_ShouldProduceCamelCaseJson()
    {
        // arrange
        var settings = CreateFullSettings();

        // act
        using var document = McpToolSettingsSerializer.Format(settings);

        // assert
        ToJson(document).MatchInlineSnapshot(
            """
            {
              "title": "Get User By ID",
              "icons": [
                {
                  "source": "https://example.com/tool-icon.png",
                  "mimeType": "image/png",
                  "sizes": [
                    "64x64",
                    "128x128"
                  ],
                  "theme": "dark"
                }
              ],
              "annotations": {
                "destructiveHint": false,
                "idempotentHint": true,
                "openWorldHint": false
              },
              "view": {
                "csp": {
                  "baseUriDomains": [
                    "https://example.com"
                  ],
                  "connectDomains": [
                    "https://connect.example.com"
                  ],
                  "frameDomains": [
                    "https://frame.example.com"
                  ],
                  "resourceDomains": [
                    "https://resource.example.com"
                  ]
                },
                "domain": "example.com",
                "permissions": {
                  "camera": true,
                  "clipboardWrite": false,
                  "geolocation": true,
                  "microphone": false
                },
                "prefersBorder": true
              },
              "visibility": [
                "model",
                "app"
              ]
            }
            """);
    }

    [Fact]
    public void Format_OnlyAnnotationsSet_ShouldOmitOtherProperties()
    {
        // arrange
        var settings = new McpToolSettings
        {
            Annotations = new McpToolSettingsAnnotations { IdempotentHint = true }
        };

        // act
        using var document = McpToolSettingsSerializer.Format(settings);

        // assert
        ToJson(document).MatchInlineSnapshot(
            """
            {
              "annotations": {
                "idempotentHint": true
              }
            }
            """);
    }

    [Fact]
    public void RoundTrip_FullSettings_ShouldPreserveAllValues()
    {
        // arrange
        var original = CreateFullSettings();

        // act
        using var document = McpToolSettingsSerializer.Format(original);
        var roundTripped = McpToolSettingsSerializer.Parse(document);

        // assert
        Assert.Equivalent(original, roundTripped, strict: true);
    }

    private static McpToolSettings CreateFullSettings()
    {
        return new McpToolSettings
        {
            Title = "Get User By ID",
            Icons =
            [
                new McpToolSettingsIcon
                {
                    Source = new Uri("https://example.com/tool-icon.png"),
                    MimeType = "image/png",
                    Sizes = ["64x64", "128x128"],
                    Theme = "dark"
                }
            ],
            Annotations = new McpToolSettingsAnnotations
            {
                DestructiveHint = false,
                IdempotentHint = true,
                OpenWorldHint = false
            },
            View = new McpToolSettingsMcpAppView
            {
                Csp = new McpToolSettingsCsp
                {
                    BaseUriDomains = ["https://example.com"],
                    ConnectDomains = ["https://connect.example.com"],
                    FrameDomains = ["https://frame.example.com"],
                    ResourceDomains = ["https://resource.example.com"]
                },
                Domain = "example.com",
                Permissions = new McpToolSettingsPermissions
                {
                    Camera = true,
                    ClipboardWrite = false,
                    Geolocation = true,
                    Microphone = false
                },
                PrefersBorder = true
            },
            Visibility = [McpAppViewVisibility.Model, McpAppViewVisibility.App]
        };
    }

    private static string ToJson(JsonDocument document)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            document.WriteTo(writer);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private const string FullJson =
        """
        {
          "title": "Get User By ID",
          "icons": [
            {
              "source": "https://example.com/tool-icon.png",
              "mimeType": "image/png",
              "sizes": ["64x64", "128x128"],
              "theme": "dark"
            }
          ],
          "annotations": {
            "destructiveHint": false,
            "idempotentHint": true,
            "openWorldHint": false
          },
          "view": {
            "csp": {
              "baseUriDomains": ["https://example.com"],
              "connectDomains": ["https://connect.example.com"],
              "frameDomains": ["https://frame.example.com"],
              "resourceDomains": ["https://resource.example.com"]
            },
            "domain": "example.com",
            "permissions": {
              "camera": true,
              "clipboardWrite": false,
              "geolocation": true,
              "microphone": false
            },
            "prefersBorder": true
          },
          "visibility": ["model", "app"]
        }
        """;
}
