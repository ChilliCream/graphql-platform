namespace HotChocolate.Fusion.Types;

/// <summary>
/// Defines the planning options declared by a composed schema.
/// </summary>
public sealed record PlannerOptions
{
    /// <summary>
    /// Gets the default planning options.
    /// </summary>
    public static PlannerOptions Default { get; } = new();

    /// <summary>
    /// Initializes the default planning options.
    /// </summary>
    public PlannerOptions()
    {
    }

    internal PlannerOptions(
        NodeResolution nodeResolution,
        ShareableFieldRuntimeTypeRouting routing)
    {
        NodeResolution = nodeResolution;
        ShareableFieldRuntimeTypeRouting = routing;
    }

    /// <summary>
    /// Gets how the gateway resolves the <c>Query.node</c> field.
    /// </summary>
    public NodeResolution NodeResolution { get; } = NodeResolution.Gateway;

    /// <summary>
    /// Gets how runtime types are routed for shareable fields whose result type is abstract.
    /// </summary>
    public ShareableFieldRuntimeTypeRouting ShareableFieldRuntimeTypeRouting { get; } = ShareableFieldRuntimeTypeRouting.SourceLocal;
}
