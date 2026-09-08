namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, per-schema snapshot the cost engine compiles operations
/// against. Building a snapshot is the only place the engine reads
/// <see cref="ISchemaDefinition"/> directly; compiling and evaluating a
/// <see cref="CostPlan"/> touch only the snapshot.
/// </summary>
public sealed class CostSchemaSnapshot
{
    /// <summary>
    /// Gets the options this snapshot was built with.
    /// </summary>
    public CostEngineOptions Options => throw ThrowHelper.NotImplemented();

    /// <summary>
    /// Builds a snapshot of <paramref name="schema"/>'s cost-relevant
    /// metadata.
    /// </summary>
    /// <param name="schema">
    /// The schema to snapshot.
    /// </param>
    /// <param name="options">
    /// The engine options that apply to <paramref name="schema"/>.
    /// </param>
    /// <returns>
    /// The immutable snapshot.
    /// </returns>
    public static CostSchemaSnapshot Create(ISchemaDefinition schema, CostEngineOptions options)
    {
        _ = schema;
        _ = options;
        throw ThrowHelper.NotImplemented();
    }
}
