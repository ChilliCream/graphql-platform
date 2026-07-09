using System.Text.Json;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Language;

public static class ValueJsonFormatter
{
    private static readonly JsonFormatterVisitor s_visitor = new();

    public static void Format(Utf8JsonWriter writer, IValueNode node)
        => s_visitor.Visit(node, new JsonFormatterContext(writer));

    private sealed class JsonFormatterVisitor : SyntaxWalker<JsonFormatterContext>
    {
        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            JsonFormatterContext context)
        {
            context.Writer.WriteStartObject();
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            JsonFormatterContext context)
        {
            context.Writer.WriteEndObject();
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            JsonFormatterContext context)
        {
            context.Writer.WriteStartArray();
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            JsonFormatterContext context)
        {
            context.Writer.WriteEndArray();
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            JsonFormatterContext context)
        {
            context.Writer.WritePropertyName(node.Name.Value);
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            IValueNode node,
            JsonFormatterContext context)
        {
            switch (node)
            {
                case EnumValueNode value:
                    context.Writer.WriteStringValue(value.Value);
                    break;

                case FloatValueNode value:
                    context.Writer.WriteRawValue(value.AsSpan(), true);
                    break;

                case IntValueNode value:
                    context.Writer.WriteRawValue(value.AsSpan(), true);
                    break;

                case BooleanValueNode value:
                    context.Writer.WriteBooleanValue(value.Value);
                    break;

                case StringValueNode value:
                    context.Writer.WriteStringValue(value.Value);
                    break;

                case NullValueNode:
                    context.Writer.WriteNullValue();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }

            return base.Enter(node, context);
        }
    }

    private sealed class JsonFormatterContext(Utf8JsonWriter writer)
    {
        public Utf8JsonWriter Writer { get; } = writer;
    }
}
