using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedArgumentMap
        : IReadOnlyDictionary<NameString, PreparedArgument>
    {
        bool IsFinal { get; }

        bool HasErrors { get; }
    }

    internal sealed class PreparedArgumentMap : IPreparedArgumentMap
    {
        public PreparedArgumentMap(IReadOnlyDictionary<NameString, PreparedArgument> arguments)
        {
            foreach (PreparedArgument argument in arguments.Values)
            {
                if (!argument.IsFinal)
                {
                    IsFinal = true;   
                }
            }
        }

        public PreparedArgument this[NameString key] => throw new System.NotImplementedException();

        public bool IsFinal { get; }

        public bool HasErrors { get; }

        public IEnumerable<NameString> Keys => throw new System.NotImplementedException();

        public IEnumerable<PreparedArgument> Values => throw new System.NotImplementedException();

        public int Count => throw new System.NotImplementedException();

        public bool ContainsKey(NameString key)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<KeyValuePair<NameString, PreparedArgument>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValue(NameString key, [MaybeNullWhen(false)] out PreparedArgument value)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }

}
