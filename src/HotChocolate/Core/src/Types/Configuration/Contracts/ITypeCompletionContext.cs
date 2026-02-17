using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

/// <summary>
/// Represents the type completion context which gives access to data available while
/// the type is being completed.
/// </summary>
public interface ITypeCompletionContext : ITypeSystemObjectContext
{
    /// <summary>
    /// Global middleware components.
    /// </summary>
    IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

    /// <summary>
    /// The fallback object to type resolver.
    /// </summary>
    IsOfTypeFallback? IsOfType { get; }

    /// <summary>
    /// Tries to resolve a type by its <paramref name="typeRef" />.
    /// </summary>
    /// <typeparam name="T">
    /// The expected type.
    /// </typeparam>
    /// <param name="typeRef">
    /// The type reference representing the type.
    /// </param>
    /// <param name="type">
    /// The resolved types.
    /// </param>
    /// <returns>
    /// <c>true</c> if the type has been resolved; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetType<T>(TypeReference typeRef, [NotNullWhen(true)] out T? type) where T : IType;

    /// <summary>
    /// Gets a type by its type reference.
    /// </summary>
    /// <param name="typeRef">
    /// The type reference representing the type.
    /// </param>
    /// <typeparam name="T">
    /// The expected type.
    /// </typeparam>
    /// <returns>
    /// The resolved types.
    /// </returns>
    /// <exception cref="SchemaException">
    /// The type could not be resolved for the given <paramref name="typeRef" />.
    /// </exception>
    T GetType<T>(TypeReference typeRef) where T : IType;

    /// <summary>
    /// Tries to resolve a directive type by its <paramref name="directiveRef" />.
    /// </summary>
    /// <param name="directiveRef">
    /// The directive reference representing the directive.
    /// </param>
    /// <param name="directiveType">
    /// The resolved directive type.
    /// </param>
    /// <returns></returns>
    bool TryGetDirectiveType(
        TypeReference directiveRef,
        [NotNullWhen(true)] out DirectiveType? directiveType);
}
