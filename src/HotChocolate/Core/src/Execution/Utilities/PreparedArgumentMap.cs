using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedArgumentMap : IPreparedArgumentMap
    {
        private readonly IReadOnlyDictionary<NameString, PreparedArgument> _arguments;

        public PreparedArgumentMap(IReadOnlyDictionary<NameString, PreparedArgument> arguments)
        {
            _arguments = arguments;
            if (_arguments.Count > 0)
            {
                foreach (PreparedArgument argument in arguments.Values)
                {
                    if (!argument.IsFinal)
                    {
                        IsFinal = false;
                    }

                    if (argument.IsError)
                    {
                        HasErrors = true;
                    }
                }
            }
        }

        public PreparedArgument this[NameString key] => _arguments[key];

        public bool IsFinal { get; } = true;

        public bool HasErrors { get; }

        public IEnumerable<NameString> Keys => _arguments.Keys;

        public IEnumerable<PreparedArgument> Values => _arguments.Values;

        public int Count => _arguments.Count;

        public bool ContainsKey(NameString key) => _arguments.ContainsKey(key);

        public bool TryGetValue(
            NameString key,
            [MaybeNullWhen(false)] out PreparedArgument value) =>
            _arguments.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<NameString, PreparedArgument>> GetEnumerator() =>
            _arguments.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
