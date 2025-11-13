using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

/// <summary>
/// The base class for GraphQL field definition collections,
/// like collections of Output Field Definitions, Input Value Definitions etc.
/// </summary>
/// <typeparam name="TField">
/// The type of field definition in the collection.
/// </typeparam>
public abstract class FusionFieldDefinitionCollection<TField>
    : IEnumerable<TField> where TField : IFieldDefinition, IInaccessibleProvider
{
    private readonly TField[] _fields;
    private readonly FrozenDictionary<string, TField> _map;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionFieldDefinitionCollection{TField}"/> class.
    /// </summary>
    /// <param name="fields">
    /// The array of field definitions to store in the collection.
    /// </param>
    protected FusionFieldDefinitionCollection(TField[] fields)
    {
        _map = fields.ToFrozenDictionary(t => t.Name);
        _fields = fields;
        _fields.PartitionByAccessibility(out _length);
    }

    /// <summary>
    /// Gets the count of fields in the collection.
    /// </summary>
    public int Count => _length;

    /// <summary>
    /// Gets the field with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the field to retrieve.
    /// </param>
    /// <returns>
    /// The field with the specified name.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the field is not found.
    /// </exception>
    public TField this[string name]
    {
        get
        {
            var field = _map[name];

            if (field.IsInaccessible)
            {
                throw new KeyNotFoundException();
            }

            return field;
        }
    }

    /// <summary>
    /// Gets the field at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the field to retrieve.
    /// </param>
    /// <returns>
    /// The field at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public TField this[int index]
    {
        get
        {
            if (index < _length)
            {
                return _fields[index];
            }

            throw new IndexOutOfRangeException();
        }
    }

    /// <summary>
    /// Gets a field by name with optional support for retrieving
    /// inaccessible (internal) fields.
    /// </summary>
    /// <param name="name">
    /// The name of the field to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible fields can be retrieved; otherwise, only accessible fields are returned.
    /// </param>
    /// <returns>
    /// The field with the specified name.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the field is not found.
    /// </exception>
    public TField GetField(
        string name,
        bool allowInaccessibleFields)
    {
        var field = _map[name];

        if (!allowInaccessibleFields && field.IsInaccessible)
        {
            throw new KeyNotFoundException();
        }

        return field;
    }

    /// <summary>
    /// Gets a field at the specified index with optional support for
    /// inaccessible (internal) fields.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the field to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible fields can be retrieved; otherwise, only accessible fields are returned.
    /// </param>
    /// <returns>
    /// The field at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public TField GetFieldAt(
        int index,
        bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _fields[index];
        }

        if (index < _length)
        {
            return _fields[index];
        }

        throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// Attempts to get a field by name.
    /// </summary>
    /// <param name="name">
    /// The name of the field to retrieve.
    /// </param>
    /// <param name="field">
    /// When this method returns, contains the field with the specified name if found;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetField(
        string name,
        [NotNullWhen(true)] out TField? field)
    {
        if (_map.TryGetValue(name, out field) && !field.IsInaccessible)
        {
            return true;
        }

        field = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a field by name with optional support for
    /// inaccessible (internal) fields.
    /// </summary>
    /// <param name="name">
    /// The name of the field to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible fields can be retrieved; otherwise, only accessible fields are returned.
    /// </param>
    /// <param name="field">
    /// When this method returns, contains the field with the specified name if found;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetField(
        string name,
        bool allowInaccessibleFields,
        [NotNullWhen(true)] out TField? field)
    {
        if (allowInaccessibleFields)
        {
            return _map.TryGetValue(name, out field);
        }

        if (_map.TryGetValue(name, out field) && !field.IsInaccessible)
        {
            return true;
        }

        field = default;
        return false;
    }

    /// <summary>
    /// Determines whether the collection contains a field with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains a field with the specified name; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name)
        => _map.TryGetValue(name, out var field)
            && !field.IsInaccessible;

    /// <summary>
    /// Determines whether the collection contains a field with the specified name,
    /// with optional support for inaccessible (internal) fields.
    /// </summary>
    /// <param name="name">
    /// The name to check.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible fields are included in the check; otherwise, only accessible fields are checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains a field with the specified name; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name, bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _map.ContainsKey(name);
        }

        return _map.TryGetValue(name, out var field) && !field.IsInaccessible;
    }

    /// <summary>
    /// Returns an enumerator for the fields in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="FieldEnumerator"/> for the fields.
    /// </returns>
    public FieldEnumerator AsEnumerable() => new(_fields, _length);

    /// <summary>
    /// Returns an enumerator with optional support for inaccessible (internal) fields.
    /// </summary>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, the enumerator includes inaccessible fields; otherwise, only accessible fields are included.
    /// </param>
    /// <returns>
    /// A <see cref="FieldEnumerator"/> for the fields.
    /// </returns>
    public FieldEnumerator AsEnumerable(bool allowInaccessibleFields)
        => allowInaccessibleFields ? new(_fields, _fields.Length) : new(_fields, _length);

    /// <summary>
    /// Returns an enumerator that iterates through the fields in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="FieldEnumerator"/> for the fields.
    /// </returns>
    public FieldEnumerator GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<TField> IEnumerable<TField>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// An enumerator for iterating through field definitions.
    /// </summary>
    public struct FieldEnumerator : IEnumerable<TField>, IEnumerator<TField>
    {
        private readonly TField[] _fields;
        private readonly int _length;
        private int _position;

        internal FieldEnumerator(TField[] fields, int length)
        {
            Debug.Assert(fields is not null);

            _fields = fields;
            _length = length;
            _position = -1;
            Current = default!;
        }

        /// <summary>
        /// Gets the field at the current position of the enumerator.
        /// </summary>
        public TField Current { get; private set; }

        readonly object? IEnumerator.Current => Current;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="FieldEnumerator"/> for the collection.</returns>
        public readonly FieldEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator.Reset();
            return enumerator;
        }

        readonly IEnumerator<TField> IEnumerable<TField>.GetEnumerator() => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Advances the enumerator to the next field in the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next field;
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            var length = _length;

            if (length == 0)
            {
                Current = default!;
                return false;
            }

            if (_position < _length && ++_position < _length)
            {
                Current = _fields[_position];
                return true;
            }

            Current = default!;
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first field in the collection.
        /// </summary>
        public void Reset()
        {
            _position = -1;
            Current = default!;
        }

        readonly void IDisposable.Dispose() { }
    }
}
