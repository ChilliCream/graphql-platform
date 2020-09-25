using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ArgumentMap : IArgumentMap
    {
        private readonly IReadOnlyDictionary<NameString, ArgumentValue> _arguments;

        public ArgumentMap(IReadOnlyDictionary<NameString, ArgumentValue> arguments)
        {
            _arguments = arguments;
            if (_arguments.Count > 0)
            {
                foreach (ArgumentValue argument in arguments.Values)
                {
                    if (!argument.IsFinal)
                    {
                        IsFinal = false;
                    }

                    if (argument.HasError)
                    {
                        HasErrors = true;
                    }
                }
            }
        }

        public ArgumentValue this[NameString key] => _arguments[key];

        public bool IsFinal { get; } = true;

        public bool HasErrors { get; }

        public IEnumerable<NameString> Keys => _arguments.Keys;

        public IEnumerable<ArgumentValue> Values => _arguments.Values;

        public int Count => _arguments.Count;

        public bool ContainsKey(NameString key) => _arguments.ContainsKey(key);

        public bool TryGetValue(
            NameString key,
            [NotNullWhen(true)] out ArgumentValue? value) =>
            _arguments.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<NameString, ArgumentValue>> GetEnumerator() =>
            _arguments.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
