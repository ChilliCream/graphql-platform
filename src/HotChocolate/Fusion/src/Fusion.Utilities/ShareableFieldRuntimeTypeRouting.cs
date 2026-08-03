namespace HotChocolate.Fusion;

/// <summary>
/// Determines how runtime types are routed for shareable fields whose result type is abstract.
/// </summary>
public enum ShareableFieldRuntimeTypeRouting
{
    /// <summary>
    /// Routes type-conditioned selections using the runtime types declared by the source schema
    /// that resolves the field. This is the default.
    /// </summary>
    SourceLocal,

    /// <summary>
    /// Routes type-conditioned selections using the runtime types common to every viable source
    /// schema that can resolve the field.
    /// </summary>
    CommonRuntimeTypes
}
