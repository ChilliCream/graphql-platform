#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents a read-only collection of interface type definitions.
/// </summary>
public interface IReadOnlyInterfaceTypeDefinitionCollection : IReadOnlyList<IInterfaceTypeDefinition>
{
    /// <summary>
    /// Determines whether the collection contains an interface type definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the interface type definition.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains an interface type definition with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string name);
}
