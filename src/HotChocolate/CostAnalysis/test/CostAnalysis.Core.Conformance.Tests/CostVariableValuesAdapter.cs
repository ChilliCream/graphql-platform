using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal sealed class CostVariableValuesAdapter(
    IReadOnlyDictionary<string, VariableValue> values) : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
    {
        if (values.TryGetValue(name, out var variable))
        {
            value = variable.Value;
            return true;
        }

        value = null;
        return false;
    }
}
