using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// The JSON scalar type represents a JSON node which can be a string,
/// a number a boolean, an array, an object or null.
/// </para>
/// <para>The runtime representation of the JSON scalar is an <see cref="JsonElement"/>.</para>
/// </summary>
public sealed class AnyType : ScalarType<JsonElement>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AnyType"/>.
    /// </summary>
    public AnyType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AnyType"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public AnyType()
        : this(ScalarNames.Any, bind: BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    public override ScalarSerializationType SerializationType
        => ScalarSerializationType.Any;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is
        {
            Kind: SyntaxKind.ObjectValue or
                SyntaxKind.ListValue or
                SyntaxKind.StringValue or
                SyntaxKind.IntValue or
                SyntaxKind.FloatValue or
                SyntaxKind.BooleanValue
        };

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is
            JsonValueKind.Object or
            JsonValueKind.Array or
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.True or
            JsonValueKind.False;

    /// <inheritdoc />
    public override object CoerceInputLiteral(IValueNode valueLiteral)
        => JsonFormatter.Format(valueLiteral);

    /// <inheritdoc />
    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => inputValue.Clone();

    /// <inheritdoc />
    public override void OnCoerceOutputValue(JsonElement runtimeValue, ResultElement resultValue)
    {
        switch (runtimeValue.ValueKind)
        {
            case JsonValueKind.String:
            {
                var value = JsonMarshal.GetRawUtf8Value(runtimeValue);
                resultValue.SetStringValue(value[1..^1]);
                break;
            }

            case JsonValueKind.Number:
            {
                var value = JsonMarshal.GetRawUtf8Value(runtimeValue);
                resultValue.SetNumberValue(value);
                break;
            }

            case JsonValueKind.True:
                resultValue.SetBooleanValue(true);
                break;

            case JsonValueKind.False:
                resultValue.SetBooleanValue(false);
                break;

            case JsonValueKind.Null:
                resultValue.SetNullValue();
                break;

            case JsonValueKind.Array:
            {
                var length = runtimeValue.GetArrayLength();
                resultValue.SetArrayValue(length);

                using var enumerator = runtimeValue.EnumerateArray().GetEnumerator();

                foreach (var element in resultValue.EnumerateArray())
                {
                    enumerator.MoveNext();
                    OnCoerceOutputValue(enumerator.Current, element);
                }
                break;
            }

            case JsonValueKind.Object:
            {
#if NET9_0_OR_GREATER
                var length = runtimeValue.GetPropertyCount();
#else
                var length = runtimeValue.EnumerateObject().Count();
#endif
                resultValue.SetObjectValue(length);

                using var enumerator = runtimeValue.EnumerateObject().GetEnumerator();

                foreach (var property in resultValue.EnumerateObject())
                {
                    enumerator.MoveNext();
                    property.Value.SetPropertyName(enumerator.Current.Name);
                    OnCoerceOutputValue(enumerator.Current.Value, property.Value);
                }
                break;
            }

            default:
                throw Scalar_Cannot_CoerceOutputValue(this, runtimeValue);
        }
    }

    /// <inheritdoc />
    public override IValueNode OnValueToLiteral(JsonElement runtimeValue)
        => JsonParser.Parse(runtimeValue);

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
            var sourceText = JsonMarshal.GetRawUtf8Value(element);
            return Utf8GraphQLParser.Syntax.ParseValueLiteral(sourceText);
        }
    }

    private static class JsonFormatter
    {
        private static readonly JsonFormatterVisitor s_visitor = new();

        public static JsonElement Format(IValueNode node)
        {
            using var bufferWriter = new PooledArrayWriter();
            using var jsonWriter = new Utf8JsonWriter(bufferWriter);
            s_visitor.Visit(node, new JsonFormatterContext(jsonWriter));
            jsonWriter.Flush();

            var jsonReader = new Utf8JsonReader(bufferWriter.WrittenSpan);
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
