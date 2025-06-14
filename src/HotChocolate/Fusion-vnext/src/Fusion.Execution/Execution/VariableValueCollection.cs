using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class VariableValueCollection : IVariableValueCollection
{
    private readonly Dictionary<string, VariableValue> _coercedValues;

    public VariableValueCollection(Dictionary<string, VariableValue> coercedValues)
    {
        ArgumentNullException.ThrowIfNull(coercedValues);

        _coercedValues = coercedValues;
    }

    public static VariableValueCollection Empty { get; } = new([]);

    public T GetValue<T>(string name) where T : IValueNode
    {
        if (TryGetValue(name, out T? value))
        {
            return value;
        }

        if (_coercedValues.ContainsKey(name))
        {
            throw ExceptionHelper.VariableNotOfType(name, typeof(T));
        }

        throw ExceptionHelper.VariableNotFound(name);
    }

    public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
    {
        if (_coercedValues.TryGetValue(name, out var variableValue)
            && variableValue.Value is T casted)
        {
            value = casted;
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerator<VariableValue> GetEnumerator()
        => _coercedValues.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
