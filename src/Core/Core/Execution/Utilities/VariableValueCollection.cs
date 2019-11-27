using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class VariableValueCollection
        : IVariableValueCollection
    {
        private readonly ITypeConversion _converter;
        private readonly Dictionary<string, VariableValue> _variables;

        public VariableValueCollection(
            ITypeConversion converter,
            Dictionary<string, VariableValue> values)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
            _variables = values
                ?? throw new ArgumentNullException(nameof(values));
        }

        public T GetVariable<T>(NameString name)
        {
            name.EnsureNotEmpty("name");

            if (!TryGetVariable(name, out T variableValue))
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(CoreResources
                        .VariableCollection_VariableNotDeclared)
                    .SetExtension(nameof(name), name)
                    .Build());
            }

            return variableValue;
        }

        public bool TryGetVariable<T>(NameString name, out T value)
        {
            name.EnsureNotEmpty("name");

            if (_variables.TryGetValue(name, out VariableValue variableValue))
            {
                object coercedValue = null;

                if (typeof(T) == typeof(object))
                {
                    coercedValue = variableValue.Literal == null
                        ? variableValue.Value
                        : variableValue.Literal;
                }
                else if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
                {
                    if (variableValue.Literal == null)
                    {
                        coercedValue = variableValue.Type.ParseValue(variableValue.Value);
                    }
                    else
                    {
                        coercedValue = variableValue.Literal;
                    }
                }
                else
                {
                    if (variableValue.Literal == null)
                    {
                        coercedValue = variableValue.Value;
                    }
                    else
                    {
                        coercedValue = variableValue.Type.ParseLiteral(variableValue.Literal);
                    }
                }

                if (coercedValue is T castedValue)
                {
                    value = castedValue;
                }
                else
                {
                    value = (T)_converter.Convert(typeof(object), typeof(T), coercedValue);
                }
                return true;
            }

            value = default;
            return false;
        }
    }
}
