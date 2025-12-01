using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a read-only map of argument values for a field selection in a GraphQL query.
/// This map provides efficient access to coerced argument values and tracks coercion errors.
/// </summary>
public sealed class ArgumentMap : IReadOnlyDictionary<string, ArgumentValue>
{
    private readonly FrozenDictionary<string, ArgumentValue> _arguments;
    private readonly bool _hasErrors;

    internal ArgumentMap(Dictionary<string, ArgumentValue> arguments)
    {
        _arguments = arguments.ToFrozenDictionary(StringComparer.Ordinal);
        IsFullyCoercedNoErrors = true;

        if (_arguments.Count > 0)
        {
            foreach (var argument in arguments.Values)
            {
                if (!argument.IsFullyCoerced)
                {
                    IsFullyCoercedNoErrors = false;
                }

                if (argument.HasError)
                {
                    _hasErrors = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets an empty argument map with no arguments.
    /// </summary>
    public static ArgumentMap Empty { get; } = new([]);

    /// <summary>
    /// Gets the <see cref="ArgumentValue"/> for the specified argument name.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <returns>
    /// The <see cref="ArgumentValue"/> associated with the specified name.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified <paramref name="name"/> is not found.
    /// </exception>
    public ArgumentValue this[string name] => _arguments[name];

    /// <summary>
    /// Gets a value indicating whether all arguments in this map are
    /// fully coerced and no errors occurred during coercion.
    /// </summary>
    /// <value>
    /// <c>true</c> if all arguments are fully coerced without errors; otherwise, <c>false</c>.
    /// </value>
    public bool IsFullyCoercedNoErrors => field && !_hasErrors;

    /// <summary>
    /// Gets a value indicating whether any argument in this map has coercion errors.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one argument has errors; otherwise, <c>false</c>.
    /// </value>
    public bool HasErrors => _hasErrors;

    /// <summary>
    /// Gets the number of arguments in this map.
    /// </summary>
    /// <value>
    /// The total count of arguments.
    /// </value>
    public int Count => _arguments.Count;

    /// <summary>
    /// Gets an immutable array containing all argument names in this map.
    /// </summary>
    /// <value>
    /// An <see cref="ImmutableArray{T}"/> of argument names.
    /// </value>
    public ImmutableArray<string> ArgumentNames => _arguments.Keys;

    IEnumerable<string> IReadOnlyDictionary<string, ArgumentValue>.Keys
        => _arguments.Keys;

    /// <summary>
    /// Gets an immutable array containing all argument values in this map.
    /// </summary>
    /// <value>
    /// An <see cref="ImmutableArray{T}"/> of <see cref="ArgumentValue"/> instances.
    /// </value>
    public ImmutableArray<ArgumentValue> ArgumentValues => _arguments.Values;

    IEnumerable<ArgumentValue> IReadOnlyDictionary<string, ArgumentValue>.Values
        => _arguments.Values;

    /// <summary>
    /// Determines whether this map contains an argument with the specified name.
    /// </summary>
    /// <param name="name">
    /// The argument name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if an argument with the specified <paramref name="name"/> exists; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name) => _arguments.ContainsKey(name);

    bool IReadOnlyDictionary<string, ArgumentValue>.ContainsKey(string key)
        => ContainsName(key);

    /// <summary>
    /// Attempts to retrieve the <see cref="ArgumentValue"/> associated with the specified argument name.
    /// </summary>
    /// <param name="name">
    /// The argument name to locate.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the <see cref="ArgumentValue"/> associated with the specified
    /// <paramref name="name"/>, if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if an argument with the specified <paramref name="name"/> was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [NotNullWhen(true)] out ArgumentValue? value)
        => _arguments.TryGetValue(name, out value);

    bool IReadOnlyDictionary<string, ArgumentValue>.TryGetValue(
        string key,
        out ArgumentValue value)
        => TryGetValue(key, out value!);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, ArgumentValue>> GetEnumerator()
        => _arguments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
