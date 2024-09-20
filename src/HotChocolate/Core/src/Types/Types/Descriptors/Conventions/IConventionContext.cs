#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The convention context is available during the convention initialization process.
/// </summary>
public interface IConventionContext : IHasScope
{
    /// <summary>
    /// The schema level services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// The schema builder context data that can be used for extensions
    /// to pass state along the initialization process.
    /// This property can also be reached through <see cref="IHasContextData.ContextData" />.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// The descriptor context that is passed through the initialization process.
    /// </summary>
    IDescriptorContext DescriptorContext { get; }
}
