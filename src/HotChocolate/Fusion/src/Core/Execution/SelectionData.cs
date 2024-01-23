using HotChocolate.Fusion.Metadata;
using static System.Text.Json.JsonValueKind;

namespace HotChocolate.Fusion.Execution;

internal readonly struct SelectionData
{
    public SelectionData(JsonResult single)
    {
        Single = single;
        Multiple = null;
        HasValue = true;
    }

    private SelectionData(JsonResult[] multiple)
    {
        Single = default;
        Multiple = multiple;
        HasValue = true;
    }

    public bool HasValue { get; }

    public JsonResult Single { get; }

    public JsonResult[]? Multiple { get; }

    public QualifiedTypeName GetTypeName()
    {
        var result = Multiple is null ? Single : Multiple[0];

        return new QualifiedTypeName(
            result.SubgraphName,
            result.Element.GetProperty("__typename").GetString()!);
    }

    public bool IsNull()
    {
        if (Multiple is null)
        {
            return Single.Element.ValueKind is Null or Undefined;
        }

        for (var i = 0; i < Multiple.Length; i++)
        {
            if (Multiple[i].Element.ValueKind is not Null and not Undefined)
            {
                return false;
            }
        }

        return true;
    }

    public SelectionData AddResult(JsonResult result)
    {
        if (HasValue is false)
        {
            return new SelectionData(result);
        }

        if (Multiple is null)
        {
            return new SelectionData([Single, result,]);
        }

        var array = new JsonResult[Multiple.Length + 1];

        for (var i = 0; i < Multiple.Length; i++)
        {
            array[i] = Multiple[i];
        }

        array[Multiple.Length] = result;
        return new SelectionData(array);
    }

    public static SelectionData Empty { get; } = new();
}
