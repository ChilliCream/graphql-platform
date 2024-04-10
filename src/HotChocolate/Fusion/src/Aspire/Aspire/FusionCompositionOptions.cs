using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// The fusion options.
/// </summary>
public class FusionCompositionOptions : IResourceAnnotation
{
    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    public bool EnableGlobalObjectIdentification { get; set; }
}
