using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyTypeDefinitionCollection : IEnumerable<ITypeDefinition>
{
    ITypeDefinition this[string name] { get; }

    /// <summary>
    /// Gets a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type does not exist or is not of the
    /// specified type kind.
    /// </exception>
    [return: NotNull]
    T GetType<T>(string typeName) where T : ITypeDefinition;

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? type);

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
    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : ITypeDefinition;

    /// <summary>
    /// Checks if a type with the specified name exists.
    /// </summary>
    /// <param name="name">
    /// The name of the type.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a type with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string name);
}
