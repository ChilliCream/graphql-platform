using System.Text.Encodings.Web;
using System.Text.Json;

namespace HotChocolate.Text.Json;

internal static class JsonSerializerOptionDefaults
{
    public static JsonSerializerOptions GraphQL { get; }
        = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

    public static JsonSerializerOptions GraphQLIndented { get; }
        = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
}
