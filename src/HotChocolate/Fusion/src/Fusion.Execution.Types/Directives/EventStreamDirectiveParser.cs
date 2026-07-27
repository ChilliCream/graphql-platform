using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class EventStreamDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals(FusionBuiltIns.EventStream, StringComparison.Ordinal);

    public static EventStreamDirective Parse(DirectiveNode directive)
    {
        string? schemaKey = null;
        var topics = ImmutableArray<string>.Empty;
        string? broker = null;
        SelectionSetNode? message = null;
        string? cursorField = null;
        string? cursorArgument = null;

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

                case "cursorField":
                    cursorField = ((StringValueNode)argument.Value).Value;
                    break;

                case "cursorArgument":
                    cursorArgument = ((StringValueNode)argument.Value).Value;
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @eventStream.");
            }
        }

        if (string.IsNullOrEmpty(schemaKey))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @eventStream directive.");
        }

        if (message is null)
        {
            throw new DirectiveParserException(
                "The `message` argument is required on the @eventStream directive.");
        }

        return new EventStreamDirective(topics, broker, message, cursorField, cursorArgument)
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
