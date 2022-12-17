using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

internal readonly struct JsonResult
{
    public JsonResult(string schemaName, JsonElement element)
    {
        SchemaName = schemaName;
        Element = element;
    }

    public string SchemaName { get; }

    public JsonElement Element { get; }
}
