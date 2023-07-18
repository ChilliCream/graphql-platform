using System.Text.Json;

namespace HotChocolate.Transport.Serialization;

public static class JsonDefaults
{
    public static JsonWriterOptions WriterOptions { get; } =
        new() { Indented = false };

#if NET6_0_OR_GREATER
    public static JsonSerializerOptions SerializerOptions { get; } =
        new(JsonSerializerDefaults.Web);
#else
    public static JsonSerializerOptions SerializerOptions { get; } =
        new()
        {
            PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET6_0_OR_GREATER
            NumberHandling = JsonNumberHandling.AllowReadingFromString
#endif
        };
#endif
}
