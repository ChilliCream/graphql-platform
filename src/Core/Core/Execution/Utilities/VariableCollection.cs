using System;
using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class VariableCollection
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
                // TODO : Resources
                throw new QueryException(QueryError.CreateVariableError(
                    "The specified variable was not declared.",
                    variableName));
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

            variableValue = default(T);
            return false;
        }
    }
}
