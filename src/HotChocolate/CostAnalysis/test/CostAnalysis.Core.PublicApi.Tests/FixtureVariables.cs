using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Converts a precision fixture's flat <c>variables</c> object (Booleans and
/// integers only, the only shapes this project's fixtures use) into
/// <see cref="IValueNode"/> literals suitable for
/// <see cref="LiteralCostVariableValues"/>.
/// </summary>
public static class FixtureVariables
{
    public static LiteralCostVariableValues Read(JsonElement? variables)
    {
        var values = new Dictionary<string, IValueNode>(StringComparer.Ordinal);

        if (variables is { ValueKind: JsonValueKind.Object } element)
        {
            foreach (var property in element.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.True => BooleanValueNode.True,
                    JsonValueKind.False => BooleanValueNode.False,
                    JsonValueKind.Number => new IntValueNode(property.Value.GetInt32()),
                    _ => throw new NotSupportedException(
                        $"Fixture variable '{property.Name}' has an unsupported JSON kind "
                        + $"'{property.Value.ValueKind}'.")
                };
            }
        }

        return new LiteralCostVariableValues(values);
    }
}
