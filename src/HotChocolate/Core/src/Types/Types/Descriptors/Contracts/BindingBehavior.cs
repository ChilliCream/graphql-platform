namespace HotChocolate.Types;

/// <summary>
/// Defines the type system binding behavior.
/// </summary>
public enum BindingBehavior
{
    /// <summary>
    /// Implicitly bind type system members.
    /// </summary>
    Implicit = 0,

    /// <summary>
    /// Type system members need to be explicitly bound.
    /// </summary>
    Explicit = 1,
}
