using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal class VariableValueCollection(
    Dictionary<string, VariableValueOrLiteral> coercedValues)
    : IVariableValueCollection
{
    public static VariableValueCollection Empty { get; } =
        new(new Dictionary<string, VariableValueOrLiteral>());

    public T? GetVariable<T>(string name)
    {
        if (TryGetVariable(name, out T? value))
        {
            return value;
        }

        if (coercedValues.ContainsKey(name))
        {
            throw ThrowHelper.VariableNotOfType(name, typeof(T));
        }

        throw ThrowHelper.VariableNotFound(name);
    }

    public bool TryGetVariable<T>(string name, out T? value)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (coercedValues.TryGetValue(name, out var variableValue))
        {
            var requestedType = typeof(T);

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
        foreach (var item in coercedValues)
        {
            var type = item.Value.Type;
            var value = item.Value.ValueLiteral;
            yield return new VariableValue(item.Key, type, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
