namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Processing-only metadata that records enabled Apollo Federation compatibility
/// capabilities on a source schema while it is composed.
/// </summary>
internal sealed class ApolloFederationCompatibilityMetadata
{
    public bool AllowNonResolvableInterfaceObjects { get; init; }
}
