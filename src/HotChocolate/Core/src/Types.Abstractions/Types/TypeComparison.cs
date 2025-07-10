namespace HotChocolate.Types;

/// <summary>
/// Specifies the type comparison mode.
/// </summary>
public enum TypeComparison
{
    /// <summary>
    /// Compare types by reference.
    /// </summary>
    Reference = 0,

    /// <summary>
    /// Compare types structurally.
    /// </summary>
    Structural = 1
}
