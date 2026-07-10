using System.Collections.Immutable;
using ChilliCream.Nitro.Adapters.Mcp.Serialization;
using HotChocolate.Adapters.Mcp.Storage;

namespace ChilliCream.Nitro.Adapters.Mcp.Extensions;

public static class PromptDefinitionExtensions
{
    extension(PromptDefinition promptDefinition)
    {
        public static PromptDefinition Create(string name, McpPromptSettings settings)
        {
            return new PromptDefinition(name)
            {
                Title = settings.Title,
                Description = settings.Description,
                Arguments = settings.Arguments?.Select(
                    a => new PromptArgumentDefinition(a.Name)
                    {
                        Title = a.Title,
                        Description = a.Description,
                        Required = a.Required
                    }).ToImmutableArray(),
                Icons = settings.Icons?.Select(
                    i => new IconDefinition(i.Source)
                    {
                        MimeType = i.MimeType,
                        Sizes = i.Sizes,
                        Theme = i.Theme
                    }).ToImmutableArray(),
                Messages = settings.Messages.Select(
                    m =>
                    {
                        return m.Content switch
                        {
                            McpPromptSettingsTextContent content => new PromptMessageDefinition(
                                MapRole(m.Role),
                                new TextContentBlockDefinition(content.Text)),
                            _ =>
                                throw new NotSupportedException(
                                    $"Message content type '{m.Content.GetType().Name}' is not supported.")
                        };
                    }).ToImmutableArray()
            };
        }
    }

    private static RoleDefinition MapRole(string role)
    {
        return role switch
        {
            "user" => RoleDefinition.User,
            "assistant" => RoleDefinition.Assistant,
            _ => throw new NotSupportedException($"Role '{role}' is not supported.")
        };
    }
}
