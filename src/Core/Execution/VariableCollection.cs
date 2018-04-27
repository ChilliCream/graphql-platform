using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class VariableCollection
    {
        private readonly Dictionary<string, CoercedValue> _variables;

        public VariableCollection(Dictionary<string, CoercedValue> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            _variables = variables;
        }

        public T GetVariable<T>(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentNullException(nameof(variableName));
            }

            if (!_variables.TryGetValue(variableName,
                out CoercedValue variableValue))
            {
                throw new QueryException(new VariableError(
                    "The specified variable was not declared.", variableName));
            }

            IInputType inputType = variableValue.InputType;
            return (T)inputType.ParseLiteral(variableValue.Value, typeof(T));
        }
    }
}
