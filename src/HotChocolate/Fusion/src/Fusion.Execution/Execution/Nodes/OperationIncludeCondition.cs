namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one client include condition in the operation plan condition table.
/// </summary>
public sealed record OperationIncludeCondition
{
    /// <summary>
    /// Gets the variable used by <c>@skip</c>, or <c>null</c> when absent.
    /// </summary>
    public string? SkipVariable { get; init; }

    /// <summary>
    /// Gets the variable used by <c>@include</c>, or <c>null</c> when absent.
    /// </summary>
    public string? IncludeVariable { get; init; }
}
