using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed class VariableValueCollection(
    Dictionary<string, VariableValue> coercedValues)
    : IVariableValueCollection
{
    public static VariableValueCollection Empty { get; } = new([]);

    public bool IsEmpty => coercedValues.Count == 0;

    public T GetValue<T>(string name) where T : IValueNode
    {
        if (TryGetValue(name, out T? value))
        {
            return value;
        }

        if (coercedValues.ContainsKey(name))
        {
            throw ThrowHelper.VariableNotOfType(name, typeof(T));
        }

        throw ThrowHelper.VariableNotFound(name);
    }

    public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
    {
        if (coercedValues.TryGetValue(name, out var variableValue)
            && variableValue.ValueLiteral is T casted)
        {
            value = casted;
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerator<Execution.VariableValue> GetEnumerator()
    {
        foreach (var (_, value) in coercedValues)
        {
            yield return new Execution.VariableValue(value.Name, value.Type, value.ValueLiteral);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
