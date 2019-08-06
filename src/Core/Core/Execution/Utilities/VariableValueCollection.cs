using System;
using System.Collections.Generic;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class VariableValueCollection
        : IVariableValueCollection
    {
        private readonly ITypeConversion _converter;
        private readonly Dictionary<string, object> _variables;

        public VariableValueCollection(
            ITypeConversion converter,
            Dictionary<string, object> variables)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
            _variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
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

            if (_variables.TryGetValue(name, out object coercedValue))
            {
                if (coercedValue is T castedValue)
                {
                    value = castedValue;
                }
                else
                {
                    value = (T)_converter.Convert(
                        typeof(object), typeof(T), coercedValue);
                }
                return true;
            }

            value = default;
            return false;
        }
    }
}
