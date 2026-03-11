using System.Text.Json;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class JsonUtils
{
    public static string GetParseMethod(RuntimeTypeInfo serializationType)
    {
        return serializationType.ToString() switch
        {
            TypeNames.Boolean => nameof(JsonElement.GetBoolean),
            TypeNames.Byte => nameof(JsonElement.GetByte),
            TypeNames.ByteArray => nameof(JsonElement.GetBytesFromBase64),
            TypeNames.DateOnly => nameof(JsonElement.GetString),
            TypeNames.DateTime => nameof(JsonElement.GetString),
            TypeNames.DateTimeOffset => nameof(JsonElement.GetString),
            TypeNames.Decimal => nameof(JsonElement.GetDecimal),
            TypeNames.Double => nameof(JsonElement.GetDouble),
            TypeNames.Guid => nameof(JsonElement.GetGuid),
            TypeNames.Int16 => nameof(JsonElement.GetInt16),
            TypeNames.Int32 => nameof(JsonElement.GetInt32),
            TypeNames.Int64 => nameof(JsonElement.GetInt64),
            TypeNames.SByte => nameof(JsonElement.GetSByte),
            TypeNames.Single => nameof(JsonElement.GetSingle),
            TypeNames.String or TypeNames.TimeOnly or TypeNames.TimeSpan => nameof(JsonElement.GetString),
            TypeNames.UInt16 => nameof(JsonElement.GetUInt16),
            TypeNames.UInt32 => nameof(JsonElement.GetUInt32),
            TypeNames.UInt64 => nameof(JsonElement.GetUInt64),
            TypeNames.Uri => nameof(JsonElement.GetString),
            _ => throw new NotSupportedException("Serialization format not supported.")
        };
    }

    public static string GetWriteMethod(RuntimeTypeInfo serializationType)
    {
        return serializationType.ToString() switch
        {
            TypeNames.Boolean => nameof(Utf8JsonWriter.WriteBoolean),
            TypeNames.Byte => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.ByteArray => nameof(Utf8JsonWriter.WriteBase64String),
            TypeNames.DateOnly => nameof(Utf8JsonWriter.WriteString),
            TypeNames.DateTime => nameof(Utf8JsonWriter.WriteString),
            TypeNames.DateTimeOffset => nameof(Utf8JsonWriter.WriteString),
            TypeNames.Decimal or TypeNames.Double => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Guid => nameof(Utf8JsonWriter.WriteString),
            TypeNames.Int16 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Int32 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Int64 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.SByte => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Single => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.String or TypeNames.TimeOnly or TypeNames.TimeSpan => nameof(Utf8JsonWriter.WriteString),
            TypeNames.UInt16 or TypeNames.UInt32 or TypeNames.UInt64 => nameof(Utf8JsonWriter.WriteNumber),
            TypeNames.Uri => nameof(Utf8JsonWriter.WriteString),
            _ => throw new NotSupportedException("Serialization format not supported.")
        };
    }
}
