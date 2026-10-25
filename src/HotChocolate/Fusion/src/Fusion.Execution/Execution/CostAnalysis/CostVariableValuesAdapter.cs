using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.CostAnalysis;

internal sealed class CostVariableValuesAdapter : ICostVariableValues
{
    private IVariableValueCollection _values = null!;

    public void SetValues(IVariableValueCollection values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
    }

    public bool TryGetValue(string name, out IValueNode? value)
        => _values.TryGetValue(name, out value);
}
