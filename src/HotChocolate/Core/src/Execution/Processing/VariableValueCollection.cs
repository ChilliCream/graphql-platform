using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal class VariableValueCollection : IVariableValueCollection
{
    private readonly Dictionary<string, VariableValueOrLiteral> _coercedValues;

    public VariableValueCollection(Dictionary<string, VariableValueOrLiteral> coercedValues)
    {
        _coercedValues = coercedValues;
    }

    public static VariableValueCollection Empty { get; } =
        new(new Dictionary<string, VariableValueOrLiteral>());

    public T? GetVariable<T>(NameString name)
    {
        if (TryGetVariable(name, out T? value))
        {
            return value;
        }

        if (_coercedValues.ContainsKey(name))
        {
            throw ThrowHelper.VariableNotOfType(name, typeof(T));
        }

        throw ThrowHelper.VariableNotFound(name);
    }

    public bool TryGetVariable<T>(NameString name, out T? value)
    {
        if (_coercedValues.TryGetValue(name.Value, out VariableValueOrLiteral variableValue))
        {
            Type requestedType = typeof(T);

            if (requestedType == typeof(IValueNode))
            {
                value = (T)variableValue.ValueLiteral;
                return true;
            }

            if (typeof(IValueNode).IsAssignableFrom(requestedType))
            {
                if (variableValue.ValueLiteral is T casted)
                {
                    value = casted;
                    return true;
                }

                value = default!;
                return false;
            }

            if (variableValue.Value is null)
            {
                value = default;
                return true;
            }

            if (variableValue.Value.GetType() == requestedType)
            {
                value = (T)variableValue.Value;
                return true;
            }

            if (variableValue.Value is T castedValue)
            {
                value = castedValue;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public IEnumerator<VariableValue> GetEnumerator()
    {
        foreach (KeyValuePair<string, VariableValueOrLiteral> item in _coercedValues)
        {
            IInputType type = item.Value.Type;
            IValueNode value = item.Value.ValueLiteral;
            yield return new VariableValue(item.Key, type, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
