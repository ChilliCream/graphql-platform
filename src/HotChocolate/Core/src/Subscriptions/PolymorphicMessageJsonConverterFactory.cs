using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotChocolate.Subscriptions;

[UnconditionalSuppressMessage(
    "Aot",
    "IL3050",
    Justification = "Subscription message serialization is runtime-based by design and already guarded on the public API.")]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "Subscription message serialization is runtime-based by design and already guarded on the public API.")]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2057",
    Justification = "Runtime type names are produced by the serializer and validated to be assignable before use.")]
internal sealed class PolymorphicMessageJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsInterface || typeToConvert.IsAbstract;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(PolymorphicMessageJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class PolymorphicMessageJsonConverter<TValue> : JsonConverter<TValue>
    {
        private const string TypePropertyName = "$type";
        private const string ValuePropertyName = "$value";

        public override TValue? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return default;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object
                || !root.TryGetProperty(TypePropertyName, out var typeProperty)
                || !root.TryGetProperty(ValuePropertyName, out var valueProperty))
            {
                throw new JsonException(
                    $"Polymorphic value for '{typeToConvert.FullName}' must contain '{TypePropertyName}' and '{ValuePropertyName}'.");
            }

            var typeName = typeProperty.GetString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException(
                    $"Polymorphic value for '{typeToConvert.FullName}' did not specify a runtime type.");
            }

            var runtimeType = Type.GetType(typeName, throwOnError: false);
            if (runtimeType is null || !typeToConvert.IsAssignableFrom(runtimeType))
            {
                throw new JsonException(
                    $"Runtime type '{typeName}' is not assignable to '{typeToConvert.FullName}'.");
            }

            if (runtimeType.IsInterface || runtimeType.IsAbstract)
            {
                throw new JsonException(
                    $"Runtime type '{runtimeType.FullName}' must be concrete.");
            }

            var value = JsonSerializer.Deserialize(
                valueProperty.GetRawText(),
                runtimeType,
                options);

            return (TValue?)value;
        }

        public override void Write(
            Utf8JsonWriter writer,
            TValue value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var runtimeType = value.GetType();
            if (runtimeType.IsInterface || runtimeType.IsAbstract)
            {
                throw new JsonException(
                    $"Runtime type '{runtimeType.FullName}' must be concrete.");
            }

            writer.WriteStartObject();
            writer.WriteString(TypePropertyName, runtimeType.AssemblyQualifiedName);
            writer.WritePropertyName(ValuePropertyName);
            JsonSerializer.Serialize(writer, value, runtimeType, options);
            writer.WriteEndObject();
        }
    }
}
