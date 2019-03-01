using System;
using System.Collections.Generic;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class VariableCollection
        : IVariableCollection
    {
        private readonly ITypeConversion _converter;
        private readonly Dictionary<string, object> _variables;

        public VariableCollection(
            ITypeConversion converter,
            Dictionary<string, object> variables)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
            _variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
        }

        public T GetVariable<T>(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentNullException(nameof(variableName));
            }

            if (!TryGetVariable(variableName, out T variableValue))
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(CoreResources.VariableCollection_VariableNotDeclared)
                    .SetExtension(nameof(variableName), variableName)
                    .Build());
            }

            return variableValue;
        }

        public bool TryGetVariable<T>(string variableName, out T variableValue)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentNullException(nameof(variableName));
            }

            if (_variables.TryGetValue(variableName, out object value))
            {
                if (value is T v)
                {
                    variableValue = v;
                }
                else
                {
                    variableValue = (T)_converter.Convert(
                        typeof(object), typeof(T), value);
                }
                return true;
            }

            variableValue = default;
            return false;
        }
    }
}
