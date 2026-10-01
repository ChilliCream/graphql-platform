using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class CostVariableValuesAdapter(IVariableValueCollection values) : ICostVariableValues
{
    public bool TryGetValue(string name, out IValueNode? value)
        => values.TryGetValue(name, out value);
}
