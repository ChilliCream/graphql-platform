using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public JsonElement Error { get; set; }

    public JsonElement? GetFirstError()
    {
        return null;
    }
}
