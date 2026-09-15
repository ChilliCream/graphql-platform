using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A minimal <see cref="ICostVariableValues"/> built directly from literal
/// values, the way a consumer without access to a full request execution
/// pipeline would supply coerced variables.
/// </summary>
public sealed class LiteralCostVariableValues(IReadOnlyDictionary<string, IValueNode> values)
    : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
        => values.TryGetValue(name, out value);
}
