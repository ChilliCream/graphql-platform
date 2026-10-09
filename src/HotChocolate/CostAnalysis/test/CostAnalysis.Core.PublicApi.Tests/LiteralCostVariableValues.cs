using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Provides coerced variable values from GraphQL literals.
/// </summary>
public sealed class LiteralCostVariableValues(IReadOnlyDictionary<string, IValueNode> values)
    : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
        => values.TryGetValue(name, out value);
}
