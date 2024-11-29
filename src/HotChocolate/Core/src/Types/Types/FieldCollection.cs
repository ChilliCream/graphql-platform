using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#nullable enable

namespace HotChocolate.Types;

public sealed class FieldCollection<T> : IFieldCollection<T> where T : class, IField
{
    private readonly FrozenDictionary<string, T> _fieldsLookup;
    private readonly T[] _fields;

    internal FieldCollection(T[] fields)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _fieldsLookup = _fields.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    private FieldCollection(Dictionary<string, T> fieldsLookup, T[] fields)
    {
        _fieldsLookup = fieldsLookup.ToFrozenDictionary(StringComparer.Ordinal);
        _fields = fields;
    }

    public T this[string fieldName] => _fieldsLookup[fieldName];

    public T this[int index] => _fields[index];

    public int Count => _fields.Length;

    public bool ContainsField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            throw new ArgumentNullException(fieldName);
        }

        return _fieldsLookup.ContainsKey(fieldName);
    }

    public bool TryGetField(string fieldName, [NotNullWhen(true)] out T? field)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

        if (_fieldsLookup.TryGetValue(fieldName, out var item))
        {
            field = item;
            return true;
        }

        field = default;
        return false;
    }

    internal ReadOnlySpan<T> AsSpan() => _fields;

    internal ref T GetReference()
        => ref MemoryMarshal.GetArrayDataReference(_fields);

    public IEnumerator<T> GetEnumerator()
        => _fields.Length == 0
            ? EmptyFieldEnumerator.Instance
            : new FieldEnumerator(_fields);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static FieldCollection<T> Empty { get; } = new([]);

    internal static FieldCollection<T> TryCreate(T[] fields, out IReadOnlyCollection<string>? duplicateFieldNames)
    {
        var internalFields = fields ?? throw new ArgumentNullException(nameof(fields));
        var internalLookup = new Dictionary<string, T>(internalFields.Length, StringComparer.Ordinal);
        HashSet<string>? duplicates = null;

        foreach (var field in internalFields)
        {
            if (!internalLookup.TryAdd(field.Name, field))
            {
                (duplicates ??= []).Add(field.Name);
            }
        }

        if (duplicates?.Count > 0)
        {
            duplicateFieldNames = duplicates;
            return Empty;
        }

        duplicateFieldNames = null;
        return new FieldCollection<T>(internalLookup, fields);
    }

    private sealed class FieldEnumerator(T[] fields) : IEnumerator<T>
    {
        private int _index = -1;

        public T Current { get; private set; } = default!;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;

            if (_index < fields.Length)
            {
                Current = fields[_index];
                return true;
            }

            Current = default!;
            return false;
        }

        public void Reset()
        {
            Current = default!;
            _index = -1;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    private sealed class EmptyFieldEnumerator : IEnumerator<T>
    {
        private EmptyFieldEnumerator() { }

        public bool MoveNext() => false;

        public void Reset() { }

        public T Current => default!;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        internal static readonly EmptyFieldEnumerator Instance = new();
    }
}
