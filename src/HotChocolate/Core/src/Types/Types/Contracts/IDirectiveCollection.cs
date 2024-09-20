#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of directives of a <see cref="ITypeSystemMember"/>.
/// </summary>
public interface IDirectiveCollection : IReadOnlyCollection<Directive>
{
    /// <summary>
    /// Gets all directives of a certain directive type.
    /// </summary>
    /// <param name="directiveName"></param>
    IEnumerable<Directive> this[string directiveName] { get; }

    /// <summary>
    /// Gets a directive by its index.
    /// </summary>
    Directive this[int index] { get; }

    /// <summary>
    /// Gets the first directive that matches the given name or <c>null</c>.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns the first directive that matches the given name or <c>null</c>.
    /// </returns>
    Directive? FirstOrDefault(string directiveName);

    /// <summary>
    /// Gets the first directive that matches the given <typeparamref name="TRuntimeType"/> or <c>null</c>.
    /// </summary>
    /// <returns>
    /// Returns the first directive that matches the given <typeparamref name="TRuntimeType"/> or <c>null</c>.
    /// </returns>
    Directive? FirstOrDefault<TRuntimeType>();

    /// <summary>
    /// Checks if a directive with the specified <paramref name="directiveName"/> exists.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if a directive with the specified <paramref name="directiveName"/>
    /// exists; otherwise, <c>false</c> will be returned.
    /// </returns>
    bool ContainsDirective(string directiveName);

    /// <summary>
    /// Checks if a directive with the specified <typeparamref name="TRuntimeType"/> exists.
    /// </summary>
    /// <returns>
    /// Returns <c>true</c> if a directive with the specified <typeparamref name="TRuntimeType"/>
    /// exists; otherwise, <c>false</c> will be returned.
    /// </returns>
    bool ContainsDirective<TRuntimeType>();
}
