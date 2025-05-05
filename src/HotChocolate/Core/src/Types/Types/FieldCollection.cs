using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types;

public abstract class FieldCollection<T> : IReadOnlyList<T> where T : INameProvider
{
    private readonly FrozenDictionary<string, T> _fieldsLookup;
    private readonly T[] _fields;

    protected FieldCollection(T[] fields)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _fieldsLookup = _fields.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
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

    internal static bool EnsureNoDuplicates(
        T[] fields,
        [NotNullWhen(false)] out IReadOnlyCollection<string>? duplicateFieldNames)
    {
        var internalFields = fields ?? throw new ArgumentNullException(nameof(fields));
        var names = TypeMemHelper.RentNameSet();
        HashSet<string>? duplicates = null;

        foreach (var field in internalFields)
        {
            if (!names.Add(field.Name))
            {
                (duplicates ??= []).Add(field.Name);
            }
        }

        TypeMemHelper.Return(names);

        if (duplicates?.Count > 0)
        {
            duplicateFieldNames = duplicates;
            return false;
        }

        duplicateFieldNames = null;
        return true;
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

/// <summary>
/// A collection of directive arguments.
/// </summary>
public sealed class DirectiveArgumentCollection : FieldCollection<DirectiveArgument>
{
    private FieldDefinitionCollection? _wrapper;

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveArgumentCollection"/>.
    /// </summary>
    /// <param name="arguments">
    /// The arguments that shall be contained in the collection.
    /// </param>
    public DirectiveArgumentCollection(DirectiveArgument[] arguments) : base(arguments)
    {
    }

    internal IReadOnlyFieldDefinitionCollection<IInputValueDefinition> AsFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(DirectiveArgumentCollection arguments) : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
    {

        public IInputValueDefinition this[string name] => arguments[name];

        public IInputValueDefinition this[int index] => arguments[index];

        public int Count => arguments.Count;

        public bool ContainsName(string name) => arguments.ContainsField(name);

        public IEnumerator<IInputValueDefinition> GetEnumerator() => arguments.GetEnumerator();

        public bool TryGetField(string name, [NotNullWhen(true)] out IInputValueDefinition? field)
        {
            if (TryGetField(name, out var arg))
            {
                field = arg;
                return true;
            }

            field = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
