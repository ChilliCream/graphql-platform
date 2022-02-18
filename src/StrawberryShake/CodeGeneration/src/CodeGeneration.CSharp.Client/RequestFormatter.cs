using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class RequestFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Format(GeneratorRequest request)
    {
        var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var buffer = JsonSerializer.SerializeToUtf8Bytes(request, _options);
        File.WriteAllBytes(fileName, buffer);
        return fileName;
    }
}
