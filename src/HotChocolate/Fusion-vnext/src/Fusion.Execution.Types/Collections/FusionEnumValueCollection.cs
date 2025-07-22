using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionEnumValueCollection
    : IReadOnlyList<FusionEnumValue>
    , IReadOnlyEnumValueCollection
{
    private readonly FusionEnumValue[] _values;
    private readonly FrozenDictionary<string, FusionEnumValue> _map;

    public FusionEnumValueCollection(FusionEnumValue[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _map = values.ToFrozenDictionary(t => t.Name);
        _values = values;
    }

    public int Count => _values.Length;

    /// <summary>
    /// Gets the enum value with the specified name.
    /// </summary>
    public FusionEnumValue this[string name] => _map[name];

    IEnumValue IReadOnlyEnumValueCollection.this[string name] => _map[name];

    public FusionEnumValue this[int index] => _values[index];

    IEnumValue IReadOnlyList<IEnumValue>.this[int index] => this[index];

    /// <summary>
    /// Tries to get the <paramref name="value"/> for
    /// the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="value">
    /// The GraphQL enum value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <paramref name="name"/> represents a value of this enum type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [NotNullWhen(true)] out FusionEnumValue? value)
        => _map.TryGetValue(name, out value);

    bool IReadOnlyEnumValueCollection.TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
    {
        if(_map.TryGetValue(name, out var enumValue))
        {
            value = enumValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Determines whether the collection contains an enum value with the specified name.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an enum value with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name)
        => _map.ContainsKey(name);

    public IEnumerable<FusionEnumValue> AsEnumerable()
        => _values;

    public IEnumerator<FusionEnumValue> GetEnumerator()
        => Unsafe.As<IEnumerable<FusionEnumValue>>(_values).GetEnumerator();

    IEnumerator<IEnumValue> IEnumerable<IEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static FusionEnumValueCollection Empty { get; } = new([]);
}
