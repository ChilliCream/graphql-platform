using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Storage;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp;

internal static class PromptFactory
{
    public static (Prompt, ImmutableArray<PromptMessage>) CreatePrompt(PromptDefinition promptDefinition)
    {
        var prompt = new Prompt
        {
            Name = promptDefinition.Name,
            Title = promptDefinition.Title,
            Description = promptDefinition.Description,
            Arguments = promptDefinition.Arguments?.Select(
                a => new PromptArgument
                {
                    Name = a.Name,
                    Title = a.Title,
                    Description = a.Description,
                    Required = a.Required
                }).ToList(),
            Icons = promptDefinition.Icons?.Select(
                i => new Icon
                {
                    Source = i.Source.OriginalString,
                    MimeType = i.MimeType,
                    Sizes = i.Sizes,
                    Theme = i.Theme
                }).ToList()
        };

        var messages = promptDefinition.Messages
            .Select(m => new PromptMessage
            {
                Role = MapRole(m.Role),
                Content = MapContent(m.Content)
            })
            .ToImmutableArray();

        return (prompt, messages);
    }

    private static Role MapRole(RoleDefinition roleDefinition)
    {
        return roleDefinition switch
        {
            RoleDefinition.User => Role.User,
            RoleDefinition.Assistant => Role.Assistant,
            _ => throw new ArgumentOutOfRangeException(nameof(roleDefinition), roleDefinition, null)
        };
    }

    private static TextContentBlock MapContent(IContentBlockDefinition contentBlockDefinition)
    {
        return contentBlockDefinition switch
        {
            TextContentBlockDefinition textContentBlock => new TextContentBlock
            {
                Text = textContentBlock.Text
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(contentBlockDefinition),
                contentBlockDefinition,
                null)
        };
    }
}
