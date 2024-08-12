using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents the map of argument values that can be accessed on the <see cref="ISelection"/>.
/// </summary>
public sealed class ArgumentMap
    : IReadOnlyDictionary<string, ArgumentValue>
    , IEnumerable<ArgumentValue>
{
    private readonly Dictionary<string, ArgumentValue> _arguments;
    private readonly bool _isFinal;
    private readonly bool _hasErrors;

    internal ArgumentMap(Dictionary<string, ArgumentValue> arguments)
    {
        _arguments = arguments;
        _isFinal = true;

        if (_arguments.Count > 0)
        {
            foreach (var argument in arguments.Values)
            {
                if (!argument.IsFullyCoerced)
                {
                    _isFinal = false;
                }

                if (argument.HasError)
                {
                    _hasErrors = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets an empty argument map.
    /// </summary>
    public static ArgumentMap Empty { get; } = new(new Dictionary<string, ArgumentValue>());

    /// <summary>
    /// This indexer allows to access the <see cref="ArgumentValue"/>
    /// by the argument <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    public ArgumentValue this[string name] => _arguments[name];

    /// <summary>
    /// Specifies if the argument map is fully coerced and has no errors.
    /// </summary>
    public bool IsFullyCoercedNoErrors => _isFinal && !_hasErrors;

    /// <summary>
    /// Specifies if this argument map has errors.
    /// </summary>
    public bool HasErrors => _hasErrors;

    /// <summary>
    /// The argument count.
    /// </summary>
    public int Count => _arguments.Count;

    IEnumerable<string> IReadOnlyDictionary<string, ArgumentValue>.Keys
        => _arguments.Keys;

    IEnumerable<ArgumentValue> IReadOnlyDictionary<string, ArgumentValue>.Values
        => _arguments.Values;

    /// <summary>
    /// This method allows to check if an argument value with the specified
    /// argument <paramref name="name"/> exists.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the argument exists; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name) => _arguments.ContainsKey(name);

    bool IReadOnlyDictionary<string, ArgumentValue>.ContainsKey(string key)
        => ContainsName(key);

    /// <summary>
    /// Tries to get an <see cref="ArgumentValue"/> by its <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <param name="value">
    /// The argument value.
    /// </param>
    /// <returns>
    /// <c>true</c> if an argument value with the specified
    /// <paramref name="value"/> was retrieved; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [NotNullWhen(true)] out ArgumentValue? value)
        => _arguments.TryGetValue(name, out value);

    bool IReadOnlyDictionary<string, ArgumentValue>.TryGetValue(
        string key,
        out ArgumentValue value)
        => TryGetValue(key, out value!);

    /// <summary>
    /// Gets an enumerator for the argument values.
    /// </summary>
    public IEnumerator<ArgumentValue> GetEnumerator()
        => _arguments.Values.GetEnumerator();

    IEnumerator<KeyValuePair<string, ArgumentValue>>
        IEnumerable<KeyValuePair<string, ArgumentValue>>.GetEnumerator()
        => _arguments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
