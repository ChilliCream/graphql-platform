using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing
{
    internal class VariableValueCollection : IVariableValueCollection
    {
        private readonly Dictionary<string, VariableValue> _coercedValues;

        public VariableValueCollection(Dictionary<string, VariableValue> coercedValues)
        {
            _coercedValues = coercedValues;
        }

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
            if (_coercedValues.TryGetValue(name.Value, out VariableValue variableValue))
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

                    if (variableValue.ValueLiteral is { })
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

        public static VariableValueCollection Empty { get; } =
            new VariableValueCollection(new Dictionary<string, VariableValue>());
    }
}
