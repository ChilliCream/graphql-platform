using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Execution.Processing;

internal sealed class ArgumentMap : IArgumentMap
{
    private readonly IReadOnlyDictionary<string, ArgumentValue> _arguments;

    public ArgumentMap(IReadOnlyDictionary<string, ArgumentValue> arguments)
    {
        _arguments = arguments;
        IsFinal = true;

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

    public ArgumentValue this[string key] => _arguments[key];

    public bool IsFinalNoErrors => IsFinal && !HasErrors;

    public bool IsFinal { get; }

    public bool HasErrors { get; }

    public IEnumerable<string> Keys => _arguments.Keys;

    public IEnumerable<ArgumentValue> Values => _arguments.Values;

    public int Count => _arguments.Count;

    public bool ContainsKey(string key) => _arguments.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ArgumentValue value)
        => _arguments.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, ArgumentValue>> GetEnumerator()
        => _arguments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
