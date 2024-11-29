using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration;

/// <summary>
/// The type discovery context is available during the discovery phase of the type system.
/// In this phase types are inspected and registered.
/// </summary>
public interface ITypeDiscoveryContext : ITypeSystemObjectContext
{
    /// <summary>
    /// The collected type dependencies.
    /// </summary>
    IList<TypeDependency> Dependencies { get; }
}
