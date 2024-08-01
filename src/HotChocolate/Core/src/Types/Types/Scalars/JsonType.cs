#nullable enable
using System.Buffers;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// The JSON scalar type represents a JSON node which can be a string,
/// a number a boolean, an array, an object or null.
///
/// The runtime representation of the JSON scalar is an <see cref="JsonElement"/>.
/// </summary>
public sealed class JsonType : ScalarType<JsonElement>
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonType"/>.
    /// </summary>
    public JsonType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind) { }

    /// <summary>
    /// Initializes a new instance of <see cref="JsonType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public JsonType()
        : base(ScalarNames.JSON, BindingBehavior.Implicit) { }

    /// <summary>
    /// Defines if the specified <paramref name="valueSyntax"/> can be handled by the JSON scalar.
    /// </summary>
    /// <param name="valueSyntax">
    /// The GraphQL value syntax that shall be evaluated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="valueSyntax"/> can be handled
    /// by the JSON scalar; otherwise <c>false</c>.
    /// </returns>
    public override bool IsInstanceOfType(IValueNode valueSyntax)
        => true;

    /// <summary>
    /// Parses the specified GraphQL value syntax into a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="valueSyntax">
    /// The GraphQL value syntax that shall be parsed.
    /// </param>
    /// <returns>
    /// Returns <c>null</c> or a <see cref="JsonElement"/>.
    /// </returns>
    public override object ParseLiteral(IValueNode valueSyntax)
        => JsonFormatter.Format(valueSyntax);

    /// <summary>
    /// Parses the runtime value into GraphQL value syntax.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value.
    /// </param>
    /// <returns>
    /// Returns GraphQL value syntax.
    /// </returns>
    public override IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is JsonElement element)
        {
            return JsonParser.Parse(element);
        }

        throw CreateParseValueError(runtimeValue);
    }

    /// <inheritdoc cref="ScalarType.ParseResult"/>
    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);

    private SerializationException CreateParseValueError(object runtimeValue)
        => new(TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()), this);

    private static class JsonParser
    {
        public static IValueNode Parse(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return ParseObject(element);

                case JsonValueKind.Array:
                    return ParseList(element);

                case JsonValueKind.String:
                    return ParseString(element);

                case JsonValueKind.Number:
                    return ParseNumber(element);

                case JsonValueKind.True:
                    return new BooleanValueNode(true);

                case JsonValueKind.False:
                    return new BooleanValueNode(false);

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return NullValueNode.Default;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ObjectValueNode ParseObject(JsonElement element)
        {
            var properties = new List<ObjectFieldNode>();

            foreach (var property in element.EnumerateObject())
            {
                properties.Add(ParseField(property));
            }

            return new ObjectValueNode(properties);
        }

        private static ListValueNode ParseList(JsonElement element)
        {
            var properties = new List<IValueNode>();

            foreach (var item in element.EnumerateArray())
            {
                properties.Add(Parse(item));
            }

            return new ListValueNode(properties);
        }

        private static ObjectFieldNode ParseField(JsonProperty property)
            => new(property.Name, Parse(property.Value));

        private static StringValueNode ParseString(JsonElement element)
            => new(element.GetString()!);

        private static IValueNode ParseNumber(JsonElement element)
        {
            var text = element.GetRawText();
            var length = checked(text.Length * 4);
            byte[]? source = null;

            var sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);
            Utf8GraphQLParser.ConvertToBytes(text, ref sourceSpan);

            var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(sourceSpan);

            if (source is not null)
            {
                ArrayPool<byte>.Shared.Return(source);
            }

            return value;
        }
    }

    private static class JsonFormatter
    {
        private static readonly JsonFormatterVisitor _visitor = new();

        public static JsonElement Format(IValueNode node)
        {
            using var bufferWriter = new ArrayWriter();
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);
            _visitor.Visit(node, new JsonFormatterContext(jsonWriter));
            jsonWriter.Flush();

            var jsonReader = new Utf8JsonReader(bufferWriter.GetWrittenSpan());
            return JsonElement.ParseValue(ref jsonReader);
        }

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
}
