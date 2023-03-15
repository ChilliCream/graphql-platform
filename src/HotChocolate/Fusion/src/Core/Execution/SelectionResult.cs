using System.Text.Json;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Execution;

internal readonly struct SelectionResult
{
    public SelectionResult(JsonResult single)
    {
        Single = single;
        Multiple = null;
        HasValue = true;
    }

    private SelectionResult(IReadOnlyList<JsonResult> multiple)
    {
        Single = default;
        Multiple = multiple;
        HasValue = true;
    }

    public bool HasValue { get; }

    public JsonResult Single { get; }

    public IReadOnlyList<JsonResult>? Multiple { get; }

    public TypeInfo GetTypeInfo()
    {
        var result = Multiple is null ? Single : Multiple[0];

        return new TypeInfo(
            result.SubgraphName,
            result.Element.GetProperty("__typename").GetString()!);
    }

    public bool IsNull()
    {
        if (Multiple is null)
        {
            return Single.Element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
        }

        for (var i = 0; i < Multiple.Count; i++)
        {
            if (Multiple[i].Element.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                return false;
            }
        }

        return true;

    }

    public SelectionResult AddResult(JsonResult result)
    {
        if (Multiple is null)
        {
            return new SelectionResult(new[] { Single, result });
        }

        var array = new JsonResult[Multiple.Count + 1];

        for (var i = 0; i < Multiple.Count; i++)
        {
            array[i] = Multiple[i];
        }

        array[Multiple.Count] = result;
        return new SelectionResult(array);
    }
}
