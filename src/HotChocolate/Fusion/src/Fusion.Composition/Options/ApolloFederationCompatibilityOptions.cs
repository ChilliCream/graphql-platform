namespace HotChocolate.Fusion.Options;

/// <summary>
/// Compatibility options for composing Apollo Federation source schemas.
/// </summary>
public sealed class ApolloFederationCompatibilityOptions
{
    /// <summary>
    /// Allows Apollo Federation <c>@interfaceObject</c> stand-ins whose keys are not
    /// resolvable. Selections that cannot be routed through such a stand-in produce
    /// field errors at runtime.
    /// </summary>
    public bool AllowNonResolvableInterfaceObjects { get; set; }
}
