#if NET9_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of enum values.
/// </summary>
public sealed class EnumValueCollection : IReadOnlyList<EnumValue>
{
#if NET9_0_OR_GREATER
    private const int CharStackallocThreshold = 128;
#endif
    private const int LinearScanThreshold = 8;

    private readonly EnumValue[] _values;
    private readonly FrozenDictionary<string, EnumValue> _nameLookup;
    private readonly bool _ordinalNames;
    private ReadOnlyEnumValueCollection? _readOnlyValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumValueCollection"/> class.
    /// </summary>
    /// <param name="values">
    /// The enum values that are part of this collection.
    /// </param>
    /// <param name="nameComparer">
    /// The name comparer that is used to compare enum names.
    /// </param>
    public EnumValueCollection(EnumValue[] values, IEqualityComparer<string> nameComparer)
    {
        _values = values;
        _nameLookup = _values.ToFrozenDictionary(v => v.Name, nameComparer);
        _ordinalNames = ReferenceEquals(nameComparer, StringComparer.Ordinal)
            || ReferenceEquals(nameComparer, EqualityComparer<string>.Default);
    }

    public EnumValue this[string name] => _nameLookup[name];

    public EnumValue this[int index] => _values[index];

    public int Count => _values.Length;

    public bool ContainsName(string name) => _nameLookup.ContainsKey(name);

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
        => _nameLookup.TryGetValue(name, out value);

    /// <summary>
    /// Attempts to get an enum value by its UTF-8 encoded name.
    /// </summary>
    /// <param name="utf8Name">
    /// The enum value name as UTF-8 bytes.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the enum value with the specified name if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the enum value was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(ReadOnlySpan<byte> utf8Name, [NotNullWhen(true)] out EnumValue? value)
    {
        // a few names compare faster than they transcode and hash.
        if (_ordinalNames && _values.Length <= LinearScanThreshold)
        {
            return TryScan(utf8Name, out value);
        }

#if NET9_0_OR_GREATER
        if (utf8Name.Length <= CharStackallocThreshold
            && _nameLookup.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup))
        {
            Span<char> buffer = stackalloc char[CharStackallocThreshold];

            if (Ascii.ToUtf16(utf8Name, buffer, out var written) is OperationStatus.Done)
            {
                return lookup.TryGetValue(buffer[..written], out value);
            }

            // Enum value names are validated GraphQL identifiers and therefore ASCII,
            // so a non-ASCII byte can never match a name compared ordinally.
            if (_ordinalNames)
            {
                value = null;
                return false;
            }
        }
#endif

        if (_ordinalNames)
        {
            return TryScan(utf8Name, out value);
        }

        return _nameLookup.TryGetValue(Encoding.UTF8.GetString(utf8Name), out value);
    }

    private bool TryScan(ReadOnlySpan<byte> utf8Name, [NotNullWhen(true)] out EnumValue? value)
    {
        foreach (var candidate in _values)
        {
            if (Ascii.Equals(utf8Name, candidate.Name.AsSpan()))
            {
                value = candidate;
                return true;
            }
        }

        value = null;
        return false;
    }

    internal IReadOnlyEnumValueCollection AsReadOnlyEnumValueCollection()
        => _readOnlyValues ??= new ReadOnlyEnumValueCollection(this);

    public IEnumerator<EnumValue> GetEnumerator() => Unsafe.As<IEnumerable<EnumValue>>(_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class ReadOnlyEnumValueCollection(
        EnumValueCollection values)
        : IReadOnlyEnumValueCollection
    {
        public int Count => values._values.Length;

        public IEnumValue this[int index] => values._values[index];

        public IEnumValue this[string name] => values._nameLookup[name];

        public bool ContainsName(string name) => values._nameLookup.ContainsKey(name);

        public bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
        {
            if (values._nameLookup.TryGetValue(name, out var enumValue))
            {
                value = enumValue;
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<IEnumValue> GetEnumerator()
            => values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
