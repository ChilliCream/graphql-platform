using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal class VariableValueCollection : IVariableValueCollection
    {
        private readonly Dictionary<string, VariableValueOrLiteral> _coercedValues;

        public VariableValueCollection(Dictionary<string, VariableValueOrLiteral> coercedValues)
        {
            _coercedValues = coercedValues;
        }

        public static VariableValueCollection Empty { get; } =
            new(new Dictionary<string, VariableValueOrLiteral>());

        public T GetVariable<T>(NameString name)
        {
            if (TryGetVariable(name, out T value))
            {
                return value;
            }

            if (_coercedValues.ContainsKey(name))
            {
                throw ThrowHelper.VariableNotOfType(name, typeof(T));
            }

            throw ThrowHelper.VariableNotFound(name);
        }

        public bool TryGetVariable<T>(NameString name, [NotNullWhen(true)] out T value)
        {
            if (_coercedValues.TryGetValue(name.Value, out VariableValueOrLiteral variableValue))
            {
                if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
                {
                    if (variableValue.ValueLiteral is T casted)
                    {
                        value = casted;
                        return true;
                    }

                    if (variableValue.ValueLiteral is null)
                    {
                        IValueNode literal = variableValue.Type.ParseValue(variableValue.Value);
                        if (literal is T casted2)
                        {
                            value = casted2;
                            return true;
                        }
                    }
                }
                else
                {
                    if (variableValue.Value is T casted)
                    {
                        value = casted;
                        return true;
                    }

                    if (variableValue.ValueLiteral is not null)
                    {
                        object? temp  = variableValue.Type.ParseLiteral(variableValue.ValueLiteral);
                        if (temp is T casted2)
                        {
                            value = casted2;
                            return true;
                        }
                    }
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
                IValueNode value = item.Value.ValueLiteral ?? type.ParseValue(item.Value.Value);
                yield return new VariableValue(item.Key, type, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
