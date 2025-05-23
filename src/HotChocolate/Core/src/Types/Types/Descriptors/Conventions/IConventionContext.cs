#nullable enable

using HotChocolate.Features;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The convention context is available during the convention initialization process.
/// </summary>
public interface IConventionContext : IFeatureProvider, IHasScope
{
    /// <summary>
    /// The schema level services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// The descriptor context that is passed through the initialization process.
    /// </summary>
    IDescriptorContext DescriptorContext { get; }
}
