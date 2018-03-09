using System;
using System.Collections.Generic;
using Zeus.Abstractions;

namespace Zeus.Execution
{
    public class VariableCollection
        : IVariableCollection
    {
        private readonly IDictionary<string, object> _variables;
        public VariableCollection()
            : this(new Dictionary<string, object>())
        {

        }

        public VariableCollection(IDictionary<string, object> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            _variables = variables;
        }

        public T GetVariable<T>(string variableName)
        {
            if (_variables.TryGetValue(variableName, out var value))
            {
                return (T)value;
            }
            return default(T);
        }


        
    }
}