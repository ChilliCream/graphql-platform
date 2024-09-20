using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

internal readonly struct JsonResult(string subgraphName, JsonElement element)
{
    public string SubgraphName { get; } = subgraphName;

    public JsonElement Element { get; } = element;
}
