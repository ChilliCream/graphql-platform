using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyTypeDefinitionCollection : IReadOnlyList<ITypeDefinition>
{
    ITypeDefinition this[string typeName] { get; }

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
    /// <param name="typeName">The name of the type.</param>
    /// <param name="typeDefinition">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    bool TryGetType(string typeName, [NotNullWhen(true)] out ITypeDefinition? typeDefinition);

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    bool TryGetType<T>(string typeName, [NotNullWhen(true)] out T? type) where T : ITypeDefinition;

    /// <summary>
    /// Checks if a type with the specified name exists.
    /// </summary>
    /// <param name="typeName">
    /// The name of the type.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a type with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string typeName);
}
