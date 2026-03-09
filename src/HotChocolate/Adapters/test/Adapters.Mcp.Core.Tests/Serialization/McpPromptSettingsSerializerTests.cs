using System.Text.Json;
using HotChocolate.Adapters.Mcp.Serialization;

namespace HotChocolate.Adapters.Mcp.Core.Serialization;

public sealed class McpPromptSettingsSerializerTests
{
    [Fact]
    public void Parse_WithPolymorphicMessages_ShouldSucceed()
    {
        // arrange
        var document =
            JsonDocument.Parse(
                """
                {
                    "messages": [
                        {
                            "role": "user",
                            "content": {
                                "text": "Get the person",
                                "type": "text"
                            }
                        }
                    ]
                }
                """);

        // act
        var settings = McpPromptSettingsSerializer.Parse(document);

        // assert
        var message = Assert.Single(settings.Messages);
        Assert.Equal("user", message.Role);
        var content = Assert.IsType<McpPromptSettingsTextContentDto>(message.Content);
        Assert.Equal("Get the person", content.Text);
    }
}
