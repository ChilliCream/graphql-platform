using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis.Fuzz;

internal sealed class JsonCostVariableValues(JsonElement values) : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
    {
        if (values.TryGetProperty(name, out var element))
        {
            value = ToValueNode(element);
            return true;
        }

        value = null;
        return false;
    }

    private static IValueNode ToValueNode(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Null => NullValueNode.Default,
            JsonValueKind.True => BooleanValueNode.True,
            JsonValueKind.False => BooleanValueNode.False,
            JsonValueKind.Number when value.TryGetInt32(out var number) => new IntValueNode(number),
            JsonValueKind.String => new StringValueNode(value.GetString()!),
            JsonValueKind.Array => new ListValueNode(value.EnumerateArray().Select(ToValueNode).ToArray()),
            JsonValueKind.Object => new ObjectValueNode(
                value.EnumerateObject()
                    .Select(property => new ObjectFieldNode(property.Name, ToValueNode(property.Value)))
                    .ToArray()),
            _ => throw ThrowHelper.InvalidOperation(
                $"Unsupported generated variable value {value.ValueKind}.")
        };
}
