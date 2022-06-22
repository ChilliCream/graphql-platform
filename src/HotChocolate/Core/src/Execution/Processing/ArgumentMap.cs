using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Execution.Processing;

internal sealed class ArgumentMap : IArgumentMap
{
    private readonly IReadOnlyDictionary<NameString, ArgumentValue> _arguments;

    public ArgumentMap(IReadOnlyDictionary<NameString, ArgumentValue> arguments)
    {
        _arguments = arguments;

        IsFinal = arguments.Count == 0;

        if (_arguments.Count > 0)
        {
            foreach (var argument in arguments.Values)
            {
                if (!argument.IsFullyCoerced)
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

    public bool IsFinalNoErrors => IsFinal && !HasErrors;

    public bool IsFinal { get; }

    public bool HasErrors { get; }

    public IEnumerable<NameString> Keys => _arguments.Keys;

    public IEnumerable<ArgumentValue> Values => _arguments.Values;

    public int Count => _arguments.Count;

    public bool ContainsKey(NameString key) => _arguments.ContainsKey(key);

    public bool TryGetValue(NameString key, [MaybeNullWhen(false)] out ArgumentValue value)
        => _arguments.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<NameString, ArgumentValue>> GetEnumerator()
        => _arguments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
