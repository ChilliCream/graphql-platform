namespace HotChocolate.Fusion;

/// <summary>
/// The schema-coordinate kind a member being folded for <c>@cost</c> occupies, which selects
/// its spec default weight under absence (R-COMPOSITION-WEIGHT-FOLD).
/// </summary>
internal enum CostCoordinateKind
{
    /// <summary>An object, interface, or union type. Default weight 1.</summary>
    CompositeType,

    /// <summary>A scalar or enum type. Default weight 0.</summary>
    LeafType,

    /// <summary>
    /// An output field. Default weight 1 when its named type (the list element type for a list
    /// field) is composite, 0 otherwise.
    /// </summary>
    OutputField,

    /// <summary>
    /// An argument or input field. Default weight 1 when its named type is an input object
    /// type, 0 otherwise.
    /// </summary>
    InputValue
}
