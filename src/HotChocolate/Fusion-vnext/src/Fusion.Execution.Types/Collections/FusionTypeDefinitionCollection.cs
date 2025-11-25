using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.Properties.FusionTypeResources;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionTypeDefinitionCollection
    : IReadOnlyTypeDefinitionCollection
{
    private readonly IFusionTypeDefinition[] _types;
    private readonly FrozenDictionary<string, IFusionTypeDefinition> _typesLookup;
    private readonly int _length;

    public FusionTypeDefinitionCollection(IFusionTypeDefinition[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types;
        _typesLookup = types.ToFrozenDictionary(t => t.Name);
        _types.PartitionByAccessibility(out _length);
    }

    public int Count => _length;

    public ITypeDefinition this[int index]
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

    public ITypeDefinition this[string name]
    {
        get
        {
            var type = _typesLookup[name];

            if (type.IsInaccessible)
            {
                throw new KeyNotFoundException(string.Format(
                    FusionTypeDefinitionCollection_TypeName_NotFound,
                    name));
            }

            return type;
        }
    }

    public ITypeDefinition GetType(
        string name,
        bool allowInaccessibleFields)
    {
        var type = _typesLookup[name];

        if (!allowInaccessibleFields && type.IsInaccessible)
        {
            throw new KeyNotFoundException(string.Format(
                FusionTypeDefinitionCollection_TypeName_NotFound,
                name));
        }

        return type;
    }

    public ITypeDefinition GetTypeAt(
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

    [return: NotNull]
    public T GetType<T>(string name) where T : ITypeDefinition
        => GetType<T>(name, allowInaccessibleFields: true);

    [return: NotNull]
    public T GetType<T>(
        string name,
        bool allowInaccessibleFields)
        where T : ITypeDefinition
    {
        if (_typesLookup.TryGetValue(name, out var type)
            && (allowInaccessibleFields || !type.IsInaccessible))
        {
            if (type is T casted)
            {
                return casted;
            }

            throw new InvalidCastException(string.Format(
                FusionTypeDefinitionCollection_TypeIsNotOfRequestedT,
                name,
                typeof(T).Name));
        }

        throw new KeyNotFoundException(string.Format(
            FusionTypeDefinitionCollection_TypeName_NotFound,
            name));
    }

    public bool TryGetType(
        string name,
        [NotNullWhen(true)] out ITypeDefinition? typeDefinition)
        => TryGetType(name, allowInaccessibleFields: false, out typeDefinition);

    public bool TryGetType(
        string name,
        bool allowInaccessibleFields,
        [NotNullWhen(true)] out ITypeDefinition? typeDefinition)
    {
        if (_typesLookup.TryGetValue(name, out var type)
            && (allowInaccessibleFields || !type.IsInaccessible))
        {
            typeDefinition = type;
            return true;
        }

        typeDefinition = null;
        return false;
    }

    public bool TryGetType<T>(
        string name,
        [NotNullWhen(true)] out T? typeDefinition)
        where T : ITypeDefinition
        => TryGetType(name, allowInaccessibleFields: false, out typeDefinition);

    public bool TryGetType<T>(
        string name,
        bool allowInaccessibleFields,
        [NotNullWhen(true)] out T? typeDefinition)
        where T : ITypeDefinition
    {
        if (_typesLookup.TryGetValue(name, out var type)
            && (allowInaccessibleFields || !type.IsInaccessible)
            && type is T casted)
        {
            typeDefinition = casted;
            return true;
        }

        typeDefinition = default;
        return false;
    }

    public bool ContainsName(string name)
        => _typesLookup.TryGetValue(name, out var type)
            && !type.IsInaccessible;

    public bool ContainsName(string name, bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _typesLookup.ContainsKey(name);
        }

        return _typesLookup.TryGetValue(name, out var type) && !type.IsInaccessible;
    }

    public TypeEnumerator AsEnumerable() => new(_types, _length);

    public TypeEnumerator AsEnumerable(bool allowInaccessibleFields)
        => allowInaccessibleFields
            ? new TypeEnumerator(_types, _types.Length)
            : new TypeEnumerator(_types, _length);

    /// <summary>
    /// Returns an enumerator that iterates through the types in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="TypeEnumerator"/> for the fields.
    /// </returns>
    public TypeEnumerator GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<ITypeDefinition> IEnumerable<ITypeDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

     /// <summary>
    /// An enumerator for iterating through type definitions.
    /// </summary>
    public struct TypeEnumerator : IEnumerable<ITypeDefinition>, IEnumerator<ITypeDefinition>
    {
        private readonly IFusionTypeDefinition[] _types;
        private readonly int _length;
        private int _position;

        internal TypeEnumerator(IFusionTypeDefinition[] types, int length)
        {
            Debug.Assert(types is not null);

            _types = types;
            _length = length;
            _position = -1;
            Current = null!;
        }

        /// <summary>
        /// Gets the field at the current position of the enumerator.
        /// </summary>
        public ITypeDefinition Current { get; private set; }

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

        readonly IEnumerator<ITypeDefinition> IEnumerable<ITypeDefinition>.GetEnumerator()
            => GetEnumerator();

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
                Current = null!;
                return false;
            }

            if (_position < _length && ++_position < _length)
            {
                Current = _types[_position];
                return true;
            }

            Current = null!;
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first field in the collection.
        /// </summary>
        public void Reset()
        {
            _position = -1;
            Current = null!;
        }

        readonly void IDisposable.Dispose() { }
    }
}
