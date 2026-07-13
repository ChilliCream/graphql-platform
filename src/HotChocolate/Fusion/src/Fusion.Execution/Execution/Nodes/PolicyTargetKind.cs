namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Identifies the result position a policy applies to.
/// </summary>
public enum PolicyTargetKind
{
    /// <summary>
    /// The policy applies to an object result position.
    /// </summary>
    Object,

    /// <summary>
    /// The policy applies to a field result position.
    /// </summary>
    Field
}
