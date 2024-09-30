using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrawberryShake.Tools;

public class JsonConsoleOutputCommand  : IDisposable
{
    private readonly JsonConsoleOutputData _data;

    public JsonConsoleOutputCommand(JsonConsoleOutputData data)
    {
        _data = data;
    }

    public void Dispose()
    {
        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = false,
        });
        Console.WriteLine(json);
    }
}
