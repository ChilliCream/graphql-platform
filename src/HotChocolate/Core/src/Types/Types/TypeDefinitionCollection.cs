#nullable enable
using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of type definitions.
/// </summary>
public sealed class TypeDefinitionCollection : IReadOnlyTypeDefinitionCollection
{
    private readonly FrozenDictionary<string, ITypeDefinition> _typeLookup;
    private readonly ITypeDefinition[] _types;

    public TypeDefinitionCollection(ITypeDefinition[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _typeLookup = types.ToFrozenDictionary(t => t.Name);
        _types = types;
    }

    /// <summary>
    /// Gets the number of types in the collection.
    /// </summary>
    public int Count => _types.Length;

    /// <summary>
    /// Gets a type by its index.
    /// </summary>
    /// <param name="index">The index of the type.</param>
    /// <returns>The type.</returns>
    public ITypeDefinition this[int index]
        => _types[index];

    /// <summary>
    /// Gets a type by its name.
    /// </summary>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    /// <returns>
    /// The type.
    /// </returns>
    public ITypeDefinition this[string name]
        => _typeLookup[name];

    /// <summary>
    /// Gets a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="name">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type does not exist or
    /// is not of the specified type kind.
    /// </exception>
    [return: NotNull]
    public T GetType<T>(string name)
        where T : ITypeDefinition
    {
        if (_typeLookup.TryGetValue(name, out var t))
        {
            if (t is T casted)
            {
                return casted;
            }

            throw new InvalidOperationException(
                $"The specified type '{name}' does not match the requested type.");
        }

        throw new ArgumentException("The specified type name does not exist.", nameof(name));
    }

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    public bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? type)
    {
        if (_typeLookup.TryGetValue(name, out var t))
        {
            type = t;
            return true;
        }

        type = null;
        return false;
    }

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type)
        where T : ITypeDefinition
    {
        if (_typeLookup.TryGetValue(name, out var t) && t is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    /// <summary>
    /// Checks if a type with the specified name exists.
    /// </summary>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a type with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name) => _typeLookup.ContainsKey(name);

    public IEnumerator<ITypeDefinition> GetEnumerator()
        => Unsafe.As<IEnumerable<ITypeDefinition>>(_types).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
