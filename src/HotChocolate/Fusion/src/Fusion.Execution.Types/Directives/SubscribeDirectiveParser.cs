using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class SubscribeDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals(FusionBuiltIns.Subscribe, StringComparison.Ordinal);

    public static SubscribeDirective Parse(DirectiveNode directive)
    {
        string? schemaKey = null;
        var topics = ImmutableArray<string>.Empty;
        string? broker = null;
        SelectionSetNode? message = null;

        foreach (var argument in directive.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaKey = ((EnumValueNode)argument.Value).Value;
                    break;

                case "topics":
                    topics = ParseTopics((ListValueNode)argument.Value);
                    break;

                case "broker":
                    broker = ((StringValueNode)argument.Value).Value;
                    break;

                case "message":
                    message = FieldDirectiveParser.ParseSelectionSet(
                        ((StringValueNode)argument.Value).Value);
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @subscribe.");
            }
        }

        if (string.IsNullOrEmpty(schemaKey))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @subscribe directive.");
        }

        if (message is null)
        {
            throw new DirectiveParserException(
                "The `message` argument is required on the @subscribe directive.");
        }

        return new SubscribeDirective(topics, broker, message)
        {
            SchemaKey = new SchemaKey(schemaKey)
        };
    }

    private static ImmutableArray<string> ParseTopics(ListValueNode value)
    {
        if (value.Items.Count == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<string>(value.Items.Count);

        foreach (var item in value.Items)
        {
            builder.Add(((StringValueNode)item).Value);
        }

        return builder.ToImmutable();
    }
}
