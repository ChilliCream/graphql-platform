#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents a read-only collection of directives.
/// </summary>
public interface IReadOnlyDirectiveCollection : IReadOnlyList<IDirective>
{
    /// <summary>
    /// Gets all directives of a certain directive definition.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive definition.
    /// </param>
    IEnumerable<IDirective> this[string directiveName] { get; }

    /// <summary>
    /// Gets the first directive that matches the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified name.
    /// </returns>
    IDirective? FirstOrDefault(string directiveName);

    /// <summary>
    /// Gets the first directive that matches the specified runtime type.
    /// </summary>
    /// <param name="runtimeType">
    /// The runtime type of the directive.
    /// </param>
    /// <returns>
    /// The first directive that matches the specified runtime type.
    /// </returns>
    IDirective? FirstOrDefault(Type runtimeType);

    /// <summary>
    /// Determines whether the collection contains a directive with the specified name.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains a directive with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string directiveName);
}
