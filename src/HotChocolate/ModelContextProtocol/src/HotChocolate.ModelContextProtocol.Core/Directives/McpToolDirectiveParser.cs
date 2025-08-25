using HotChocolate.Language;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Directives;

internal static class McpToolDirectiveParser
{
    public static McpToolDirective Parse(DirectiveNode directive)
    {
        string? title = null;
        bool? destructiveHint = null;
        bool? idempotentHint = null;
        bool? openWorldHint = null;

        foreach (var argument in directive.Arguments)
        {
            switch (argument.Name.Value)
            {
                case WellKnownArgumentNames.Title:
                    if (argument.Value is StringValueNode titleString)
                    {
                        title = titleString.Value;
                    }

                    break;

                case WellKnownArgumentNames.DestructiveHint:
                    if (argument.Value is BooleanValueNode destructiveHintBoolean)
                    {
                        destructiveHint = destructiveHintBoolean.Value;
                    }

                    break;

                case WellKnownArgumentNames.IdempotentHint:
                    if (argument.Value is BooleanValueNode idempotentHintBoolean)
                    {
                        idempotentHint = idempotentHintBoolean.Value;
                    }

                    break;

                case WellKnownArgumentNames.OpenWorldHint:
                    if (argument.Value is BooleanValueNode openWorldHintBoolean)
                    {
                        openWorldHint = openWorldHintBoolean.Value;
                    }

                    break;

                default:
                    throw new Exception(
                        string.Format(
                            McpToolDirectiveParser_ArgumentNotSupportedOnMcpToolDirective,
                            argument.Name.Value));
            }
        }

        return new McpToolDirective()
        {
            Title = title,
            DestructiveHint = destructiveHint,
            IdempotentHint = idempotentHint,
            OpenWorldHint = openWorldHint
        };
    }
}
