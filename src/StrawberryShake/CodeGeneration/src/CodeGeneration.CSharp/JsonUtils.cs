using System.Text.Json;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class JsonUtils
{
    public static string GetParseMethod(RuntimeTypeInfo serializationType)
    {
        return serializationType.ToString() switch
        {
            TypeNames.String => nameof(JsonElement.GetString),
            TypeNames.Uri => nameof(JsonElement.GetString),
            TypeNames.Byte => nameof(JsonElement.GetByte),
            TypeNames.ByteArray => nameof(JsonElement.GetBytesFromBase64),
            TypeNames.Int16 => nameof(JsonElement.GetInt16),
            TypeNames.Int32 => nameof(JsonElement.GetInt32),
            TypeNames.Int64 => nameof(JsonElement.GetInt64),
            TypeNames.UInt16 => nameof(JsonElement.GetUInt16),
            TypeNames.UInt32 => nameof(JsonElement.GetUInt32),
            TypeNames.UInt64 => nameof(JsonElement.GetUInt64),
            TypeNames.Single => nameof(JsonElement.GetSingle),
            TypeNames.Double => nameof(JsonElement.GetDouble),
            TypeNames.Decimal => nameof(JsonElement.GetDecimal),
            TypeNames.DateTimeOffset => nameof(JsonElement.GetString),
            TypeNames.DateTime => nameof(JsonElement.GetString),
            TypeNames.TimeSpan => nameof(JsonElement.GetString),
            TypeNames.Boolean => nameof(JsonElement.GetBoolean),
            TypeNames.Guid => nameof(JsonElement.GetGuid),
            _ => throw new NotSupportedException("Serialization format not supported."),
        };
    }

    public static string GetWriteMethod(RuntimeTypeInfo serializationType)
    {
        return serializationType.ToString() switch
        {
            TypeNames.String => nameof(Utf8JsonWriter.WriteString),
            TypeNames.Uri => nameof(Utf8JsonWriter.WriteString),
            TypeNames.Byte => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.ByteArray => nameof(Utf8JsonWriter.WriteBase64String),
            TypeNames.Int16 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Int32 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Int64 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.UInt16 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.UInt32 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.UInt64 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Single => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Double => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Decimal => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.DateTimeOffset => nameof(Utf8JsonWriter.WriteString),
            TypeNames.DateTime => nameof(Utf8JsonWriter.WriteString),
            TypeNames.TimeSpan => nameof(Utf8JsonWriter.WriteString),
            TypeNames.Boolean => nameof(Utf8JsonWriter.WriteBoolean),
            TypeNames.Guid => nameof(Utf8JsonWriter.WriteString),
            _ => throw new NotSupportedException("Serialization format not supported."),
        };
    }
}
