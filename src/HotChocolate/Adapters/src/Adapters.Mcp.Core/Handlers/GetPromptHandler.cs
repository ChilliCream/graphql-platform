using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Handlers;

internal static partial class GetPromptHandler
{
    public static GetPromptResult Handle(RequestContext<GetPromptRequestParams> context)
    {
        var registry = context.Services!.GetRequiredService<McpFeatureRegistry>();

        if (!registry.TryGetPrompt(context.Params!.Name, out var prompt))
        {
            throw new McpProtocolException(
                string.Format(GetPromptHandler_PromptNotFound, context.Params.Name),
                McpErrorCode.InvalidParams)
            {
                Data =
                {
                    { "name", context.Params!.Name }
                }
            };
        }

        // Validate required arguments.
        if (prompt.Value.Item1.Arguments is { } arguments)
        {
            var missingRequiredArguments = new List<string>();

            foreach (var argument in arguments.Where(a => a.Required is true))
            {
                if (context.Params.Arguments?.ContainsKey(argument.Name) == false)
                {
                    missingRequiredArguments.Add(argument.Name);
                }
            }

            if (missingRequiredArguments.Count != 0)
            {
                throw new McpProtocolException(
                    string.Format(
                        GetPromptHandler_MissingRequiredArguments,
                        string.Join(", ", missingRequiredArguments)),
                    McpErrorCode.InvalidParams)
                {
                    Data =
                    {
                        { "missingRequiredArguments", missingRequiredArguments }
                    }
                };
            }
        }

        return new GetPromptResult
        {
            Description = prompt.Value.Item1.Description,
            Messages = PrepareMessages(prompt.Value.Item2, context.Params.Arguments)
        };
    }

    private static List<PromptMessage> PrepareMessages(
        ImmutableArray<PromptMessage> messages,
        IDictionary<string, JsonElement>? arguments)
    {
        arguments ??= ImmutableDictionary<string, JsonElement>.Empty;

        ContentBlock FormatContentBlock(ContentBlock contentBlock)
        {
            if (contentBlock is not TextContentBlock textContentBlock)
            {
                return contentBlock;
            }

            return new TextContentBlock { Text = Format(textContentBlock.Text, arguments) };
        }

        return messages.Select(m => new PromptMessage
        {
            Role = m.Role,
            Content = FormatContentBlock(m.Content)
        }).ToList();
    }

    private static string Format(string content, IDictionary<string, JsonElement> arguments)
    {
        return PlaceholderRegex().Replace(
            content,
            match =>
            {
                var key = match.Groups[1].Value;
                return arguments.TryGetValue(key, out var value) ? value.ToString() : match.Value;
            });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderRegex();
}
