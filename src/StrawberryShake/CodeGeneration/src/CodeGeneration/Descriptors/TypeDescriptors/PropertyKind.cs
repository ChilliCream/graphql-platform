namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

/// <summary>
/// Specifies the various property kinds.
/// </summary>
public enum PropertyKind
{
    /// <summary>
    /// A property that is mapped from a GraphQL result.
    /// </summary>
    Field,

    /// <summary>
    /// A property that represents an indicator that tells us if a fragment was fulfilled.
    /// </summary>
    FragmentIndicator,

    /// <summary>
    /// A non-null field that is deferred.
    /// </summary>
    DeferredField,
}
