#if NET9_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types;

public abstract class FieldCollection<T> : IReadOnlyList<T> where T : INameProvider
{
#if NET9_0_OR_GREATER
    private const int CharStackallocThreshold = 128;
    private const int LinearScanThreshold = 8;
#endif

    private readonly FrozenDictionary<string, T> _fieldsLookup;
    private readonly T[] _fields;

    protected FieldCollection(T[] fields)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));

        // We filter out duplicates in the lookup so we do not throw here.
        // Duplication of fields will be reported gracefully as a schema error
        // outside of this collection.
        var fieldsLookup = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var field in fields.AsSpan())
        {
            fieldsLookup.TryAdd(field.Name, field);
        }

        _fieldsLookup = fieldsLookup.ToFrozenDictionary();
    }

    public T this[string fieldName] => _fieldsLookup[fieldName];

    public T this[int index] => _fields[index];

    public int Count => _fields.Length;

    public bool ContainsField(string fieldName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        return _fieldsLookup.ContainsKey(fieldName);
    }

    public bool TryGetField(string fieldName, [NotNullWhen(true)] out T? field)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        if (_fieldsLookup.TryGetValue(fieldName, out var item))
        {
            field = item;
            return true;
        }

        field = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a field by its UTF-8 encoded name.
    /// </summary>
    /// <param name="utf8FieldName">
    /// The field name as UTF-8 bytes.
    /// </param>
    /// <param name="field">
    /// When this method returns, contains the field with the specified name if found;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetField(ReadOnlySpan<byte> utf8FieldName, [NotNullWhen(true)] out T? field)
    {
#if NET9_0_OR_GREATER
        if (_fields.Length > LinearScanThreshold && utf8FieldName.Length <= CharStackallocThreshold)
        {
            Span<char> buffer = stackalloc char[CharStackallocThreshold];

            // Field names are validated GraphQL identifiers and therefore ASCII,
            // so a non-ASCII byte can never match a field name.
            if (Ascii.ToUtf16(utf8FieldName, buffer, out var written) is OperationStatus.Done
                && _fieldsLookup.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(buffer[..written], out var item))
            {
                field = item;
                return true;
            }

            field = default;
            return false;
        }
#endif

        // a few names compare faster than they transcode and hash.
        foreach (var candidate in _fields)
        {
            if (Ascii.Equals(utf8FieldName, candidate.Name.AsSpan()))
            {
                field = candidate;
                return true;
            }
        }

        field = default;
        return false;
    }

    internal ReadOnlySpan<T> AsSpan() => _fields;

    internal ref T GetReference()
        => ref MemoryMarshal.GetArrayDataReference(_fields);

    public IEnumerator<T> GetEnumerator()
        => _fields.Length == 0
            ? EmptyFieldEnumerator.s_instance
            : new FieldEnumerator(_fields);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

        internal static readonly EmptyFieldEnumerator s_instance = new();
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
        ArgumentNullException.ThrowIfNull(arguments);
    }

    internal IReadOnlyFieldDefinitionCollection<IInputValueDefinition> AsReadOnlyFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(DirectiveArgumentCollection arguments) : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
    {
        public IInputValueDefinition this[string name] => arguments[name];

        public IInputValueDefinition this[int index] => arguments[index];

        public int Count => arguments.Count;

        public bool ContainsName(string name) => arguments.ContainsField(name);

        public bool TryGetField(string name, [NotNullWhen(true)] out IInputValueDefinition? field)
        {
            if (arguments.TryGetField(name, out var arg))
            {
                field = arg;
                return true;
            }

            field = null;
            return false;
        }

        public IEnumerator<IInputValueDefinition> GetEnumerator() => arguments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public sealed class InputFieldCollection : FieldCollection<InputField>
{
    private FieldDefinitionCollection? _wrapper;

    public InputFieldCollection(InputField[] fields) : base(fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
    }

    internal IReadOnlyFieldDefinitionCollection<IInputValueDefinition> AsReadOnlyFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(InputFieldCollection fields)
        : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
    {
        public IInputValueDefinition this[string name] => fields[name];

        public IInputValueDefinition this[int index] => fields[index];

        public int Count => fields.Count;

        public bool ContainsName(string name) => fields.ContainsField(name);

        public bool TryGetField(string name, [NotNullWhen(true)] out IInputValueDefinition? field)
        {
            if (fields.TryGetField(name, out var arg))
            {
                field = arg;
                return true;
            }

            field = null;
            return false;
        }
        public IEnumerator<IInputValueDefinition> GetEnumerator() => fields.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public sealed class ArgumentCollection : FieldCollection<Argument>
{
    private FieldDefinitionCollection? _wrapper;

    public ArgumentCollection(Argument[] fields) : base(fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
    }

    internal IReadOnlyFieldDefinitionCollection<IInputValueDefinition> AsReadOnlyFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(ArgumentCollection arguments) : IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
    {
        public IInputValueDefinition this[string name] => arguments[name];

        public IInputValueDefinition this[int index] => arguments[index];

        public int Count => arguments.Count;

        public bool ContainsName(string name) => arguments.ContainsField(name);

        public IEnumerator<IInputValueDefinition> GetEnumerator() => arguments.GetEnumerator();

        public bool TryGetField(string name, [NotNullWhen(true)] out IInputValueDefinition? field)
        {
            if (arguments.TryGetField(name, out var arg))
            {
                field = arg;
                return true;
            }

            field = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static ArgumentCollection Empty { get; } = new([]);
}

public sealed class InterfaceFieldCollection : FieldCollection<InterfaceField>
{
    private FieldDefinitionCollection? _wrapper;

    public InterfaceFieldCollection(InterfaceField[] fields) : base(fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
    }

    internal IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> AsReadOnlyFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(InterfaceFieldCollection fields)
        : IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
    {
        public IOutputFieldDefinition this[string name] => fields[name];

        public IOutputFieldDefinition this[int index] => fields[index];

        public int Count => fields.Count;

        public bool ContainsName(string name) => fields.ContainsField(name);

        public bool TryGetField(string name, [NotNullWhen(true)] out IOutputFieldDefinition? field)
        {
            if (fields.TryGetField(name, out var fld))
            {
                field = fld;
                return true;
            }

            field = null;
            return false;
        }

        public IEnumerator<IOutputFieldDefinition> GetEnumerator() => fields.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public sealed class ObjectFieldCollection : FieldCollection<ObjectField>
{
    private FieldDefinitionCollection? _wrapper;

    public ObjectFieldCollection(ObjectField[] fields) : base(fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
    }

    internal IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> AsReadOnlyFieldDefinitionCollection()
        => _wrapper ??= new FieldDefinitionCollection(this);

    private sealed class FieldDefinitionCollection(ObjectFieldCollection fields)
        : IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition>
    {
        public IOutputFieldDefinition this[string name] => fields[name];

        public IOutputFieldDefinition this[int index] => fields[index];

        public int Count => fields.Count;

        public bool ContainsName(string name) => fields.ContainsField(name);

        public bool TryGetField(string name, [NotNullWhen(true)] out IOutputFieldDefinition? field)
        {
            if (fields.TryGetField(name, out var fld))
            {
                field = fld;
                return true;
            }

            field = null;
            return false;
        }

        public IEnumerator<IOutputFieldDefinition> GetEnumerator() => fields.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
