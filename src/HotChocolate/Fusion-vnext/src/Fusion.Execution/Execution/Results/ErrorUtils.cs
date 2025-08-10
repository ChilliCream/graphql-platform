using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorUtils
{
    public static ErrorBuilder? CreateErrorBuilder(JsonElement jsonError)
    {
        if (jsonError.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        if (jsonError.TryGetProperty("message", out var message)
            && message.ValueKind is JsonValueKind.String)
        {
            var errorBuilder = ErrorBuilder.New()
                .SetMessage(message.GetString()!);

            if (jsonError.TryGetProperty("code", out var code)
                && code.ValueKind is JsonValueKind.String)
            {
                errorBuilder.SetCode(code.GetString());
            }

            if (jsonError.TryGetProperty("extensions", out var extensions)
                && extensions.ValueKind is JsonValueKind.Object)
            {
                foreach (var property in extensions.EnumerateObject())
                {
                    errorBuilder.SetExtension(property.Name, property.Value);
                }
            }

            return errorBuilder;
        }

        return null;
    }
}
