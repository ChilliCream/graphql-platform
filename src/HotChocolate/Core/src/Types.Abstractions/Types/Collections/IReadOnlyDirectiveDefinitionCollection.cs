using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyDirectiveDefinitionCollection : IEnumerable<IDirectiveDefinition>
{
    /// <summary>
    /// Gets a directive type by its name.
    /// </summary>
    /// <param name="name">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns directive type that was resolved by the given name
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

    bool ContainsName(string name);
}
