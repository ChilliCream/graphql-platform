using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

/// <summary>
/// Represents a collection of GraphQL object type definitions.
/// </summary>
public sealed class FusionObjectTypeDefinitionCollection
    : IReadOnlyObjectTypeDefinitionCollection
    , IEnumerable<FusionObjectTypeDefinition>
{
    private readonly FusionObjectTypeDefinition[] _types;
    private readonly FrozenSet<string> _typeNames;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionObjectTypeDefinitionCollection"/> class.
    /// </summary>
    /// <param name="types">
    /// The array of object type definitions to store in the collection.
    /// </param>
    public FusionObjectTypeDefinitionCollection(
        FusionObjectTypeDefinition[] types)
    {
        _types = types;
        _typeNames = types.Select(t => t.Name).ToFrozenSet();
        _types.PartitionByAccessibility(out _length);
    }

    /// <summary>
    /// Gets the count of object type definitions in the collection.
    /// </summary>
    public int Count => _length;

    /// <summary>
    /// Gets the object type definition at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the object type definition to retrieve.
    /// </param>
    /// <returns>
    /// The object type definition at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public FusionObjectTypeDefinition this[int index]
    {
        get
        {
            if (index < _length)
            {
                return _types[index];
            }

            throw new IndexOutOfRangeException();
        }
    }

    IObjectTypeDefinition IReadOnlyList<IObjectTypeDefinition>.this[int index]
        => this[index];

    /// <summary>
    /// Gets the object type definition at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the object type definition to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible (internal) types can be retrieved; otherwise, only accessible fields are returned.
    /// </param>
    /// <returns>
    /// The object type definition at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public FusionObjectTypeDefinition GetFieldAt(
        int index,
        bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _types[index];
        }

        if (index < _length)
        {
            return _types[index];
        }

        throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// Determines whether the collection contains an object type definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an object type definition with the specified name; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name)
    {
        if (!_typeNames.Contains(name))
        {
            return false;
        }

        // Check if the type is accessible
        for (var i = 0; i < _length; i++)
        {
            if (_types[i].Name == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the collection contains an object type definition with the specified name,
    /// with optional support for inaccessible (internal) types.
    /// </summary>
    /// <param name="name">
    /// The name to check.
    /// </param>
    /// <param name="allowInaccessibleTypes">
    /// If <c>true</c>, inaccessible types are included in the check; otherwise, only accessible types are checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an object type definition with the specified name; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name, bool allowInaccessibleTypes)
    {
        if (allowInaccessibleTypes)
        {
            return _typeNames.Contains(name);
        }

        return ContainsName(name);
    }

    /// <summary>
    /// Returns an enumerator for the object type definitions in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="TypeEnumerator"/> for the object type definitions.
    /// </returns>
    public TypeEnumerator AsEnumerable() => new(_types, _length);

    /// <summary>
    /// Returns an enumerator with optional support for inaccessible (internal) types.
    /// </summary>
    /// <param name="allowInaccessibleTypes">
    /// If <c>true</c>, the enumerator includes inaccessible types; otherwise, only accessible types are included.
    /// </param>
    /// <returns>
    /// A <see cref="TypeEnumerator"/> for the object type definitions.
    /// </returns>
    public TypeEnumerator AsEnumerable(bool allowInaccessibleTypes)
        => allowInaccessibleTypes ? new(_types, _types.Length) : new(_types, _length);

    /// <summary>
    /// Returns an enumerator that iterates through the object type definitions in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="TypeEnumerator"/> for the object type definitions.
    /// </returns>
    public TypeEnumerator GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<FusionObjectTypeDefinition> IEnumerable<FusionObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// An enumerator for iterating through object type definitions.
    /// </summary>
    public struct TypeEnumerator : IEnumerable<FusionObjectTypeDefinition>, IEnumerator<FusionObjectTypeDefinition>
    {
        private readonly FusionObjectTypeDefinition[] _types;
        private readonly int _length;
        private int _position;

        internal TypeEnumerator(FusionObjectTypeDefinition[] types, int length)
        {
            Debug.Assert(types is not null);

            _types = types;
            _length = length;
            _position = -1;
            Current = default!;
        }

        /// <summary>
        /// Gets the object type definition at the current position of the enumerator.
        /// </summary>
        public FusionObjectTypeDefinition Current { get; private set; }

        readonly object? IEnumerator.Current => Current;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="TypeEnumerator"/> for the collection.</returns>
        public readonly TypeEnumerator GetEnumerator()
        {
            var enumerator = this;
            enumerator.Reset();
            return enumerator;
        }

        readonly IEnumerator<FusionObjectTypeDefinition> IEnumerable<FusionObjectTypeDefinition>.GetEnumerator()
            => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Advances the enumerator to the next object type definition in the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next object type definition;
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
                Current = _types[_position];
                return true;
            }

            Current = default!;
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first object type definition in the collection.
        /// </summary>
        public void Reset()
        {
            _position = -1;
            Current = default!;
        }

        readonly void IDisposable.Dispose() { }
    }
}
