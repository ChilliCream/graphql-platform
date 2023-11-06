using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

internal readonly struct JsonResult
{
    public JsonResult(string subgraphName, JsonElement element)
    {
        SubgraphName = subgraphName;
        Element = element;
    }

    public string SubgraphName { get; }

    public JsonElement Element { get; }
}
