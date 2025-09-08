using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents a read-only collection of directive definitions.
/// </summary>
public interface IReadOnlyDirectiveDefinitionCollection : IReadOnlyList<IDirectiveDefinition>
{
    /// <summary>
    /// Gets a directive type by its name.
    /// </summary>
    /// <param name="name">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns directive type resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified directive type does not exist.
    /// </exception>
    IDirectiveDefinition this[string name] { get; }

    /// <summary>
    /// Tries to get a directive type by its name.
    /// </summary>
    /// <param name="name">
    /// The directive name.
    /// </param>
    /// <param name="directive">
    /// The directive type that was resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a directive type with the specified
    /// name exists; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetDirective(string name, [NotNullWhen(true)] out IDirectiveDefinition? directive);

    /// <summary>
    /// Determines whether the collection contains a directive definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the directive definition.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains a directive definition with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string name);
}
