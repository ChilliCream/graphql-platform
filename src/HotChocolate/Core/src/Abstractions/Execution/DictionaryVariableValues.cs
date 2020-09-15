using System.Collections.Generic;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class DictionaryVariableValues
        : IVariableValues
    {
        private readonly IReadOnlyDictionary<string, object?> _values;

        public DictionaryVariableValues(IReadOnlyDictionary<string, object?> values)
        {
            _values = values;
        }

        public IReadOnlyDictionary<string, object?> ToDictionary(
            IReadOnlyDictionary<string, IInputType> variableDefinitions) =>
            _values;

        internal IReadOnlyDictionary<string, object?> Values => _values;
    }
}
