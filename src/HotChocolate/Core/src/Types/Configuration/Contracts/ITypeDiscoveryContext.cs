using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
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

    /// <summary>
    /// Registers a reference to a directive on which <see cref="Type" /> depends.
    /// </summary>
    /// <param name="reference">
    /// A reference to a directive.
    /// </param>
    void RegisterDependency(
        IDirectiveReference reference);

    /// <summary>
    /// Registers multiple references to directives on which <see cref="Type" /> depends.
    /// </summary>
    /// <param name="references">
    /// Multiple references to a directives.
    /// </param>
    void RegisterDependencyRange(
        IEnumerable<IDirectiveReference> references);
}
